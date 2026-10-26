using UnityEngine;
public sealed class WindowCloseInteractable : MonoBehaviour,IInteractable
{
 public BedroomWindowPanel panel;
 public string GetPrompt(PlayerInteractor actor)
 {
  if(panel.phases.State!=BedroomPhase.NightGameplay||panel.phases.PlayerState!=NightPlayerState.OutOfBed)return "";
  return panel.CloseBlockedReason.Length==0?"[E] Close window":panel.CloseBlockedReason;
 }
 public bool CanInteract(PlayerInteractor actor)=>panel.CloseBlockedReason.Length==0;
 public void Interact(PlayerInteractor actor){if(CanInteract(actor))panel.TryClose();}
}

