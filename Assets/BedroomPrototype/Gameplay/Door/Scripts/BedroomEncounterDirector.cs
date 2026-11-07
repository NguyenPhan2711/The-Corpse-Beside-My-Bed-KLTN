using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

[DefaultExecutionOrder(100)]
public sealed class BedroomEncounterDirector : MonoBehaviour, IMonsterEncounterCoordinator
{
    public BlanketController blanket;
    public BedroomPhaseController phases;
    public BedroomWindowPanel windowPanel;
    public UnderBedMonsterAI underBed;
    public DoorMonsterEncounter door;
    public WindowMonsterEncounter window;
    public CorpseEncounterController corpse;
    public bool IsPaused => !NightActive || phases.IsTransitioning || phases.ui.DialogueActive;
    public NightEncounterSchedule[] nightSchedules;
    public bool automaticScheduling=true, logScheduling, useFixedSeed;
    public int fixedSeed=12345;
    public float quietInterval=3,selectionInterval=.5f,grantTimeout=2;
    [Min(1)] public float outOfBedAttackGrace=10;
    [Min(.1f)] public float returnedDefenseGrace=1.5f;
    [Tooltip("Testing only: show threat text cues on every night, even where the night's config turns them off.")]
    public bool showThreatCuesOnAllNights;
    [Tooltip("Testing only: every encounter plays out fully, ignoring the configs' fake-out chances.")]
    public bool disableFakeOuts;
    readonly HashSet<Object> active=new HashSet<Object>();
    readonly HashSet<Object> defenders=new HashSet<Object>(),concurrentDefenders=new HashSet<Object>();
    readonly Dictionary<Object,float> requests=new Dictionary<Object,float>();
    readonly Dictionary<Object,float> defenseGrace=new Dictionary<Object,float>();
    readonly List<BedroomEncounterKind> shuffleBag=new List<BedroomEncounterKind>();
    Object operationOwner;
    float availableAt,nextOpportunity,bagWaitSince=-1;
    bool session,firstStarted;
    System.Random random;
    public int SessionSeed {get;private set;}
    public int SelectionCount {get;private set;}
    public float LastWindowSelectionWeight {get;private set;}=1;
    public BedroomEncounterKind LastSelected {get;private set;}=BedroomEncounterKind.None;
    public BedroomEncounterKind FirstSelected {get;private set;}=BedroomEncounterKind.None;
    public NightEncounterSchedule CurrentSchedule=>nightSchedules==null||nightSchedules.Length==0?null:nightSchedules[Mathf.Clamp(phases.tasks.CurrentDay-1,0,nightSchedules.Length-1)];
    public bool HasActiveThreat=>active.Count>0;
    public int ActiveCount=>active.Count;
    public string ActiveThreat=>string.Join(", ",active.Where(a=>a).Select(a=>a.name));
    public bool HasPending(Object candidate)=>requests.ContainsKey(candidate);
    public float RemainingOpportunity=>Mathf.Max(0,nextOpportunity-Time.time);
    public bool UsingShuffleBag=>CurrentSchedule&&CurrentSchedule.useShuffleBag;
    public float NightProgress=>phases&&phases.night?Mathf.Clamp01(phases.night.Elapsed/Mathf.Max(1,phases.night.nightDurationSeconds)):0;
    public float HourMultiplier=>CurrentSchedule?CurrentSchedule.RampMultiplier(NightProgress):1;
    public bool CuesVisible(bool configFlag)=>configFlag||showThreatCuesOnAllNights;
    public string DebugLine=>"Night "+phases.tasks.CurrentDay+" | "+phases.night.Clock+" | ramp x"+HourMultiplier.ToString("F2")+" | next pick "+RemainingOpportunity.ToString("F1")+"s";
    // Seeded with the session, so fixed-seed replays reproduce fake-outs too.
    public bool RollChance(float chance){if(disableFakeOuts)return false;EnsureSession();return chance>0&&random.NextDouble()<chance;}
    public string ShuffleBagContents=>!UsingShuffleBag?"off":shuffleBag.Count==0?"(refills on next pick)":string.Join(",",shuffleBag);
    bool NightActive=>phases&&phases.State==BedroomPhase.NightGameplay&&phases.night.Running;
    void OnEnable(){if(phases)phases.NightPlayerStateChanged+=PlayerStateChanged;}
    void OnDisable(){if(phases)phases.NightPlayerStateChanged-=PlayerStateChanged;}
    void PlayerStateChanged(NightPlayerState state)
    {
        if(state!=NightPlayerState.InBed)foreach(var candidate in requests.Keys.Where(c=>c is WindowMonsterEncounter||c is CorpseEncounterController).ToArray())CancelRequest(candidate);
        else foreach(var candidate in defenseGrace.Keys.ToArray())defenseGrace[candidate]=Time.time+returnedDefenseGrace;
    }
    void EnsureSession()
    {
        if(session)return;session=true;firstStarted=false;FirstSelected=LastSelected=BedroomEncounterKind.None;
        SessionSeed=useFixedSeed?fixedSeed+Mathf.Max(0,phases.tasks.CurrentDay-1)*7919:Guid.NewGuid().GetHashCode();
        random=new System.Random(SessionSeed);nextOpportunity=Time.time+(CurrentSchedule?Mathf.Max(0,CurrentSchedule.initialDelay):18);
        shuffleBag.Clear();bagWaitSince=-1;
    }
    void RefillShuffleBag(){shuffleBag.Clear();foreach(var a in new Object[]{underBed,door,window,corpse})if(a&&CurrentSchedule.Weight(Kind(a))>0)shuffleBag.Add(Kind(a));}
    [ContextMenu("Restart automatic scheduling (debug)")]
    public void RestartAutomaticScheduling(){if(!Application.isPlaying)return;session=false;EnsureSession();LogDecision("explicit debug scheduler restart",null,null);}
    public float SampleCooldown(Vector2 range){EnsureSession();float min=Mathf.Max(0,range.x),max=Mathf.Max(min,range.y);return min+(float)random.NextDouble()*(max-min);}
    float Interval(){var c=CurrentSchedule;float min=c?Mathf.Max(.05f,c.opportunityInterval.x):8,max=c?Mathf.Max(min,c.opportunityInterval.y):14;return (min+(float)random.NextDouble()*(max-min))/HourMultiplier;}
    BedroomEncounterKind Kind(Object candidate)=>candidate==underBed?BedroomEncounterKind.UnderBed:candidate==door?BedroomEncounterKind.Door:candidate==window?BedroomEncounterKind.Window:candidate==corpse?BedroomEncounterKind.Corpse:BedroomEncounterKind.None;
    public float Cooldown(Object candidate)=>candidate==underBed?Mathf.Max(0,underBed.RemainingDormant):candidate==door?Mathf.Max(0,door.RemainingDormant):candidate==window?Mathf.Max(0,window.RemainingDormant):candidate==corpse?Mathf.Max(0,corpse.RemainingDormant):0;
    bool WarningOnly(Object candidate)=>candidate is UnderBedMonsterAI u?(u.State==UnderBedState.Scratching||u.State==UnderBedState.Emerging):candidate is DoorMonsterEncounter d?(d.State==DoorMonsterState.Approaching||d.State==DoorMonsterState.GentleKnocking||d.State==DoorMonsterState.AggressiveKnocking||d.State==DoorMonsterState.DoorOpening||d.State==DoorMonsterState.FakeRetreat):candidate is WindowMonsterEncounter w?(w.State==WindowMonsterState.Knocking||w.State==WindowMonsterState.SilentApproaching||w.State==WindowMonsterState.Opening||w.State==WindowMonsterState.HeadEmerging||w.State==WindowMonsterState.FakeOut):candidate is CorpseEncounterController c?(c.State==CorpseEncounterState.Beckoning||c.IsPaused):false;
    bool CanShareWarnings()=>CurrentSchedule&&CurrentSchedule.allowOverlappingWarnings&&active.Count<CurrentSchedule.maximumWarningConcurrency&&defenders.Count==0&&active.All(WarningOnly);
    bool Eligible(Object candidate)
    {
        if(!NightActive||phases.IsTransitioning)return false;
        if(operationOwner||(windowPanel&&windowPanel.IsAnimating)||blanket.IsCovering)return false;
        if(candidate is WindowMonsterEncounter&&phases.PlayerState!=NightPlayerState.InBed)return false;
        if(candidate is CorpseEncounterController c&&(!c.NightActive||phases.PlayerState!=NightPlayerState.InBed||!c.reveal.Finished))return false;
        return true;
    }
    public string SelectionBlockedReason(Object candidate)
    {
        if(!candidate)return "missing encounter";
        if(!NightActive)return "night inactive";
        if(phases.IsTransitioning||operationOwner||(windowPanel&&windowPanel.IsAnimating))return "mandatory player transition/closing";
        if(blanket.IsCovering)return "blanket is raised";
        if(active.Contains(candidate))return "encounter already active";
        if(candidate is UnderBedMonsterAI u){if(!u.isActiveAndEnabled||!u.encountersEnabled||u.CurrentConfig==null||!u.CurrentConfig.encounterEnabled)return "disabled";if(u.encounterCoordinator!=this)return "external coordinator";if(u.State!=UnderBedState.Dormant)return "not dormant";}
        else if(candidate is DoorMonsterEncounter d){if(!d.isActiveAndEnabled||!d.encountersEnabled||!d.Config.encounterEnabled)return "disabled";if(d.State!=DoorMonsterState.Dormant)return "not dormant";}
        else if(candidate is WindowMonsterEncounter w){if(!w.isActiveAndEnabled||!w.encountersEnabled||!w.Config.encounterEnabled)return "disabled";if(phases.PlayerState!=NightPlayerState.InBed)return "window requires InBed";if(!w.CanSpawn)return "window not dormant/available";}
        else if(candidate is CorpseEncounterController c){if(!c.isActiveAndEnabled||!c.encountersEnabled)return "disabled";if(phases.tasks.CurrentDay!=3||!c.reveal.Finished)return "corpse is passive before Night 3 reveal";if(phases.PlayerState!=NightPlayerState.InBed)return "corpse requires InBed";if(c.State!=CorpseEncounterState.Dormant)return "not dormant";}
        if(Cooldown(candidate)>.001f)return "cooldown "+Cooldown(candidate).ToString("F2")+"s";
        if(CurrentSchedule&&CurrentSchedule.Weight(Kind(candidate))<=0)return "selection weight is zero";
        return "";
    }
    public double SelectionWeight(Object candidate)
    {double weight=CurrentSchedule?CurrentSchedule.Weight(Kind(candidate)):1;return Math.Max(0,weight)*(candidate==window?window.DangerMultiplier:1);}
    public string OperationBlockedReason(Object candidate,bool returning=false)
    {
        if(!NightActive)return GameText.UI("reason.nightInactive");
        if(phases.IsTransitioning)return GameText.UI("reason.transition");
        if(operationOwner&&operationOwner!=candidate)return GameText.UI("reason.windowClosing");
        if(windowPanel&&windowPanel.IsAnimating)return GameText.UI("reason.windowClosing");
        if(!returning&&active.Any(a=>a!=corpse))return GameText.UI("reason.dealThreat");
        if(!returning&&corpse&&active.Contains(corpse)&&!corpse.CanPauseForTravel)return GameText.UI("reason.finishResisting");
        return "";
    }
    public bool TryAcquireOperation(Object candidate,bool returning=false){if(OperationBlockedReason(candidate,returning).Length!=0)return false;operationOwner=candidate;return true;}
    // Explicit force requests remain exclusive. Automatic starts may overlap configurable warning stages only.
    public bool TryAcquire(Object candidate)
    {
        if(active.Contains(candidate))return true;
        if(HasActiveThreat||Time.time<availableAt||!Eligible(candidate))return false;
        EnsureSession();active.Add(candidate);MarkStarted(candidate);CancelRequest(candidate);return true;
    }
    public bool RequestScheduled(Object candidate,float weight=1)
    {
        if((candidate is WindowMonsterEncounter||candidate is CorpseEncounterController)&&phases.PlayerState!=NightPlayerState.InBed){CancelRequest(candidate);return false;}
        if(candidate is CorpseEncounterController c&&!c.NightActive){CancelRequest(candidate);return false;}
        if(active.Contains(candidate))return true;requests[candidate]=weight;return false;
    }
    void MarkStarted(Object candidate){LastSelected=Kind(candidate);if(!firstStarted){FirstSelected=LastSelected;firstStarted=true;}}
    void Update()
    {
        if(!NightActive){session=false;requests.Clear();defenseGrace.Clear();active.Clear();defenders.Clear();concurrentDefenders.Clear();operationOwner=null;return;}
        EnsureSession();
        if(!automaticScheduling||Time.time<nextOpportunity)return;
        if(Time.time<availableAt||(!CanShareWarnings()&&HasActiveThreat)||operationOwner||phases.IsTransitioning||windowPanel.IsAnimating)
        {LogDecision("opportunity deferred by concurrency/transition",null,null);nextOpportunity=Time.time+Mathf.Max(.05f,selectionInterval);return;}
        Object[] all={underBed,door,window,corpse}; // Stable kind order, never hierarchy/component iteration order.
        var eligible=all.Where(a=>a&&SelectionBlockedReason(a).Length==0).ToArray();var weights=eligible.Select(SelectionWeight).ToArray();
        if(eligible.Length==0){LogDecision("no eligible encounters",eligible,weights);nextOpportunity=Time.time+Mathf.Max(.05f,selectionInterval);return;}
        int index=-1;var forced=CurrentSchedule&&phases.tasks.CurrentDay==1&&!firstStarted?CurrentSchedule.forceFirstEncounter:BedroomEncounterKind.None;
        if(forced!=BedroomEncounterKind.None){index=Array.FindIndex(eligible,a=>Kind(a)==forced);if(index<0){LogDecision("configured Night 1 tutorial currently ineligible",eligible,weights);nextOpportunity=Time.time+selectionInterval;return;}}
        else if(UsingShuffleBag)
        {
            if(shuffleBag.Count==0)RefillShuffleBag();
            index=BedroomEncounterSelection.Choose(eligible.Select((a,i)=>shuffleBag.Contains(Kind(a))?weights[i]:0).ToArray(),random);
            if(index<0)
            {
                if(bagWaitSince<0)bagWaitSince=Time.time;
                if(Time.time-bagWaitSince<CurrentSchedule.shuffleBagFallbackSeconds){LogDecision("shuffle bag waiting for "+string.Join(",",shuffleBag),eligible,weights);nextOpportunity=Time.time+selectionInterval;return;}
                index=BedroomEncounterSelection.Choose(weights,random); // Fallback pick; the bag keeps its remaining threats.
            }
        }
        else index=BedroomEncounterSelection.Choose(weights,random);
        if(index<0){LogDecision("no finite positive weights",eligible,weights);nextOpportunity=Time.time+selectionInterval;return;}
        var selected=eligible[index];LastWindowSelectionWeight=eligible.Contains(window)?(float)SelectionWeight(window):0;
        // Recheck immediately before reserving/activating; pending window work cannot bypass the player state.
        if(SelectionBlockedReason(selected).Length!=0){LogDecision("activation recheck failed",eligible,weights);nextOpportunity=Time.time+selectionInterval;return;}
        active.Add(selected);
        bool started=selected==underBed?underBed.StartScheduledEncounter():selected==door?door.ForceStartEncounter():selected==window?window.ForceStartEncounter():corpse.StartScheduledEncounter();
        if(started){SelectionCount++;MarkStarted(selected);shuffleBag.Remove(Kind(selected));bagWaitSince=-1;LogDecision("selected "+Kind(selected),eligible,weights);nextOpportunity=Time.time+Interval();}
        else{active.Remove(selected);LogDecision("selected encounter refused activation",eligible,weights);nextOpportunity=Time.time+selectionInterval;}
    }
    void LogDecision(string result,Object[] eligible,double[] weights)
    {
        if(!logScheduling||!(Application.isEditor||Debug.isDebugBuild))return;
        string candidates=eligible==null?"deferred":string.Join(", ",eligible.Select((e,i)=>Kind(e)+"("+weights[i].ToString("F2")+")"));
        Object[] all={underBed,door,window,corpse};
        Debug.Log("[MonsterDirector] Night="+phases.tasks.CurrentDay+" | Player="+phases.PlayerState+" | Seed="+SessionSeed+" | Eligible="+candidates+" | "+result+" | Status="+string.Join(", ",all.Where(a=>a).Select(a=>Kind(a)+": "+SelectionBlockedReason(a)))+" | Cooldowns="+string.Join(",",all.Where(a=>a).Select(a=>Kind(a)+"="+Cooldown(a).ToString("F1")))+" | WindowOpen="+windowPanel.IsWindowOpen+" | Danger="+(window?window.DangerMultiplier:1).ToString("F1"));
    }
    // One defense at a time unless the night allows concurrent defense; a later defender then gets extra grace.
    public bool TryBeginDefense(Object candidate)
    {
        if(defenders.Contains(candidate))return true;
        if(operationOwner||phases.IsTransitioning||(candidate!=underBed&&blanket.IsCovering))return false;
        if(defenders.Count>0&&!(CurrentSchedule&&CurrentSchedule.allowConcurrentDefense&&defenders.Count<CurrentSchedule.maximumWarningConcurrency))return false;
        if(defenders.Count>0)concurrentDefenders.Add(candidate);
        defenders.Add(candidate);return true;
    }
    public float ConcurrentDefenseGrace(Object candidate)=>concurrentDefenders.Contains(candidate)&&CurrentSchedule?CurrentSchedule.concurrentDefenseGrace:0;
    public int DefenderCount=>defenders.Count;
    void RemoveDefender(Object candidate){defenders.Remove(candidate);concurrentDefenders.Remove(candidate);}
    public void SuspendDefense(Object candidate){RemoveDefender(candidate);defenseGrace.Remove(candidate);}
    public void BeginBedDefense(Object candidate){defenders.Add(candidate);if(phases.PlayerState!=NightPlayerState.InBed||phases.IsTransitioning)defenseGrace[candidate]=Time.time+outOfBedAttackGrace;else defenseGrace.Remove(candidate);}
    public bool DeferBedAttack(Object candidate)=>defenseGrace.TryGetValue(candidate,out float until)&&Time.time<until;
    public float RemainingBedGrace(Object candidate)=>defenseGrace.TryGetValue(candidate,out float until)?Mathf.Max(0,until-Time.time):0;
    public void CancelRequest(Object candidate){bool pending=requests.Remove(candidate);if(pending&&candidate is WindowMonsterEncounter w&&w.logDiagnostics&&(Application.isEditor||Debug.isDebugBuild))Debug.Log("[Window] Pending scheduling request cleared | Player="+(phases?phases.PlayerState.ToString():"Unavailable"));}
    public void Release(Object candidate)
    {
        CancelRequest(candidate);defenseGrace.Remove(candidate);if(operationOwner==candidate)operationOwner=null;RemoveDefender(candidate);
        if(!active.Remove(candidate))return;availableAt=Time.time+quietInterval;
        if(session&&active.Count==0)nextOpportunity=Time.time+Interval();
    }
    public bool TryStartEncounter(UnderBedMonsterAI encounter)=>RequestScheduled(encounter);
    public void EncounterEnded(UnderBedMonsterAI encounter,UnderBedOutcome outcome)=>Release(encounter);
}





