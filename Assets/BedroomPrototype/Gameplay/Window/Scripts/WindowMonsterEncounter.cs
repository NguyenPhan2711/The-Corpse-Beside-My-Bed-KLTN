using UnityEngine;
public enum WindowMonsterState { Dormant,Knocking,Opening,HeadEmerging,AttackWarning,Retreat,Attack,SilentApproaching }
[DefaultExecutionOrder(35)]
public sealed class WindowMonsterEncounter : MonoBehaviour
{
 public BedroomPhaseController phases;public BedroomEncounterDirector director;public BedroomWindowPanel panel;public NightFlashlightController flashlight;
 public WindowEncounterConfig[] nightConfigs;
 public Transform visual,head,upperChest,hiddenPosition,approachPosition,headPosition,attackPosition,retreatPosition;
 public Collider faceTarget;
 public AudioSource quietSource,aggressiveSource,latchSource,openingSource,breathingSource,retreatSource;
 public bool encountersEnabled=true,attacksEnabled=true;
 public bool preserveAttackTailOnLoss=true;
 public WindowMonsterState State {get;private set;}public float StateElapsed {get;private set;}public float Exposure {get;private set;}
 public bool SilentEncounter {get;private set;}
 public float RemainingDormant=>Mathf.Max(0,dormant);
 public float ReactionWindow=>SilentEncounter?Mathf.Max(Config.requiredExposure+.5f,Config.silentReactionWindow):Mathf.Max(Config.requiredExposure+.5f,Config.reactionWindow);
 public int QuietKnockCalls {get;private set;} public int AggressiveKnockCalls {get;private set;} public int OpeningSoundCalls {get;private set;} public int ListeningSoundCalls {get;private set;}
 public int SuccessfulDefenses {get;private set;}public int FailedDefenses {get;private set;}
 public float RemainingReaction=>State==WindowMonsterState.AttackWarning?Mathf.Max(0,ReactionWindow-StateElapsed):0;
 public WindowEncounterConfig Config=>nightConfigs[Mathf.Clamp(phases&&phases.tasks?phases.tasks.CurrentDay-1:0,0,nightConfigs.Length-1)];
 public float DangerMultiplier=>panel.IsWindowOpen?Mathf.Clamp(Config.DangerWeight(panel.OpenSeconds),1,Mathf.Max(1,Config.maximumOpenMultiplier)):1;
 public bool NightActive=>phases.State==BedroomPhase.NightGameplay&&!phases.IsTransitioning&&phases.night.Running;
 public bool logDiagnostics;
 public bool CanSpawn=>NightActive&&phases.PlayerState==NightPlayerState.InBed&&encountersEnabled&&Config.encounterEnabled&&State==WindowMonsterState.Dormant&&!panel.IsAnimating;
 public bool FlashlightDetectionEnabled=>phases.CanUseNightDefenses&&State==WindowMonsterState.AttackWarning;

