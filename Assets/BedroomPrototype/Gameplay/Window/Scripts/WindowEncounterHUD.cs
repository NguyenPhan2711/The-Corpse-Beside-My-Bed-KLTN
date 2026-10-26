using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public sealed class WindowEncounterHUD : MonoBehaviour
{
 public WindowMonsterEncounter monster;public TMP_Text cue,debugText;public GameObject debugPanel;
 public UnityEngine.UI.Button forceButton,retreatButton,openButton,resetButton;
 public InputAction toggle=new InputAction("Window debug",InputActionType.Button,"<Keyboard>/f5");bool visible;
 void Awake(){forceButton.onClick.AddListener(()=>monster.ForceStartEncounter());retreatButton.onClick.AddListener(monster.ForceRetreat);openButton.onClick.AddListener(()=>{if(monster.NightActive&&monster.State==WindowMonsterState.Dormant&&!monster.director.HasActiveThreat)monster.panel.SetOpenAmount(1);});resetButton.onClick.AddListener(()=>{monster.ResetEncounter();monster.panel.ResetClosed();});debugPanel.SetActive(false);}
 void OnEnable(){toggle.Enable();}void OnDisable(){toggle.Disable();if(monster)monster.phases.SetObservationLock(this,false);}void OnDestroy(){toggle.Dispose();}
 void Update(){bool night=monster.phases.State==BedroomPhase.NightGameplay;bool allowed=Application.isEditor||Debug.isDebugBuild;if(!night)visible=false;if(allowed&&night&&toggle.WasPressedThisFrame()){visible=!visible;if(visible)monster.phases.ReleaseCursor();else monster.phases.CaptureCursor();}debugPanel.SetActive(allowed&&night&&visible);monster.phases.SetObservationLock(this,visible&&allowed&&night);cue.text=monster.Cue;if(visible)debugText.text="WINDOW DEBUG [F5]\n"+monster.State+" | reaction "+monster.RemainingReaction.ToString("F2")+"s\nDanger x"+monster.DangerMultiplier.ToString("F1")+" | open "+monster.panel.OpenSeconds.ToString("F1")+"s\nBattery "+(monster.flashlight.ChargeFraction*100).ToString("F0")+"% | on "+monster.flashlight.SwitchedOn+"\nRecharging "+monster.flashlight.IsRecharging+" | depleted lock "+monster.flashlight.DepletedLockout+"\nHead hit "+monster.flashlight.Illuminates(monster.faceTarget)+"\nExposure "+monster.Exposure.ToString("F2")+" | away "+monster.phases.NightStanding;}
}

