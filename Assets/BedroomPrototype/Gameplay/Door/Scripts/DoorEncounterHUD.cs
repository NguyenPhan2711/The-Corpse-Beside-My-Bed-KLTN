using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public sealed class DoorEncounterHUD : MonoBehaviour
{
 public DoorMonsterEncounter monster; public TMP_Text cue,debugText; public GameObject debugPanel;
 public UnityEngine.UI.Button forceButton,resetButton,enableButton;
 public InputAction toggle=new InputAction("Door debug",InputActionType.Button,"<Keyboard>/f4"); bool visible;
 void Awake(){forceButton.onClick.AddListener(()=>monster.ForceStartEncounter());resetButton.onClick.AddListener(monster.ResetEncounter);enableButton.onClick.AddListener(()=>monster.SetEncountersEnabled(!monster.encountersEnabled));debugPanel.SetActive(false);}
 void OnEnable(){toggle.Enable();}void OnDisable(){toggle.Disable();if(monster)monster.phases.SetObservationLock(this,false);}void OnDestroy(){toggle.Dispose();}
 void Update(){bool night=monster.NightActive;bool allowed=Application.isEditor||Debug.isDebugBuild;if(!night)visible=false;if(allowed&&night&&toggle.WasPressedThisFrame()){visible=!visible;if(visible)monster.phases.ReleaseCursor();else monster.phases.CaptureCursor();}debugPanel.SetActive(allowed&&night&&visible);monster.phases.SetObservationLock(this,allowed&&night&&visible);cue.text=monster.Cue;if(visible)debugText.text="DOOR DEBUG [F4]\n"+monster.State+" | listening "+monster.RemainingListening.ToString("F2")+"s\nBreath "+(monster.breath.Capacity*100).ToString("F0")+"% | held "+monster.breath.IsHolding+" | gasp "+monster.breath.IsGasping+"\nLockout "+monster.breath.LockoutRemaining.ToString("F1")+"s | release required "+monster.breath.RequiresRelease+"\nRecovering "+monster.breath.IsRecovering+" | detects breathing "+monster.DetectsBreathing+"\nSuccess "+monster.SuccessfulDefenses+" / failure "+monster.FailedDefenses+"\nLease: "+monster.director.ActiveThreat;}
}

