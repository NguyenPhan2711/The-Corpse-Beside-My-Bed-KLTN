using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public sealed class NarrativeDebugHUD:MonoBehaviour
{
 public BedroomPhaseController phases;public NightIntroductionController introductions;public CorpseEncounterController corpse;public BedroomEncounterDirector director;public GameObject panel;public TMP_Text stateText,corpseCue;
 public InputAction toggle=new InputAction("Narrative debug",InputActionType.Button,"<Keyboard>/f6");public bool Visible{get;private set;}
 void Awake(){panel.SetActive(false);}void OnEnable(){toggle.Enable();}void OnDisable(){toggle.Disable();if(phases)phases.SetObservationLock(this,false);}void OnDestroy(){toggle.Dispose();}
 void Update(){bool allowed=Application.isEditor||Debug.isDebugBuild;if(allowed&&toggle.WasPressedThisFrame()){Visible=!Visible;if(Visible)phases.ReleaseCursor();else phases.CaptureCursor();}panel.SetActive(allowed&&Visible);phases.SetObservationLock(this,allowed&&Visible);if(allowed&&Visible)stateText.text="NARRATIVE DEBUG [F6]\nPhase: "+phases.State+" | Night "+phases.tasks.CurrentDay+"\nDirector paused: "+director.IsPaused+"\nDialogue: "+(phases.ui.CurrentDialogue?phases.ui.CurrentDialogue.sequenceId:"none")+" line "+phases.ui.DialogueLineIndex+" | event busy "+phases.ui.DialogueBusy+"\nGlass complete: "+introductions.reveal.Finished+" | same face: "+introductions.reveal.SharedFacialIdentity+"\nCorpse: "+corpse.State+" | paused "+corpse.IsPaused+"\nPull "+corpse.PullProgress.ToString("F2")+" | Q "+corpse.ResistanceProgress.ToString("F2")+"\nFacing "+corpse.FacingCorpse+" | release Q "+corpse.RequiresRelease+"\nCooldown "+corpse.RemainingDormant.ToString("F1")+"s";}
 void CloseForAction(){Visible=false;panel.SetActive(false);phases.SetObservationLock(this,false);phases.CaptureCursor();}
 public void StartNight1(){CloseForAction();introductions.DebugStartNight(1);}public void StartNight2(){CloseForAction();introductions.DebugStartNight(2);}public void StartNight3(){CloseForAction();introductions.DebugStartNight(3);}public void SkipIntro(){CloseForAction();introductions.DebugSkipIntroduction();}public void RevealGlass(){CloseForAction();introductions.DebugForceReveal();}public void ForceCorpse(){CloseForAction();corpse.ForceStartEncounter();}public void ResetCorpse()=>corpse.ResetEncounter();
}



