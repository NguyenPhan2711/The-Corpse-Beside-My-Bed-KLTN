#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
// Explicit debug convenience; never saved and never included in a player build.
public sealed class DoorNightDebugEntry : MonoBehaviour
{
 [UnityEditor.MenuItem("Bedroom Prototype/Debug/Start Door Test Night (Play mode)")]
 static void Launch(){if(!Application.isPlaying){Debug.LogWarning("Enter Play mode first, then choose Start Door Test Night.");return;}new GameObject("TEMP Door night entry").AddComponent<DoorNightDebugEntry>();}
 IEnumerator Start(){var ai=FindAnyObjectByType<DoorMonsterEncounter>();var p=ai.phases;if(p.State==BedroomPhase.NightIntroduction){p.introductions.DebugSkipIntroduction();while(p.State==BedroomPhase.NightIntroduction)yield return null;ai.ForceStartEncounter();Destroy(gameObject);yield break;}if(p.State==BedroomPhase.NightGameplay){ai.ForceStartEncounter();Destroy(gameObject);yield break;}if(p.State!=BedroomPhase.MorningExploration){Debug.LogWarning("Start this debug shortcut from morning exploration or night.");Destroy(gameObject);yield break;}p.SitAtDesk();while(p.IsTransitioning)yield return null;p.ui.OpenLaptop();for(int i=0;i<p.tasks.Definition.messages.Length;i++){Reply(p);yield return null;}p.ui.CloseModal();p.ui.OpenTruth();for(int i=0;i<p.tasks.Definition.conversation.Length;i++){Reply(p);yield return null;}p.ui.CloseModal();p.StandUp();while(p.IsTransitioning)yield return null;p.ceilingSwitch.SetLight(false);p.GoToBed();while(p.IsTransitioning)yield return null;yield return null;if(p.State==BedroomPhase.NightIntroduction){p.introductions.DebugSkipIntroduction();while(p.State==BedroomPhase.NightIntroduction)yield return null;}ai.ForceStartEncounter();Destroy(gameObject);}
 void Reply(BedroomPhaseController p){p.ui.ActiveButtonRoot.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.gameObject.activeSelf&&b!=p.ui.buttonTemplate).onClick.Invoke();}
}
#endif