 float dormant,nextKnock,warningScale=1;int knockCount;bool session,leased;Vector3 retreatStart,attackStart,attackGoal;Quaternion chestRest,headRest,attackRotation;
 void Awake(){chestRest=upperChest.localRotation;headRest=head.localRotation;Hide();}
 void OnEnable(){if(phases){phases.PhaseChanged+=PhaseChanged;phases.NightPlayerStateChanged+=PlayerStateChanged;}if(panel)panel.Closed+=OnClosed;}
 void OnDisable(){if(phases){phases.PhaseChanged-=PhaseChanged;phases.NightPlayerStateChanged-=PlayerStateChanged;}if(panel)panel.Closed-=OnClosed;if(Application.isPlaying)ResetEncounter();if(breathingSource)breathingSource.Stop();}
 void PhaseChanged(BedroomPhase phase){if(phase!=BedroomPhase.NightGameplay){session=false;ResetEncounter();}}
 void PlayerStateChanged(NightPlayerState state)
 {
  if(state==NightPlayerState.InBed){LogDiagnostic("bed scheduling resumed; cooldown preserved");return;}
  bool pending=director.HasPending(this);director.CancelRequest(this);Exposure=0;
  if(State!=WindowMonsterState.Dormant){float remaining=dormant;ResetEncounter();dormant=Mathf.Max(remaining,Config.successCooldown);}
  LogDiagnostic(pending?"pending encounter canceled/deferred":"spawning deferred outside bed");
 }
 void LogDiagnostic(string reason){if(logDiagnostics&&(Application.isEditor||Debug.isDebugBuild))Debug.Log("[Window] "+reason+" | Encounter="+State+" | Player="+phases.PlayerState+" | Spawn="+CanSpawn+" | FlashDetection="+FlashlightDetectionEnabled+" | Open="+panel.IsWindowOpen+" | Danger="+DangerMultiplier.ToString("F1"));}
 void OnClosed(){director.CancelRequest(this);dormant=Mathf.Max(dormant,Config.safeClosedCooldown);}
 AudioSource[] Sources()=>new[]{quietSource,aggressiveSource,latchSource,openingSource,breathingSource,retreatSource};
 void StopAudio(AudioSource preserve=null){if(!preserve&&preserveAttackTailOnLoss&&phases&&phases.State==BedroomPhase.NightLost&&State==WindowMonsterState.Attack)preserve=breathingSource;foreach(var s in Sources())if(s&&s!=preserve)s.Stop();}
 void Play(AudioSource s,AudioClip clip,bool loop=false){if(!s||!clip)return;s.clip=clip;s.loop=loop;s.Play();}
 void Change(WindowMonsterState next)
 {
  StopAudio(State==WindowMonsterState.Opening&&next==WindowMonsterState.HeadEmerging&&!SilentEncounter?openingSource:null);
  State=next;LogDiagnostic("state changed");StateElapsed=0;nextKnock=0;knockCount=0;Exposure=0;
  if(next==WindowMonsterState.Opening&&!SilentEncounter&&!panel.IsWindowOpen){OpeningSoundCalls++;Play(latchSource,Config.latchRattle);Play(openingSource,Config.opening);}
  if(next==WindowMonsterState.AttackWarning&&!SilentEncounter){ListeningSoundCalls++;breathingSource.volume=Config.monsterBreathingVolume;breathingSource.priority=Config.breathingPriority;Play(breathingSource,Config.monsterBreathing,true);}
  if(next==WindowMonsterState.Retreat)Play(retreatSource,Config.retreating);
  // Reuse the existing voice source after incompatible breathing has stopped.
  if(next==WindowMonsterState.Attack){breathingSource.volume=Config.attackVolume;breathingSource.priority=Config.attackPriority;Play(breathingSource,Config.attacking);}
 }
 void Update()
 {
  if(!NightActive){if(phases.State!=BedroomPhase.NightGameplay&&session){session=false;ResetEncounter();}return;}
  if(!encountersEnabled||!Config.encounterEnabled){if(session){session=false;ResetEncounter();}return;}
  if(!session){session=true;ResetEncounter();}
  panel.closingClip=Config.closing;
  StateElapsed+=Time.deltaTime;
  switch(State)
  {
   case WindowMonsterState.Dormant:if(!CanSpawn){director.CancelRequest(this);break;}dormant-=Time.deltaTime;break;
   case WindowMonsterState.SilentApproaching:
    float silent=Mathf.Clamp01(StateElapsed/Mathf.Max(.1f,Config.silentApproachDuration));
    visual.SetPositionAndRotation(Vector3.Lerp(hiddenPosition.position,approachPosition.position,Mathf.SmoothStep(0,1,silent)),approachPosition.rotation);
    if(silent>=1){panel.HeadOccupiesOpening=true;Change(WindowMonsterState.HeadEmerging);}break;
   case WindowMonsterState.Knocking:
    float length=Mathf.Max(Config.minimumKnocking,Config.knockingDuration*warningScale);float k=Mathf.Clamp01(StateElapsed/Mathf.Max(.01f,length));panel.Rattle(StateElapsed,k);
    if(StateElapsed>=nextKnock&&!quietSource.isPlaying&&!aggressiveSource.isPlaying){var source=k<.55f?quietSource:aggressiveSource;source.volume=Mathf.Lerp(Config.quietKnockVolume,Config.aggressiveKnockVolume,k);var clip=k<.55f?Config.quietKnock:Config.aggressiveKnock;if(k<.55f)QuietKnockCalls++;else AggressiveKnockCalls++;Play(source,clip);knockCount++;nextKnock=StateElapsed+(clip?clip.length/Mathf.Max(.01f,Mathf.Abs(source.pitch)):0)+Mathf.Max(0,Mathf.Lerp(Config.knockPauseStart,Config.knockPauseEnd,k));}
    if(StateElapsed>=length){panel.ResetLatch();Change(WindowMonsterState.Opening);}break;
   case WindowMonsterState.Opening:
    visual.gameObject.SetActive(true);visual.SetPositionAndRotation(approachPosition.position,approachPosition.rotation);
    if(!panel.IsWindowOpen)panel.SetOpenAmount(Mathf.Clamp01(StateElapsed/Mathf.Max(.01f,Config.openingDuration)));
    if(panel.IsWindowOpen){panel.HeadOccupiesOpening=true;Change(WindowMonsterState.HeadEmerging);}break;
   case WindowMonsterState.HeadEmerging:
    float e=Mathf.Clamp01(StateElapsed/(SilentEncounter?Mathf.Max(Config.minimumEmerging,Config.silentEmergingDuration):Mathf.Max(Config.minimumEmerging,Config.emergingDuration*warningScale)));visual.SetPositionAndRotation(Vector3.Lerp(approachPosition.position,headPosition.position,Mathf.SmoothStep(0,1,e)),Quaternion.Slerp(approachPosition.rotation,headPosition.rotation,e));if(e>=1&&director.TryBeginDefense(this))Change(WindowMonsterState.AttackWarning);break;
   case WindowMonsterState.AttackWarning:
    Exposure=flashlight.Illuminates(faceTarget)?Exposure+Time.deltaTime:Config.resetExposureOnMiss?0:Mathf.Max(0,Exposure-Config.exposureDecayPerSecond*Time.deltaTime);
    if(Exposure>=Config.requiredExposure){SuccessfulDefenses++;if(Config.logResults)Debug.Log("Window defense succeeded; window remains open");BeginRetreat();}
    else if(StateElapsed>=ReactionWindow)Fail();break;
   case WindowMonsterState.Retreat:
    float r=Mathf.Clamp01(StateElapsed/Mathf.Max(.01f,Config.retreatDuration));visual.position=Vector3.Lerp(retreatStart,retreatPosition.position,1-Mathf.Pow(1-r,3));if(r>=1)Finish();break;
   case WindowMonsterState.Attack:
    float a=Mathf.SmoothStep(0,1,Mathf.Clamp01(StateElapsed/Mathf.Max(.01f,Config.attackDuration)));visual.SetPositionAndRotation(Vector3.Lerp(attackStart,attackGoal,a),attackRotation);if(a>=1){if(attacksEnabled)phases.LoseNight();else Finish();}break;
  }
 }
 void LateUpdate(){if(!NightActive||!visual.gameObject.activeSelf)return;breathingSource.transform.position=head.position;upperChest.localRotation=chestRest*Quaternion.Euler(35,0,0);if(State==WindowMonsterState.HeadEmerging||State==WindowMonsterState.AttackWarning)head.rotation=Quaternion.Slerp(head.rotation,Quaternion.LookRotation(phases.view.transform.position-head.position,Vector3.up),Time.deltaTime*5);}
 bool TryBegin(bool forced){if(!Application.isPlaying||!CanSpawn||!encountersEnabled||!Config.encounterEnabled||State!=WindowMonsterState.Dormant||panel.IsAnimating)return false;bool allowed=forced?director.TryAcquire(this):director.RequestScheduled(this,DangerMultiplier);if(!allowed)return false;leased=true;session=true;warningScale=1-Mathf.Clamp01((DangerMultiplier-1)/Mathf.Max(.01f,Config.maximumOpenMultiplier-1))*.25f;SilentEncounter=panel.IsWindowOpen;StopAudio();if(SilentEncounter){visual.gameObject.SetActive(true);visual.SetPositionAndRotation(hiddenPosition.position,hiddenPosition.rotation);Change(WindowMonsterState.SilentApproaching);}else Change(WindowMonsterState.Knocking);return true;}
 public bool ForceStartEncounter()=>TryBegin(true);
 [ContextMenu("Force window encounter (night Play mode)")]void ForceMenu(){ForceStartEncounter();}
 public void ForceRetreat(){if(!NightActive||State==WindowMonsterState.Dormant)return;BeginRetreat();}
 void BeginRetreat(){retreatStart=visual.position;Change(WindowMonsterState.Retreat);}
 void Fail(){FailedDefenses++;if(Config.logResults)Debug.Log("Window defense failed: face not illuminated continuously");attackStart=visual.position;attackRotation=Quaternion.LookRotation(-phases.view.transform.forward,Vector3.up);attackGoal=attackPosition.position-attackRotation*visual.InverseTransformPoint(head.position);phases.SetObservationLock(this,true);Change(WindowMonsterState.Attack);}
 void Finish(){Release();Hide();panel.HeadOccupiesOpening=false;Change(WindowMonsterState.Dormant);dormant=Config.successCooldown;phases.SetObservationLock(this,false);}
 void Release(){if(director){director.CancelRequest(this);if(leased)director.Release(this);}leased=false;}
 void Hide(){if(visual){visual.gameObject.SetActive(false);visual.SetPositionAndRotation(hiddenPosition.position,hiddenPosition.rotation);}if(faceTarget)faceTarget.enabled=true;}
 public void ResetEncounter(){Release();StopAudio();State=WindowMonsterState.Dormant;StateElapsed=Exposure=0;dormant=0;SilentEncounter=false;Hide();if(head)head.localRotation=headRest;if(upperChest)upperChest.localRotation=chestRest;if(panel){panel.HeadOccupiesOpening=false;panel.ResetLatch();}if(phases)phases.SetObservationLock(this,false);}
 public void SetEncountersEnabled(bool value){encountersEnabled=value;if(!value)ResetEncounter();}
 public string Cue=>!NightActive||!Config.textCues||(SilentEncounter&&State!=WindowMonsterState.Dormant&&!Config.showSilentAttackDebugCue)?"":State==WindowMonsterState.AttackWarning?"Aim at its FACE. F: flashlight.":State==WindowMonsterState.Knocking?"Tapping at the window...":State==WindowMonsterState.Opening?"The window latch gives way...":State==WindowMonsterState.HeadEmerging?"Something is leaning through the window.":State==WindowMonsterState.Retreat?"It recoils. The window is still open.":panel.IsWindowOpen?"The open window invites it back.":"";
}









