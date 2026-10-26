using UnityEngine;

public interface IInteractable
{
    string GetPrompt(PlayerInteractor actor);
    bool CanInteract(PlayerInteractor actor);
    void Interact(PlayerInteractor actor);
}

public sealed class BedroomInteractable : MonoBehaviour, IInteractable
{
    public enum Action { SitAtDesk, Laptop, TalkToTruth, GoToBed }
    public Action action;
    public BedroomPhaseController phases;
    public string GetPrompt(PlayerInteractor actor)
    {
        if (action == Action.SitAtDesk) return phases.State == BedroomPhase.MorningExploration ? "[E] Sit at desk" : "";
        if (action == Action.Laptop) return phases.State == BedroomPhase.MorningDesk ? "[E] Interact with laptop" : "";
        if (action == Action.TalkToTruth) return phases.State == BedroomPhase.MorningDesk ? "[E] Talk to The Truth" : "";
        if (action == Action.GoToBed && phases.State == BedroomPhase.NightGameplay && phases.NightStanding) return phases.nightExcursion.ReturnBlockedReason.Length == 0 ? "[E] Return to bed" : phases.nightExcursion.ReturnBlockedReason;
        if (phases.State != BedroomPhase.MorningExploration) return "";
        if (!phases.tasks.BedUnlocked) return "I'm not sleepy yet.";
        return phases.CanStartNight ? "[E] Go to bed" : "I should turn off the light first.";
    }
    public bool CanInteract(PlayerInteractor actor)
    {
        if (phases.IsTransitioning || phases.ui.ModalOpen) return false;
        if (action == Action.SitAtDesk) return phases.State == BedroomPhase.MorningExploration;
        if (action == Action.GoToBed) return (phases.State == BedroomPhase.MorningExploration && phases.CanStartNight) || (phases.State == BedroomPhase.NightGameplay && phases.NightStanding && phases.nightExcursion.ReturnBlockedReason.Length == 0);
        return phases.State == BedroomPhase.MorningDesk;
    }
    public void Interact(PlayerInteractor actor)
    {
        if (!CanInteract(actor)) return;
        switch (action)
        {
            case Action.SitAtDesk: phases.SitAtDesk(); break;
            case Action.Laptop: phases.ui.OpenLaptop(); break;
            case Action.TalkToTruth: phases.ui.OpenTruth(); break;
            case Action.GoToBed: if (phases.State == BedroomPhase.NightGameplay) phases.nightExcursion.TryReturnToBed(); else phases.GoToBed(); break;
        }
    }
}


