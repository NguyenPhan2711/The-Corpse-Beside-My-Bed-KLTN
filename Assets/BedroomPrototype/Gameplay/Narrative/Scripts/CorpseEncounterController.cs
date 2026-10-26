using UnityEngine;
using UnityEngine.InputSystem;
public enum CorpseEncounterState{Dormant,Beckoning,Reaching,Resisting,Retreat,Surrender}
[DefaultExecutionOrder(-30)]
public sealed class CorpseEncounterController:MonoBehaviour
{
 public BedroomPhaseController phases;public BedroomEncounterDirector director;public CorpseGlassReveal reveal;public CorpseEncounterConfig config;public Transform bodyRoot,head,reachingArm,mattress;public AudioSource creakSource;public TMPro.TMP_Text cue;
 public InputAction resistAction=new InputAction("Resist corpse",InputActionType.Button,"<Keyboard>/q");public bool encountersEnabled=true;
 public CorpseEncounterState State{get;private set;}public float StateElapsed{get;private set;}public float PullProgress{get;private set;}public float ResistanceProgress{get;private set;}public float RemainingDormant{get;private set;}public float CameraDrop{get;private set;}public int SuccessfulDefenses{get;private set;}public int FailedDefenses{get;private set;}
 public bool BlocksOtherDefenses=>State==CorpseEncounterState.Resisting||State==CorpseEncounterState.Surrender;
 public bool NightActive=>phases.State==BedroomPhase.NightGameplay&&phases.night.Running&&phases.tasks.CurrentDay==3&&reveal.Finished;
 public bool BedAvailable=>NightActive&&phases.PlayerState==NightPlayerState.InBed&&!phases.IsTransitioning&&!phases.ui.ModalOpen;
 public bool FacingCorpse=>Vector3.Angle(phases.view.transform.forward,head.position-phases.view.transform.position)<=config.gazeDegrees;
 public bool CanPauseForTravel=>State==CorpseEncounterState.Beckoning||State==CorpseEncounterState.Reaching;
 public bool IsPaused=>!BedAvailable;public bool InputActive=>resistAction.enabled;public bool RequiresRelease=>requireRelease;
 Vector3 bodyHome,mattressHome,surrenderStart,surrenderGoal;Quaternion bodyRotation,headHome,armHome;bool leased,requireRelease;float resistanceStartPull;
 void Awake(){bodyHome=bodyRoot.localPosition;bodyRotation=bodyRoot.localRotation;headHome=head.localRotation;armHome=reachingArm.localRotation;mattressHome=mattress.localPosition;}
 void OnEnable(){if(phases){phases.PhaseChanged+=PhaseChanged;phases.NightPlayerStateChanged+=PlayerStateChanged;phases.night.Began+=NewNight;}}
 void OnDisable(){if(phases){phases.PhaseChanged-=PhaseChanged;phases.NightPlayerStateChanged-=PlayerStateChanged;phases.night.Began-=NewNight;}ResetEncounter();}
 void OnDestroy(){resistAction.Dispose();}
 void NewNight(){ResetEncounter();RemainingDormant=config.initialDelay;requireRelease=Keyboard.current!=null&&Keyboard.current.qKey.isPressed;}
 void PhaseChanged(BedroomPhase phase){if(phase!=BedroomPhase.NightGameplay)ResetEncounter();}
 void PlayerStateChanged(NightPlayerState state){requireRelease=Keyboard.current!=null&&Keyboard.current.qKey.isPressed;if(state!=NightPlayerState.InBed){resistAction.Disable();director.SuspendDefense(this);if(State==CorpseEncounterState.Resisting){State=CorpseEncounterState.Reaching;ResistanceProgress=0;}CameraDrop=0;RestorePose();}}
 void Change(CorpseEncounterState next){State=next;StateElapsed=0;if(next==CorpseEncounterState.Resisting){resistanceStartPull=PullProgress;ResistanceProgress=0;phases.ClearHeldDefenses();}if(next==CorpseEncounterState.Surrender){FailedDefenses++;surrenderStart=bodyRoot.position;surrenderGoal=phases.view.transform.position+phases.view.transform.forward*.27f-(head.position-bodyRoot.position);phases.SetObservationLock(this,true);phases.ClearHeldDefenses();}if(next==CorpseEncounterState.Beckoning&&creakSource&&config.creak)creakSource.PlayOneShot(config.creak);}
 public bool CanSpawn=>encountersEnabled&&isActiveAndEnabled&&BedAvailable&&State==CorpseEncounterState.Dormant&&RemainingDormant<=0;
 bool Begin(bool force){if(!encountersEnabled||!BedAvailable||State!=CorpseEncounterState.Dormant||(!force&&RemainingDormant>0)||!director.TryAcquire(this))return false;leased=true;headHome=head.localRotation;armHome=reachingArm.localRotation;requireRelease=Keyboard.current!=null&&Keyboard.current.qKey.isPressed;PullProgress=ResistanceProgress=0;Change(CorpseEncounterState.Beckoning);return true;}
 public bool ForceStartEncounter()=>Application.isPlaying&&Begin(true);public bool StartScheduledEncounter()=>Application.isPlaying&&Begin(false);
 void Update()
 {
  if(cue){cue.gameObject.SetActive(BedAvailable&&State!=CorpseEncounterState.Dormant);cue.text=State==CorpseEncounterState.Beckoning?"The mattress shifts beside you...":State==CorpseEncounterState.Reaching?"Look toward him. Hold Q to push yourself upright.":State==CorpseEncounterState.Resisting?"Keep holding Q.":"";}
  bool held=Keyboard.current!=null&&Keyboard.current.qKey.isPressed;if(!held)requireRelease=false;
  if(!encountersEnabled||!NightActive){if(State!=CorpseEncounterState.Dormant||CameraDrop>0)ResetEncounter();resistAction.Disable();return;}
  if(!BedAvailable){resistAction.Disable();CameraDrop=0;return;}
  if(State==CorpseEncounterState.Reaching||State==CorpseEncounterState.Resisting)resistAction.Enable();else resistAction.Disable();
  if((State==CorpseEncounterState.Reaching||State==CorpseEncounterState.Resisting)&&!director.TryBeginDefense(this)){resistAction.Disable();return;}
  StateElapsed+=Time.deltaTime;
  switch(State)
  {
   case CorpseEncounterState.Dormant:RemainingDormant=Mathf.Max(0,RemainingDormant-Time.deltaTime);break;
   case CorpseEncounterState.Beckoning:
    if(StateElapsed>=config.beckoningSeconds&&director.TryBeginDefense(this)){director.BeginBedDefense(this);Change(CorpseEncounterState.Reaching);}break;
   case CorpseEncounterState.Reaching:
    if(!requireRelease&&resistAction.IsPressed()&&FacingCorpse){Change(CorpseEncounterState.Resisting);break;}
    PullProgress=Mathf.Clamp01(PullProgress+Time.deltaTime/Mathf.Max(.1f,config.reachingSeconds));if(PullProgress>=1)Change(CorpseEncounterState.Surrender);break;
   case CorpseEncounterState.Resisting:
    if(requireRelease||!resistAction.IsPressed()||!FacingCorpse){PullProgress=Mathf.Clamp01(resistanceStartPull+ResistanceProgress*config.resistanceSeconds/Mathf.Max(.1f,config.reachingSeconds));ResistanceProgress=0;Change(PullProgress>=1?CorpseEncounterState.Surrender:CorpseEncounterState.Reaching);break;}
    ResistanceProgress=Mathf.Clamp01(ResistanceProgress+Time.deltaTime/Mathf.Max(.1f,config.resistanceSeconds));if(ResistanceProgress>=1){SuccessfulDefenses++;PullProgress=0;Change(CorpseEncounterState.Retreat);}break;
   case CorpseEncounterState.Retreat:if(StateElapsed>=config.retreatSeconds){RestorePose();Change(CorpseEncounterState.Dormant);if(leased)director.Release(this);leased=false;RemainingDormant=director.SampleCooldown(config.cooldown);}break;
   case CorpseEncounterState.Surrender:
    float fail=Mathf.Clamp01(StateElapsed/Mathf.Max(.1f,config.surrenderSeconds));bodyRoot.position=Vector3.Lerp(surrenderStart,surrenderGoal,Mathf.SmoothStep(0,1,fail));phases.ui.SetFade(fail);if(fail>=1){phases.ui.SetFade(0);phases.LoseNight();}break;
  }
  float target=State==CorpseEncounterState.Beckoning?config.maximumCameraDrop*.12f*Mathf.Clamp01(StateElapsed/config.beckoningSeconds):State==CorpseEncounterState.Reaching?config.maximumCameraDrop*Mathf.Lerp(.12f,1,PullProgress):State==CorpseEncounterState.Resisting?config.maximumCameraDrop*Mathf.Lerp(resistanceStartPull,0,ResistanceProgress):State==CorpseEncounterState.Surrender?config.maximumCameraDrop:0;
  CameraDrop=Mathf.MoveTowards(CameraDrop,target,config.cameraRecoverySpeed*Time.deltaTime);
 }
 void LateUpdate()
 {
  if(!BedAvailable||State==CorpseEncounterState.Dormant||State==CorpseEncounterState.Retreat){return;}
  float amount=State==CorpseEncounterState.Beckoning?.15f:State==CorpseEncounterState.Surrender?1:Mathf.Lerp(.3f,.8f,PullProgress);
  head.rotation=Quaternion.Slerp(head.rotation,Quaternion.LookRotation(phases.view.transform.position-head.position,Vector3.up),amount*Time.deltaTime*2);
  reachingArm.localRotation=armHome*Quaternion.Euler(-28*amount,0,15*amount);mattress.localPosition=mattressHome+Vector3.down*(config.mattressMotion*(.5f+.5f*Mathf.Sin(Time.time*2)));
 }
 public void ApplyCameraEffect(Transform view)
 {
  if(!BedAvailable)return;var pos=view.position;float minimum=phases.protagonistPillow.bounds.max.y+phases.view.nearClipPlane+.05f;pos.y=Mathf.Max(minimum,pos.y-CameraDrop);view.position=pos;
  if(State==CorpseEncounterState.Beckoning){float blend=Mathf.Clamp01(StateElapsed/config.beckoningSeconds);float sign=Mathf.Sign(Vector3.SignedAngle(view.forward,head.position-view.position,Vector3.up));view.rotation*=Quaternion.Euler(0,sign*config.warningYaw*blend,config.warningRoll*blend);}
  if(State==CorpseEncounterState.Surrender)view.rotation=Quaternion.Slerp(view.rotation,Quaternion.LookRotation(head.position-view.position,Vector3.up),Mathf.Clamp01(StateElapsed/config.surrenderSeconds));
 }
 void RestoreBonePose(){if(head)head.localRotation=headHome;if(reachingArm)reachingArm.localRotation=armHome;if(mattress)mattress.localPosition=mattressHome;}
 void RestorePose(){if(bodyRoot){bodyRoot.localPosition=bodyHome;bodyRoot.localRotation=bodyRotation;}RestoreBonePose();}
 [ContextMenu("Reset corpse encounter")]
 public void ResetEncounter(){if(director){director.CancelRequest(this);if(leased)director.Release(this);}leased=false;State=CorpseEncounterState.Dormant;StateElapsed=PullProgress=ResistanceProgress=CameraDrop=0;resistAction.Disable();RestorePose();if(phases){phases.SetObservationLock(this,false);if(phases.ui)phases.ui.SetFade(0);}if(creakSource)creakSource.Stop();}
}





