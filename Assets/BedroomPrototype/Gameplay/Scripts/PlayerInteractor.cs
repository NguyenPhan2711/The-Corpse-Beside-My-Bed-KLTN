using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)]
public sealed class PlayerInteractor : MonoBehaviour
{
    public Camera view;
    public BedroomPhaseController phases;
    public InputActionAsset inputActions;
    [Min(.1f)] public float interactionDistance = 2f;
    public LayerMask raycastMask = ~0;
    public bool logInteractions;
    InputActionAsset runtime;
    InputAction interact, look, cancel, click;
    public IInteractable CurrentTarget { get; private set; }
    public string CurrentPrompt { get; private set; } = "";
    public Collider CurrentCollider { get; private set; }
    public float TargetDistance { get; private set; }
    public string ResolvedTargetName { get; private set; } = "None";
    public string BlockedReason { get; private set; } = "";
    public int ExecutedInteractions { get; private set; }
    public int ReceivedPresses { get; private set; }
    public bool InteractionInputEnabled => interact != null && interact.enabled;
    public Vector2 Look => look == null ? Vector2.zero : look.ReadValue<Vector2>();
    public bool CancelPressed => cancel != null && cancel.WasPressedThisFrame();
    public bool ClickPressed => click != null && click.WasPressedThisFrame();
    string feedback;
    float feedbackUntil;
    int lastPressFrame = -1;
    void Awake()
    {
        runtime = Instantiate(inputActions); runtime.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
        interact = runtime.FindAction("Player/Interact", true);
        // Keep the imported action asset intact.
        // WasPressedThisFrame reacts to button-down independently of the existing Hold interaction.
        look = runtime.FindAction("Player/Look", true);
        cancel = runtime.FindAction("UI/Cancel", true); click = runtime.FindAction("UI/Click", true);
    }
    void OnEnable() { if (runtime != null) runtime.Enable(); }
    void OnDisable() { if (runtime != null) runtime.Disable(); }
    void OnDestroy() { if (runtime != null) Destroy(runtime); }
    void Update()
    {
        RefreshTarget();
        if (interact != null && interact.WasPressedThisFrame() && lastPressFrame != Time.frameCount)
        {
            lastPressFrame = Time.frameCount; ReceivedPresses++;
            var target = CurrentTarget;
            bool allowed = target != null && !phases.IsTransitioning && !phases.ui.ModalOpen && !phases.ui.GameplayInputSuppressed && target.CanInteract(this);
            string reason = allowed ? "" : BlockedReason.Length > 0 ? BlockedReason : "No usable interaction target.";
            if (logInteractions && (Application.isEditor || Debug.isDebugBuild))
                Debug.Log("[Interaction] E pressed=True | State=" + phases.PlayerState + " | Phase=" + phases.State + " | Collider=" + (CurrentCollider ? CurrentCollider.name : "None") + " | Target=" + ResolvedTargetName + " | Distance=" + TargetDistance.ToString("F2") + " | Allowed=" + allowed + " | Reason=" + reason);
            if (allowed) { ExecutedInteractions++; target.Interact(this); }
            else { feedback = reason; feedbackUntil = Time.unscaledTime + 1.2f; }
            RefreshTarget(); // The same press never resolves or executes a second target.
        }
        if (CurrentTarget == null && Time.unscaledTime < feedbackUntil) CurrentPrompt = feedback;
    }
    string UnavailableReason(IInteractable target)
    {
        if (target is WindowCloseInteractable window) return window.panel.CloseBlockedReason;
        if (target is WindowNightExcursion excursion) return excursion.LeaveBlockedReason;
        if (target is BedroomInteractable bed && bed.action == BedroomInteractable.Action.GoToBed && phases.State == BedroomPhase.NightGameplay) return phases.nightExcursion.ReturnBlockedReason;
        return target.GetPrompt(this);
    }
    void Resolve(IInteractable target)
    {
        CurrentTarget = target; ResolvedTargetName = target is Component component ? component.name + " (" + component.GetType().Name + ")" : target.GetType().Name;
        bool allowed = target.CanInteract(this);
        BlockedReason = allowed ? "" : UnavailableReason(target);
        CurrentPrompt = allowed ? target.GetPrompt(this) : BlockedReason;
    }
    public void RefreshTarget()
    {
        CurrentTarget = null; CurrentCollider = null; CurrentPrompt = BlockedReason = ""; TargetDistance = 0; ResolvedTargetName = "None";
        if (phases == null) return;
        if (phases.IsTransitioning) { BlockedReason = "Player transition in progress."; return; }
        if (phases.ui.ModalOpen || phases.ui.GameplayInputSuppressed) { BlockedReason = "Close the current dialogue first."; return; }
        bool night = phases.State == BedroomPhase.NightGameplay;
        if (night && phases.PlayerState == NightPlayerState.InBed)
        {
            if (phases.nightExcursion != null && phases.nightExcursion.panel.IsWindowOpen) Resolve(phases.nightExcursion);
            else BlockedReason = "No bed interaction is available; the window is closed.";
            return;
        }
        if (!night && phases.State != BedroomPhase.MorningExploration && phases.State != BedroomPhase.MorningDesk) { BlockedReason = "Gameplay interaction is unavailable."; return; }
        var hits = Physics.RaycastAll(view.transform.position, view.transform.forward, interactionDistance + 2f, raycastMask, QueryTriggerInteraction.Collide)
            .Where(h => !h.transform.IsChildOf(transform)).OrderBy(h => h.distance)
            .ThenBy(h => h.collider.isTrigger ? 0 : 1);
        foreach (var hit in hits)
        {
            var target = hit.collider.GetComponentsInParent<MonoBehaviour>().OfType<IInteractable>().FirstOrDefault();
            bool usable = target != null && (!night || target is WindowCloseInteractable || (target is BedroomInteractable bed && bed.action == BedroomInteractable.Action.GoToBed));
            if (usable && !string.IsNullOrEmpty(target.GetPrompt(this)))
            {
                CurrentCollider = hit.collider; TargetDistance = hit.distance;
                if (hit.distance > interactionDistance) { ResolvedTargetName = ((Component)target).name + " (" + target.GetType().Name + ")"; BlockedReason = "Player too far away."; return; }
                Resolve(target); return;
            }
            if (!hit.collider.isTrigger) { CurrentCollider = hit.collider; TargetDistance = hit.distance; BlockedReason = "Interaction target obstructed by " + hit.collider.name + "."; return; }
        }
        BlockedReason = "No interaction target in view.";
    }
}






