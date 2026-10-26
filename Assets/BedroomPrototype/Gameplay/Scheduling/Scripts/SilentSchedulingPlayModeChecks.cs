#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public sealed class SilentSchedulingPlayModeChecks : MonoBehaviour
{
 [Serializable]public class Result{public bool finished,passed;public string error;public List<string> checks=new List<string>();public List<string> sequences=new List<string>();}public Result report=new Result();
 BedroomPhaseController p;BedroomEncounterDirector director;WindowMonsterEncounter w;DoorMonsterEncounter d;UnderBedMonsterAI u;
 Keyboard keyboard;Mouse mouse;InputSettings originalInput,testInput;WindowEncounterConfig[] oldWindow;DoorEncounterConfig[] oldDoor;UnderBedEncounterConfig[] oldUnder;AudioClip tone;
 void Check(bool condition,string label){report.checks.Add((condition?"PASS: ":"FAIL: ")+label);if(!condition)throw new Exception(label);}
 void Key(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
 IEnumerator Keys(params Key[] keys){Key(keys);yield return null;yield return null;}
 IEnumerator Wait(Func<bool> condition,float timeout=12){float end=Time.realtimeSinceStartup+timeout;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;}
 void AimNow(Vector3 target){p.CaptureCursor();var wanted=Quaternion.LookRotation(target-p.view.transform.position);InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(Mathf.DeltaAngle(p.view.transform.eulerAngles.y,wanted.eulerAngles.y),-Mathf.DeltaAngle(p.view.transform.eulerAngles.x,wanted.eulerAngles.x))/p.movement.mouseSensitivity);}
 IEnumerator Aim(Vector3 target){AimNow(target);yield return null;yield return null;}
 void Reply(){p.ui.ActiveButtonRoot.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.gameObject.activeSelf&&b!=p.ui.buttonTemplate).onClick.Invoke();}
 IEnumerator Start()
 {
  p=FindAnyObjectByType<BedroomPhaseController>();director=FindAnyObjectByType<BedroomEncounterDirector>();w=director.window;d=director.door;u=director.underBed;
  SchedulingTestOverride.Configure(director,.2f);director.automaticScheduling=false;director.quietInterval=.05f;director.selectionInterval=.05f;
  originalInput=InputSystem.settings;testInput=Instantiate(originalInput);testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings=testInput;keyboard=InputSystem.AddDevice<Keyboard>("SilentSchedulerVerification");mouse=InputSystem.AddDevice<Mouse>("SilentSchedulerMouse");
  oldWindow=w.nightConfigs;w.nightConfigs=oldWindow.Select(Instantiate).ToArray();oldDoor=d.nightConfigs;d.nightConfigs=oldDoor.Select(Instantiate).ToArray();oldUnder=u.nightConfigs;u.nightConfigs=oldUnder.Select(Instantiate).ToArray();
  tone=AudioClip.Create("Temporary zero-sample playback test",44100,1,44100,false);
  foreach(var c in w.nightConfigs){c.knockingDuration=1.4f;c.minimumKnocking=.1f;c.knockPauseStart=c.knockPauseEnd=.05f;c.openingDuration=.4f;c.emergingDuration=.5f;c.minimumEmerging=.4f;c.silentApproachDuration=.3f;c.silentEmergingDuration=.7f;c.silentReactionWindow=2.5f;c.successCooldown=.8f;c.retreatDuration=.25f;c.attackDuration=.2f;c.quietKnock=c.aggressiveKnock=c.latchRattle=c.opening=c.monsterBreathing=tone;}
  foreach(var c in d.nightConfigs){c.footstepsDuration=.25f;c.approachSilence=.1f;c.gentleDuration=.25f;c.aggressiveDuration=.3f;c.silenceAfterBanging=.15f;c.handleDuration=.15f;c.openingDuration=.3f;c.reactionGrace=.4f;c.listeningDuration=.6f;c.retreatDuration=.25f;c.attackDuration=.2f;c.cooldown=new Vector2(.8f,.8f);}
  foreach(var c in u.nightConfigs){c.scratchingDuration=.2f;c.emergingDuration=.4f;c.reactionWindow=1.2f;c.requiredHidingDuration=.7f;c.retreatDuration=.3f;c.cooldownRange=new Vector2(.8f,.8f);c.attackDuration=.2f;}
  p.night.nightDurationSeconds=300;w.flashlight.batteryEnabled=false;u.blanket.coverSeconds=.08f;u.blanket.uncoverSeconds=.07f;
  var runner=Run();while(true){bool more=false;object next=null;try{more=runner.MoveNext();if(more)next=runner.Current;}catch(Exception ex){report.error=ex.ToString();break;}if(!more)break;yield return next;}
  Key();InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.settings=originalInput;Destroy(testInput);foreach(var c in w.nightConfigs)Destroy(c);w.nightConfigs=oldWindow;foreach(var c in d.nightConfigs)Destroy(c);d.nightConfigs=oldDoor;foreach(var c in u.nightConfigs)Destroy(c);u.nightConfigs=oldUnder;Destroy(tone);
  report.finished=true;report.passed=string.IsNullOrEmpty(report.error);System.IO.File.WriteAllText("Temp/BedroomPrototype/SilentSchedulingPlayModeChecks.json",JsonUtility.ToJson(report,true));Debug.Log("Silent/scheduling checks: "+report.passed+" "+report.error);
 }
 IEnumerator CounterWindow(){yield return Aim(w.faceTarget.bounds.center);if(!w.flashlight.SwitchedOn){yield return Keys(UnityEngine.InputSystem.Key.F);yield return Keys();}yield return Aim(w.faceTarget.bounds.center);yield return Wait(()=>w.State==WindowMonsterState.Retreat);yield return Wait(()=>w.State==WindowMonsterState.Dormant);if(w.flashlight.SwitchedOn){yield return Keys(UnityEngine.InputSystem.Key.F);yield return Keys();}}
 IEnumerator SurviveSelections(int count,List<BedroomEncounterKind> observed,float timeout=30)
 {
  int completed=w.SuccessfulDefenses+d.SuccessfulDefenses+u.SuccessfulDefenses;int seen=director.SelectionCount;float until=Time.time+timeout;bool lastF=false;
  while(p.State==BedroomPhase.NightGameplay&&Time.time<until)
  {
   if(w.SuccessfulDefenses+d.SuccessfulDefenses+u.SuccessfulDefenses>=completed+count){director.automaticScheduling=false;if(!director.HasActiveThreat)break;}
   if(director.SelectionCount!=seen){seen=director.SelectionCount;observed.Add(director.LastSelected);}
   var keys=new List<Key>();if(u.State==UnderBedState.FullyExposed||u.State==UnderBedState.PlayerHiding||u.State==UnderBedState.Retreat)keys.Add(UnityEngine.InputSystem.Key.Space);if(d.State==DoorMonsterState.Listening||d.State==DoorMonsterState.Retreat)keys.Add(UnityEngine.InputSystem.Key.R);
   bool wantsLight=w.State==WindowMonsterState.HeadEmerging||w.State==WindowMonsterState.AttackWarning;
   if(wantsLight)AimNow(w.faceTarget.bounds.center);
   bool toggle=wantsLight!=w.flashlight.SwitchedOn&&!lastF;if(toggle)keys.Add(UnityEngine.InputSystem.Key.F);lastF=toggle;Key(keys.ToArray());yield return new WaitForEndOfFrame();
  }
  Key();director.automaticScheduling=false;
  while(director.HasActiveThreat&&Time.time<until)yield return null;
 }
 IEnumerator Run()
 {
  yield return null;Check(!w.ForceStartEncounter(),"window force rejects morning");p.SitAtDesk();yield return Wait(()=>p.State==BedroomPhase.MorningDesk&&!p.IsTransitioning);p.ui.OpenLaptop();for(int i=0;i<p.tasks.Definition.messages.Length;i++){Reply();yield return null;}p.ui.CloseModal();p.ui.OpenTruth();for(int i=0;i<p.tasks.Definition.conversation.Length;i++){Reply();yield return null;}p.ui.CloseModal();p.StandUp();yield return Wait(()=>p.State==BedroomPhase.MorningExploration&&!p.IsTransitioning);p.ceilingSwitch.SetLight(false);p.GoToBed();yield return Wait(()=>p.CanUseNightDefenses);yield return null;
  Check(w.ForceStartEncounter()&&w.State==WindowMonsterState.Knocking&&!w.SilentEncounter,"closed window starts audible knocking mode");yield return new WaitForSeconds(.08f);Check(w.quietSource.isPlaying&&w.QuietKnockCalls>0,"closed window executes real quiet-knock playback");yield return Wait(()=>w.State==WindowMonsterState.Opening);Check(w.AggressiveKnockCalls>0,"closed knocking escalates to aggressive playback");yield return new WaitForSeconds(.15f);Check(w.openingSource.isPlaying&&w.OpeningSoundCalls==1&&w.panel.OpenAmount>0&&w.panel.OpenAmount<1,"closed window plays opening sound and visible animation");yield return Wait(()=>w.State==WindowMonsterState.AttackWarning);yield return CounterWindow();Check(w.SuccessfulDefenses==1&&w.panel.IsWindowOpen,"flashlight repels closed-window attack and leaves it open");Check(director.SelectionBlockedReason(w).StartsWith("cooldown"),"shared scheduler excludes window during its real cooldown");
  int knocks=w.QuietKnockCalls+w.AggressiveKnockCalls,opening=w.OpeningSoundCalls,listening=w.ListeningSoundCalls;var rotation=w.panel.hinge.localRotation;yield return new WaitForSeconds(.1f);Check(w.ForceStartEncounter()&&w.SilentEncounter&&w.State==WindowMonsterState.SilentApproaching,"already-open window enters silent approach directly");
  bool stayedOpen=true,neverAnnounced=true;float until=Time.time+5;while(w.State!=WindowMonsterState.AttackWarning&&Time.time<until){stayedOpen&=w.panel.IsWindowOpen&&Quaternion.Angle(rotation,w.panel.hinge.localRotation)<.01f;neverAnnounced&=!w.quietSource.isPlaying&&!w.aggressiveSource.isPlaying&&!w.latchSource.isPlaying&&!w.openingSource.isPlaying&&!w.breathingSource.isPlaying&&w.Cue=="";yield return null;}
  Check(stayedOpen,"silent approach never closes or reanimates the open panel");Check(neverAnnounced&&knocks==w.QuietKnockCalls+w.AggressiveKnockCalls&&opening==w.OpeningSoundCalls&&listening==w.ListeningSoundCalls,"silent attack emits no knock/open/listening announcement or attack text cue");Check(w.State==WindowMonsterState.AttackWarning&&w.visual.gameObject.activeSelf&&w.head.position.z<2.8f,"silent head emerges visibly through opening");yield return new WaitForSeconds(.2f);Check(p.State==BedroomPhase.NightGameplay&&w.RemainingReaction>1,"silent head grants a fair reaction window instead of instant loss");yield return CounterWindow();Check(w.SuccessfulDefenses==2&&w.panel.IsWindowOpen,"flashlight repels silent attack and keeps window open");Check(director.SelectionWeight(w)>director.SelectionWeight(d),"open danger raises relative window scheduling weight");
  p.interactor.RefreshTarget();yield return Keys(UnityEngine.InputSystem.Key.E);yield return Keys();yield return Wait(()=>p.NightStanding&&!p.IsTransitioning);Check(!w.ForceStartEncounter()&&!director.RequestScheduled(w,3)&&!director.HasPending(w),"standing blocks both window activation and pending requests");Check(!w.flashlight.IsEmitting,"standing flashlight stays disabled");Check(!director.TryAcquire(w),"director itself rejects standing window starts");Check(w.panel.TryClose(),"standing window can be closed after retreat");yield return Wait(()=>!w.panel.IsAnimating);Check(w.DangerMultiplier==1,"closing resets extra danger weight");p.nightExcursion.TryReturnToBed();yield return Wait(()=>p.CanUseNightDefenses);Check(w.CanSpawn,"returning to bed restores window eligibility");
  var seeds=new int[3];for(int seed=1;seed<1000;seed++){int choice=BedroomEncounterSelection.Choose(new double[]{1,1,1},new System.Random(seed));if(seeds[choice]==0)seeds[choice]=seed;if(seeds.All(s=>s!=0))break;}
  for(int run=0;run<3;run++)
  {
   u.ResetEncounter();d.ResetEncounter();w.ResetEncounter();w.panel.ResetClosed();director.fixedSeed=seeds[run];director.useFixedSeed=true;director.CurrentSchedule.initialDelay=.15f;director.CurrentSchedule.opportunityInterval=new Vector2(.15f,.25f);director.CurrentSchedule.allowOverlappingWarnings=false;director.automaticScheduling=true;director.RestartAutomaticScheduling();var seen=new List<BedroomEncounterKind>();int before=u.SuccessfulDefenses+d.SuccessfulDefenses+w.SuccessfulDefenses;yield return SurviveSelections(3,seen);Check(p.State==BedroomPhase.NightGameplay&&u.SuccessfulDefenses+d.SuccessfulDefenses+w.SuccessfulDefenses>=before+3,"three complete random encounters survive seed "+seeds[run]);Check(seen.Count>=3&&seen[0]==(BedroomEncounterKind)run,"runtime first selection matches seeded production selector for "+(BedroomEncounterKind)run);report.sequences.Add("seed "+seeds[run]+": "+string.Join(" -> ",seen));
  }
  // Enable actual overlapping warnings and verify the mandatory defense stages never overlap.
  u.ResetEncounter();d.ResetEncounter();w.ResetEncounter();w.panel.ResetClosed();u.attacksEnabled=d.attacksEnabled=false;w.SetEncountersEnabled(false);director.CurrentSchedule.allowOverlappingWarnings=true;director.CurrentSchedule.opportunityInterval=new Vector2(.05f,.05f);director.CurrentSchedule.initialDelay=.05f;director.CurrentSchedule.maximumWarningConcurrency=2;director.automaticScheduling=true;director.RestartAutomaticScheduling();bool overlapped=false,conflict=false;float end=Time.time+2;
  while(Time.time<end&&p.State==BedroomPhase.NightGameplay){overlapped|=director.ActiveCount==2;conflict|=u.State==UnderBedState.FullyExposed&&d.State==DoorMonsterState.Listening;yield return null;}
  Check(overlapped,"configured shared director can overlap warning stages");Check(!conflict,"defense reservation prevents simultaneous blanket and breath requirements");director.automaticScheduling=false;u.ResetEncounter();d.ResetEncounter();u.attacksEnabled=d.attacksEnabled=true;yield return new WaitForSeconds(.1f);director.CurrentSchedule.initialDelay=.1f;director.automaticScheduling=true;director.RestartAutomaticScheduling();int overlapBefore=u.SuccessfulDefenses+d.SuccessfulDefenses;var overlapSequence=new List<BedroomEncounterKind>();yield return SurviveSelections(4,overlapSequence);Check(p.State==BedroomPhase.NightGameplay&&u.SuccessfulDefenses+d.SuccessfulDefenses>=overlapBefore+4,"real blanket and breath counters survive overlapping warning sequences");report.sequences.Add("overlap: "+string.Join(" -> ",overlapSequence));u.SetEncountersEnabled(false);d.SetEncountersEnabled(false);u.attacksEnabled=d.attacksEnabled=true;director.automaticScheduling=false;yield return null;
  // Explicit tutorial is opt-in and applies only on Night 1.
  w.SetEncountersEnabled(true);w.ResetEncounter();director.CurrentSchedule.forceFirstEncounter=BedroomEncounterKind.Window;director.CurrentSchedule.initialDelay=.05f;director.CurrentSchedule.allowOverlappingWarnings=false;director.automaticScheduling=true;director.RestartAutomaticScheduling();yield return Wait(()=>w.State!=WindowMonsterState.Dormant);Check(director.FirstSelected==BedroomEncounterKind.Window,"explicit Night 1 forced-first option is honored");director.automaticScheduling=false;w.ForceRetreat();yield return Wait(()=>w.State==WindowMonsterState.Dormant);yield return new WaitForSeconds(.1f);w.panel.SetOpenAmount(1);w.ForceStartEncounter();yield return Wait(()=>w.State==WindowMonsterState.AttackWarning);yield return Wait(()=>p.State==BedroomPhase.NightLost);Check(p.State==BedroomPhase.NightLost,"missing a silent flashlight counter calls existing loss");p.RetryNight();yield return null;Check(p.State==BedroomPhase.NightGameplay&&p.PlayerState==NightPlayerState.InBed,"retry resets encounter ownership without duplicate player");w.SetEncountersEnabled(false);p.night.nightDurationSeconds=.2f;p.night.BeginNight();yield return Wait(()=>p.State==BedroomPhase.NightWon);Check(p.State==BedroomPhase.NightWon&&!director.HasActiveThreat,"night victory cancels scheduled threats");
 }
}
#endif




