using System;
using System.Collections;
using UnityEngine;
public sealed class BedroomWindowPanel : MonoBehaviour
{
 public BedroomPhaseController phases;public BedroomEncounterDirector director;public Transform hinge,latch;
 public float openingAngle=-95,closingSeconds=1;
 public AudioSource closingSource;public AudioClip closingClip;
 [Range(0,1),Tooltip("0 starts with closing motion; 1 plays at the final latch.")] public float closingAudioNormalizedTime=0;
 public bool IsWindowOpen {get;private set;}public bool IsAnimating {get;private set;}public float OpenSeconds {get;private set;}public float OpenAmount {get;private set;}
 public bool HeadOccupiesOpening {get;set;}
 public event Action Closed;
 Quaternion closedRotation=Quaternion.identity,latchRest=Quaternion.identity;Coroutine closing;
 void Awake(){closedRotation=hinge.localRotation;latchRest=latch.localRotation;SetOpenAmount(0);}
 void OnEnable(){if(phases)phases.PhaseChanged+=PhaseChanged;}
 void OnDisable(){if(phases)phases.PhaseChanged-=PhaseChanged;if(Application.isPlaying)ResetClosed();}
 void PhaseChanged(BedroomPhase state){if(state!=BedroomPhase.NightGameplay)ResetClosed();}
 void Update(){if(IsWindowOpen&&phases.State==BedroomPhase.NightGameplay)OpenSeconds+=Time.deltaTime;}
 public void SetOpenAmount(float amount){OpenAmount=Mathf.Clamp01(amount);if(hinge)hinge.localRotation=closedRotation*Quaternion.Euler(0,openingAngle*Mathf.SmoothStep(0,1,OpenAmount),0);IsWindowOpen=OpenAmount>=.999f;}
 public void Rattle(float time,float strength){latch.localRotation=latchRest*Quaternion.Euler(0,0,Mathf.Sin(time*55)*strength*9);}
 public void ResetLatch(){if(latch)latch.localRotation=latchRest;}
 public string CloseBlockedReason
 {
  get
  {
   if(phases.State!=BedroomPhase.NightGameplay)return "Night gameplay is not active.";
   if(phases.IsTransitioning)return "Player transition in progress.";
   if(phases.PlayerState!=NightPlayerState.OutOfBed)return "Get out of bed first.";
   if(HeadOccupiesOpening)return "Window monster still occupies the opening.";
   if(IsAnimating)return "The window is still closing.";
   if(!IsWindowOpen)return "The window is already closed.";
   return director.OperationBlockedReason(this);
  }
 }
 public bool TryClose(){if(!Application.isPlaying||CloseBlockedReason.Length!=0||!director.TryAcquireOperation(this))return false;closing=StartCoroutine(CloseRoutine());return true;}
 IEnumerator CloseRoutine(){IsAnimating=true;bool audioPlayed=false;float start=OpenAmount;float elapsed=0;while(elapsed<closingSeconds){if(phases.State!=BedroomPhase.NightGameplay)yield break;elapsed+=Time.deltaTime;float progress=Mathf.Clamp01(elapsed/Mathf.Max(.01f,closingSeconds));SetOpenAmount(Mathf.Lerp(start,0,progress));if(!audioPlayed&&progress>=closingAudioNormalizedTime){audioPlayed=true;if(closingSource&&closingClip){closingSource.clip=closingClip;closingSource.loop=false;closingSource.Play();}}yield return null;}SetOpenAmount(0);OpenSeconds=0;ResetLatch();IsAnimating=false;closing=null;director.Release(this);Closed?.Invoke();}
 public void ResetClosed(){if(closing!=null)StopCoroutine(closing);closing=null;if(closingSource)closingSource.Stop();IsAnimating=false;HeadOccupiesOpening=false;OpenSeconds=0;SetOpenAmount(0);ResetLatch();if(director)director.Release(this);}
}


