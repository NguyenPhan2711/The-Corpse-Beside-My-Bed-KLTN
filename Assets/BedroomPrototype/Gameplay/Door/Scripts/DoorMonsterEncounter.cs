using UnityEngine;
public enum DoorMonsterState { Dormant, Approaching, GentleKnocking, AggressiveKnocking, DoorOpening, Listening, Retreat, Attack, FakeRetreat }
[DefaultExecutionOrder(30)]
public sealed class DoorMonsterEncounter : MonoBehaviour
{
 public BedroomPhaseController phases;
 public HoldBreathController breath;
 public BedroomEncounterDirector director;
 public DoorEncounterConfig[] nightConfigs;
 public Transform hinge, handle, visual, head, distantAnchor, outsideAnchor, doorwayAnchor, attackAnchor;
 public AudioSource distantSource, approachSource, gentleSource, aggressiveSource, handleSource, creakSource, listeningSource, retreatSource, attackSource;
 public bool encountersEnabled=true, attacksEnabled=true;
 public bool preserveAttackTailOnLoss=true;
 public DoorMonsterState State {get;private set;}
 public float StateElapsed {get;private set;}
 public float RemainingDormant=>Mathf.Max(0,dormant);
 public float RemainingListening=>State==DoorMonsterState.Listening?Mathf.Max(0,ReactionGrace+Config.listeningDuration-StateElapsed):0;
 // Includes the extra grace when this defense started while another one was running.
 public float ReactionGrace=>Config.reactionGrace+director.ConcurrentDefenseGrace(this);
 public bool FakeOut {get;private set;}
 public int SuccessfulDefenses {get;private set;} public int FailedDefenses {get;private set;}
 public DoorEncounterConfig Config=>nightConfigs[Mathf.Clamp(phases.tasks.CurrentDay-1,0,nightConfigs.Length-1)];
 public bool NightActive=>phases.State==BedroomPhase.NightGameplay&&phases.night.Running;
 Quaternion hingeClosed,handleClosed,headRest; float dormant, nextKnock; int gentleKnockCount; bool session, leased,openingAudioPending,closingAudioPlayed; Vector3 attackStart,attackGoal; Quaternion attackRotation;
 void Awake(){hingeClosed=hinge.localRotation;handleClosed=handle.localRotation;headRest=head.localRotation;}
 void OnEnable(){if(phases)phases.PhaseChanged+=PhaseChanged;if(breath)breath.Gasped+=OnGasp;}
 void OnDisable(){if(phases)phases.PhaseChanged-=PhaseChanged;if(breath)breath.Gasped-=OnGasp;ResetEncounter();if(attackSource)attackSource.Stop();}
 public bool DetectsBreathing=>NightActive&&State==DoorMonsterState.Listening&&(breath.IsGasping||(!director.DeferBedAttack(this)&&StateElapsed>=ReactionGrace&&!breath.IsQuiet));
 void OnGasp(){if(NightActive&&State==DoorMonsterState.Listening)Fail();}
 void PhaseChanged(BedroomPhase value){if(value!=BedroomPhase.NightGameplay){session=false;ResetEncounter();}}
 void StopAudio(AudioSource preserve=null){if(!preserve&&preserveAttackTailOnLoss&&phases&&phases.State==BedroomPhase.NightLost&&State==DoorMonsterState.Attack)preserve=attackSource;foreach(var s in Sources())if(s&&s!=preserve)s.Stop();}
 AudioSource[] Sources()=>new[]{distantSource,approachSource,gentleSource,aggressiveSource,handleSource,creakSource,listeningSource,retreatSource,attackSource};
 void Play(AudioSource source,AudioClip clip,bool loop=false){if(!source||!clip)return;source.clip=clip;source.loop=loop;source.Play();}
 void Change(DoorMonsterState next)
 {
  // An opening tail may finish naturally; footsteps/knocks never leak into Listening.
  StopAudio(State==DoorMonsterState.DoorOpening&&next==DoorMonsterState.Listening?creakSource:null);
  State=next;StateElapsed=0;nextKnock=0;gentleKnockCount=0;
  switch(next)
  {
   case DoorMonsterState.Approaching:
    approachSource.transform.position=distantAnchor.position;approachSource.volume=Config.approachStartVolume;
    Play(approachSource,Config.approachingFootsteps?Config.approachingFootsteps:Config.distantFootsteps,Config.loopFootsteps);break;
   case DoorMonsterState.DoorOpening:
    openingAudioPending=Quaternion.Angle(hinge.localRotation,hingeClosed)<1;Play(handleSource,Config.handleTurning);break;
   case DoorMonsterState.Listening:director.BeginBedDefense(this);Play(listeningSource,Config.monsterListening,true);break;
   case DoorMonsterState.Retreat:
    closingAudioPlayed=false;retreatSource.transform.position=outsideAnchor.position;retreatSource.volume=Config.retreatStartVolume;
    Play(retreatSource,Config.retreating,Config.loopFootsteps);break;
   case DoorMonsterState.FakeRetreat:
    retreatSource.transform.position=outsideAnchor.position;retreatSource.volume=Config.retreatStartVolume;
    Play(retreatSource,Config.retreating,Config.loopFootsteps);break;
   case DoorMonsterState.Attack:attackSource.transform.position=head.position;Play(attackSource,Config.attacking);break;
  }
 }
 void Update()
 {
  if(!NightActive||!encountersEnabled||!Config.encounterEnabled){if(session||State!=DoorMonsterState.Dormant){session=false;ResetEncounter();}return;}
  if(!session){session=true;ResetEncounter();}
  StateElapsed+=Time.deltaTime;
  switch(State){
   case DoorMonsterState.Dormant:dormant-=Time.deltaTime;break;
   case DoorMonsterState.Approaching:
    float a=Mathf.Clamp01(StateElapsed/Mathf.Max(.01f,Config.footstepsDuration));approachSource.transform.position=Vector3.Lerp(distantAnchor.position,outsideAnchor.position,a);approachSource.volume=Mathf.Lerp(Config.approachStartVolume,Config.approachEndVolume,a);
    if(StateElapsed>=Config.footstepsDuration){approachSource.Stop();distantSource.Stop();}if(StateElapsed>=Config.footstepsDuration+Config.approachSilence)Change(DoorMonsterState.GentleKnocking);break;
   case DoorMonsterState.GentleKnocking:Knock(false);if(StateElapsed>=Config.gentleDuration)Change(DoorMonsterState.AggressiveKnocking);break;
   case DoorMonsterState.AggressiveKnocking:
    if(StateElapsed<Config.aggressiveDuration){Knock(true);float p=Mathf.Clamp01(StateElapsed/Mathf.Max(.01f,Config.aggressiveDuration));hinge.localRotation=hingeClosed*Quaternion.Euler(0,Mathf.Sin(StateElapsed*55)*p*.45f,0);handle.localRotation=handleClosed*Quaternion.Euler(0,0,Mathf.Sin(StateElapsed*65)*p*5);}
    else {StopAudio();hinge.localRotation=hingeClosed;handle.localRotation=handleClosed;}if(StateElapsed>=Config.aggressiveDuration+Config.silenceAfterBanging)Change(FakeOut?DoorMonsterState.FakeRetreat:DoorMonsterState.DoorOpening);break;
   case DoorMonsterState.DoorOpening:
    handle.localRotation=handleClosed*Quaternion.Euler(0,0,-28*Mathf.Clamp01(StateElapsed/Mathf.Max(.01f,Config.handleDuration)));
    float open=Mathf.Clamp01((StateElapsed-Config.handleDuration)/Mathf.Max(.01f,Config.openingDuration));SetDoor(open);if(open>0){visual.gameObject.SetActive(true);if(openingAudioPending){openingAudioPending=false;Play(creakSource,Config.doorCreaking);}}
    visual.position=Vector3.Lerp(outsideAnchor.position,doorwayAnchor.position,Mathf.SmoothStep(0,1,open));FacePlayer();if(open>=1&&director.TryBeginDefense(this))Change(DoorMonsterState.Listening);break;
   case DoorMonsterState.Listening:
    FacePlayer();if(breath.IsGasping){Fail();break;}if(director.DeferBedAttack(this)&&!breath.IsQuiet){StateElapsed-=Time.deltaTime;break;}if(phases.PlayerState!=NightPlayerState.InBed){Fail();break;}if(StateElapsed>=ReactionGrace&&!breath.IsQuiet){Fail();break;}if(StateElapsed>=ReactionGrace+Config.listeningDuration){SuccessfulDefenses++;if(Config.logResults)Debug.Log("Door defense succeeded");Change(DoorMonsterState.Retreat);}break;
   case DoorMonsterState.Retreat:
    float r=Mathf.Clamp01(StateElapsed/Mathf.Max(.01f,Config.retreatDuration));visual.position=Vector3.Lerp(doorwayAnchor.position,outsideAnchor.position,Mathf.Clamp01(r*2));SetDoor(1-Mathf.Clamp01((r-.35f)/.65f));if(r>.35f&&!closingAudioPlayed){closingAudioPlayed=true;Play(creakSource,Config.doorClosing);}retreatSource.transform.position=Vector3.Lerp(outsideAnchor.position,distantAnchor.position,r);retreatSource.volume=Mathf.Lerp(Config.retreatStartVolume,0,r);if(r>=1)FinishCooldown();break;
   case DoorMonsterState.FakeRetreat:
    // Footsteps walk away without the door opening; counts as a full encounter for pacing and cooldown.
    float f=Mathf.Clamp01(StateElapsed/Mathf.Max(.01f,Config.retreatDuration));retreatSource.transform.position=Vector3.Lerp(outsideAnchor.position,distantAnchor.position,f);retreatSource.volume=Mathf.Lerp(Config.retreatStartVolume,0,f);if(f>=1)FinishCooldown();break;
   case DoorMonsterState.Attack:
    float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(StateElapsed/Mathf.Max(.01f,Config.attackDuration)));visual.SetPositionAndRotation(Vector3.Lerp(attackStart,attackGoal,t),attackRotation);if(t>=1){if(attacksEnabled)phases.LoseNight();else FinishCooldown();}break;
  }
 }
 // Apply after the Animator so the idle clip cannot overwrite the listening pose.
 void LateUpdate(){if(NightActive&&(State==DoorMonsterState.Listening||State==DoorMonsterState.DoorOpening))FacePlayer();if(State==DoorMonsterState.Attack&&attackSource)attackSource.transform.position=head.position;}
 void Knock(bool aggressive)
 {
  var source=aggressive?aggressiveSource:gentleSource;if(StateElapsed<nextKnock||source.isPlaying)return;
  float p=Mathf.Clamp01(StateElapsed/Mathf.Max(.01f,aggressive?Config.aggressiveDuration:Config.gentleDuration));
  source.volume=aggressive?Mathf.Lerp(Config.aggressiveStartVolume,Config.aggressiveEndVolume,p):Mathf.Lerp(Config.gentleStartVolume,Config.gentleEndVolume,p);
  var clip=aggressive?Config.aggressiveKnocking:Config.gentleKnocking;Play(source,clip);gentleKnockCount++;
  float pause=aggressive?Mathf.Lerp(Config.aggressivePauseStart,Config.aggressivePauseEnd,p):Mathf.Lerp(Config.gentlePauseStart,Config.gentlePauseEnd,p);
  nextKnock=StateElapsed+(clip?clip.length/Mathf.Max(.01f,Mathf.Abs(source.pitch)):0)+Mathf.Max(0,pause);
 }
 void SetDoor(float amount){hinge.localRotation=hingeClosed*Quaternion.Euler(0,Config.openingAngle*Mathf.SmoothStep(0,1,amount),0);}
 void FacePlayer(){head.rotation=Quaternion.Slerp(head.rotation,Quaternion.LookRotation(phases.view.transform.position-head.position,Vector3.up)*Quaternion.Euler(Mathf.Sin(Time.time*1.5f)*3,0,0),Time.deltaTime*3);}
 void Fail(){FailedDefenses++;if(Config.logResults)Debug.Log("Door defense failed: breathing or involuntary gasp");attackStart=visual.position;attackRotation=Quaternion.LookRotation(-phases.view.transform.forward,Vector3.up);attackGoal=attackAnchor.position-attackRotation*visual.InverseTransformPoint(head.position);phases.SetObservationLock(this,true);Change(DoorMonsterState.Attack);}
 void FinishCooldown(){ResetEncounter();dormant=director.SampleCooldown(Config.cooldown);}
