#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
public sealed class WindowNightDebugEntry : MonoBehaviour
{
 [UnityEditor.MenuItem("Bedroom Prototype/Debug/Start Window Test Night (Play mode)")]
 static void Launch(){if(!Application.isPlaying){Debug.LogWarning("Enter Play mode before starting the window test night.");return;}new GameObject("TEMP Window night entry").AddComponent<WindowNightDebugEntry>();}
 IEnumerator Start(){var ai=FindAnyObjectByType<WindowMonsterEncounter>();var p=ai.phases;if(p.State==BedroomPhase.NightIntroduction){p.introductions.DebugSkipIntroduction();while(p.State==BedroomPhase.NightIntroduction)yield return null;ai.ForceStartEncounter();Destroy(gameObject);yield break;}if(p.State==BedroomPhase.NightGameplay){ai.ForceStartEncounter();Destroy(gameObject);yield break;}if(p.State!=BedroomPhase.MorningExploration){Destroy(gameObject);yield break;}p.SitAtDesk();while(p.IsTransitioning)yield return null;p.ui.OpenLaptop();while(p.tasks.Definition.messages.Any(m=>!p.tasks.Current.repliedMessages.Contains(m.id))){Reply(p);yield return null;}p.ui.CloseModal();p.ui.OpenTruth();while(p.tasks.Current.conversationStep<p.tasks.Definition.conversation.Length){Reply(p);yield return null;}p.ui.CloseModal();p.StandUp();while(p.IsTransitioning)yield return null;p.ceilingSwitch.SetLight(false);p.GoToBed();while(p.IsTransitioning)yield return null;yield return null;if(p.State==BedroomPhase.NightIntroduction){p.introductions.DebugSkipIntroduction();while(p.State==BedroomPhase.NightIntroduction)yield return null;}ai.ForceStartEncounter();Destroy(gameObject);}
 void Reply(BedroomPhaseController p){p.ui.ActiveButtonRoot.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.gameObject.activeSelf&&b!=p.ui.buttonTemplate).onClick.Invoke();}
}
#endif

