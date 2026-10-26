using System.Collections;
using UnityEngine;
public sealed class WindowNightExcursion : MonoBehaviour, IInteractable
{
    public BedroomPhaseController phases;
    public BedroomWindowPanel panel;
    public WindowMonsterEncounter monster;
    public BedroomEncounterDirector director;
    public Transform standingExit;
    public float nightWalkingSpeed = 1.35f;
    float oldWalk, oldSprint; bool restricted;
    public string LeaveBlockedReason
    {
        get
        {
            if (phases.State != BedroomPhase.NightGameplay) return "Night gameplay is not active.";
            if (phases.IsTransitioning) return "Player transition in progress.";
            if (phases.PlayerState != NightPlayerState.InBed) return "Player is not in bed.";
            if (!panel.IsWindowOpen) return "The window is closed.";
            if (panel.HeadOccupiesOpening || monster.State != WindowMonsterState.Dormant) return "Repel the window monster first.";
            return director.OperationBlockedReason(this);
        }
    }
    public string ReturnBlockedReason
    {
        get
        {
            if (phases.PlayerState != NightPlayerState.OutOfBed) return "Player is not standing beside the bed.";
            return director.OperationBlockedReason(this, true);
        }
    }
    public bool CanLeave => LeaveBlockedReason.Length == 0;
    public string GetPrompt(PlayerInteractor actor) => CanLeave ? "[E] Get out of bed" : panel.IsWindowOpen ? LeaveBlockedReason : "";
    public bool CanInteract(PlayerInteractor actor) => CanLeave;
    public void Interact(PlayerInteractor actor) { TryLeaveBed(); }
    public bool TryLeaveBed()
    {
        if (!CanLeave || !director.TryAcquireOperation(this)) return false;
        if (!phases.LeaveBedAtNight(standingExit)) { director.Release(this); return false; }
        RestrictSpeed(); StartCoroutine(ReleaseAfterTravel()); return true;
    }
    public bool TryReturnToBed()
    {
        if (ReturnBlockedReason.Length != 0 || !director.TryAcquireOperation(this, true)) return false;
        if (!phases.ReturnToNightBed()) { director.Release(this); return false; }
        RestoreSpeed(); StartCoroutine(ReleaseAfterTravel()); return true;
    }
    IEnumerator ReleaseAfterTravel() { while (phases.IsTransitioning && phases.State == BedroomPhase.NightGameplay) yield return null; if (director) director.Release(this); }
    void RestrictSpeed() { if (restricted) return; oldWalk = phases.movement.walkSpeed; oldSprint = phases.movement.sprintSpeed; phases.movement.walkSpeed = phases.movement.sprintSpeed = nightWalkingSpeed; restricted = true; }
    void RestoreSpeed() { if (!restricted) return; if (phases && phases.movement) { phases.movement.walkSpeed = oldWalk; phases.movement.sprintSpeed = oldSprint; } restricted = false; }
    void OnEnable() { if (phases) phases.PhaseChanged += Changed; }
    void OnDisable() { if (phases) phases.PhaseChanged -= Changed; RestoreSpeed(); if (director) director.Release(this); }
    void Changed(BedroomPhase state) { if (state != BedroomPhase.NightGameplay) { RestoreSpeed(); if (director) director.Release(this); } }
}