public bool ForceStartEncounter(){if(!Application.isPlaying||!NightActive||!encountersEnabled||!Config.encounterEnabled||State!=DoorMonsterState.Dormant||!director.TryAcquire(this))return false;leased=true;session=true;FakeOut=director.RollChance(Config.fakeOutChance);Change(DoorMonsterState.Approaching);return true;}
 [ContextMenu("Force encounter (night Play mode)")]void ForceMenu(){ForceStartEncounter();}
 [ContextMenu("Reset encounter")]public void ResetEncounter(){if(director)director.CancelRequest(this);if(leased&&director)director.Release(this);leased=false;StopAudio();State=DoorMonsterState.Dormant;StateElapsed=0;dormant=0;FakeOut=false;if(visual)visual.gameObject.SetActive(false);if(hinge)hinge.localRotation=hingeClosed;if(handle)handle.localRotation=handleClosed;if(head)head.localRotation=headRest;if(phases)phases.SetObservationLock(this,false);}
 public void SetEncountersEnabled(bool value){encountersEnabled=value;if(!value)ResetEncounter();}
 public string Cue=>!NightActive||!director.CuesVisible(Config.textCues)||State==DoorMonsterState.Dormant?"":State==DoorMonsterState.FakeRetreat?GameText.Cue("door.fakeOut"):State==DoorMonsterState.Listening?(phases.PlayerState==NightPlayerState.InBed?GameText.Cue("door.listening"):GameText.Cue("door.returnToBed")):State==DoorMonsterState.DoorOpening?GameText.Cue("door.opening"):State==DoorMonsterState.Retreat?GameText.Cue("door.retreat"):State==DoorMonsterState.AggressiveKnocking&&StateElapsed>=Config.aggressiveDuration?GameText.Cue("door.silence"):GameText.Get(GameText.Cues,StateCueKeys[(int)State],"Door: "+State);
 static readonly string[] StateCueKeys={"door.state.Dormant","door.state.Approaching","door.state.GentleKnocking","door.state.AggressiveKnocking","door.state.DoorOpening","door.state.Listening","door.state.Retreat","door.state.Attack"};
}










