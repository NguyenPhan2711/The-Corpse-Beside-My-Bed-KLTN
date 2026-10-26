using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public sealed class NightIntroductionController:MonoBehaviour
{
 public BedroomPhaseController phases;public CorpseGlassReveal reveal;public DialogueSequence[] introductions;
 [Min(0)]public float pauseBeforeGameplay=.5f;public bool skipSeenAutomaticallyOnRetry;public string skipPrompt="Tab - Skip introduction (already seen)";
 public TMPro.TMP_Text skipLabel;public InputAction skipAction=new InputAction("Skip seen introduction",InputActionType.Button,"<Keyboard>/tab");
 [SerializeField]bool[] seen=new bool[3];public bool IsIntroducing=>phases.State==BedroomPhase.NightIntroduction;public bool CanSkip=>IsIntroducing&&seen[Mathf.Clamp(phases.tasks.CurrentDay-1,0,2)];public bool Finishing{get;private set;}
 [System.NonSerialized]public bool debugAutoSkipAllIntroductions;
 void OnEnable(){skipAction.Enable();}void OnDisable(){skipAction.Disable();CancelIntroduction();}void OnDestroy(){skipAction.Dispose();}
 void Update(){if(skipLabel){skipLabel.gameObject.SetActive(CanSkip&&!Finishing);skipLabel.text=skipPrompt;}if(CanSkip&&!Finishing&&skipAction.WasPressedThisFrame())SkipIntroduction();}
 public void BeginIntroduction(bool retry=false)
 {
  CancelIntroduction();reveal.ResetForNight(phases.tasks.CurrentDay);Finishing=false;int index=Mathf.Clamp(phases.tasks.CurrentDay-1,0,introductions.Length-1);
  if(debugAutoSkipAllIntroductions&&(Application.isEditor||Debug.isDebugBuild)){if(phases.tasks.CurrentDay==3)reveal.ApplyCompleted();seen[Mathf.Clamp(phases.tasks.CurrentDay-1,0,2)]=true;phases.BeginNightGameplay();return;}
  if(retry&&skipSeenAutomaticallyOnRetry&&CanSkip){FinishIntroduction();return;}
  if(!phases.ui.TryShowDialogue(introductions[index],FinishIntroduction,HandleEvent))Debug.LogError("Night introduction could not open: another modal or dialogue owns input.");
 }
 IEnumerator HandleEvent(string id){if(id=="glass.fracture")yield return reveal.Fracture();else if(id=="glass.reveal")yield return reveal.Reveal(phases);}
 public void SkipIntroduction(){if(!CanSkip)return;FinishIntroduction();}
 public void DebugSkipIntroduction(){if((Application.isEditor||Debug.isDebugBuild)&&IsIntroducing)FinishIntroduction();}
 public void FinishIntroduction()
 {
  if(!IsIntroducing||Finishing)return;Finishing=true;phases.ui.CancelDialogue();if(phases.tasks.CurrentDay==3)reveal.ApplyCompleted();phases.SetNarrativeFocus(null,0);seen[Mathf.Clamp(phases.tasks.CurrentDay-1,0,2)]=true;StartCoroutine(BeginGameplay());
 }
 IEnumerator BeginGameplay(){yield return new WaitForSecondsRealtime(pauseBeforeGameplay);if(IsIntroducing)phases.BeginNightGameplay();Finishing=false;}
 public void CancelIntroduction(){StopAllCoroutines();if(phases&&phases.ui)phases.ui.CancelDialogue();if(phases)phases.SetNarrativeFocus(null,0);Finishing=false;if(skipLabel)skipLabel.gameObject.SetActive(false);}
 public void DebugStartNight(int day){if(Application.isEditor||Debug.isDebugBuild)phases.DebugBeginIntroduction(day);}
 public void DebugForceReveal(){if(Application.isEditor||Debug.isDebugBuild)reveal.ApplyCompleted();}
}


