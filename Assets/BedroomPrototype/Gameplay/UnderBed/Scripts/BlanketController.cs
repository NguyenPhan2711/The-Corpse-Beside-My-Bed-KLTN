using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-25)]
public sealed class BlanketController : MonoBehaviour
{
    public BedroomPhaseController phases;
    public RectTransform overlay;
    [Min(.01f)] public float coverSeconds = .25f;
    [Min(.01f)] public float uncoverSeconds = .22f;
    [Range(0, .2f)] public float uncoveredPressThreshold = .02f;
    public InputAction coverAction = new InputAction("Cover with blanket", InputActionType.Button, "<Keyboard>/space");
    bool requireRelease;
    public bool HoldRequested { get; private set; }
    public float Coverage { get; private set; }
    public bool IsCovered => Coverage >= .999f;
    public bool IsCovering => HoldRequested || Coverage > .001f;
    public bool InputActive { get; private set; }
    public uint PressSequence { get; private set; }
    public bool FreshUncoveredPress { get; private set; }
    public event Action<BlanketController> CoverStarted;
    public event Action<BlanketController> CoverReleased;
    void Awake() { coverAction.wantsInitialStateCheck = true; ApplyOverlay(); }
    void OnEnable() { if (phases != null) { phases.PhaseChanged += PhaseChanged; phases.NightPlayerStateChanged += PlayerStateChanged; } }
    void OnDisable() { if (phases != null) { phases.PhaseChanged -= PhaseChanged; phases.NightPlayerStateChanged -= PlayerStateChanged; } ResetCover(); }
    void OnDestroy() { coverAction.Dispose(); }
    void PhaseChanged(BedroomPhase state) { requireRelease = Keyboard.current != null && Keyboard.current.spaceKey.isPressed; if (state != BedroomPhase.NightGameplay) ResetCover(); }
    void PlayerStateChanged(NightPlayerState value) { requireRelease = Keyboard.current != null && Keyboard.current.spaceKey.isPressed; if (value != NightPlayerState.InBed) ResetCover(); }
    void Update()
    {
        if (requireRelease && (Keyboard.current == null || !Keyboard.current.spaceKey.isPressed)) requireRelease = false;
        bool active = phases.CanUseNightDefenses;
        if (!active) { if (InputActive || IsCovering) ResetCover(); return; }
        if (requireRelease) { if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) return; requireRelease = false; }
        if (!InputActive) { coverAction.Enable(); InputActive = true; }
        bool held = coverAction.IsPressed();
        if (held && !HoldRequested)
        {
            HoldRequested = true; FreshUncoveredPress = Coverage <= uncoveredPressThreshold; PressSequence++;
            CoverStarted?.Invoke(this);
        }
        else if (!held && HoldRequested) { HoldRequested = false; FreshUncoveredPress = false; CoverReleased?.Invoke(this); }
        Coverage = Mathf.MoveTowards(Coverage, held ? 1 : 0, Time.deltaTime / Mathf.Max(.01f, held ? coverSeconds : uncoverSeconds));
        phases.SetObservationLock(this, IsCovering); ApplyOverlay();
    }
    void ApplyOverlay()
    {
        if (overlay == null) return;
        overlay.gameObject.SetActive(Coverage > .001f);
        float height = ((RectTransform)overlay.parent).rect.height;
        overlay.anchoredPosition = new Vector2(0, -(height + 16) * (1 - Mathf.SmoothStep(0, 1, Coverage)));
    }
    public void ResetCover()
    {
        coverAction.Disable(); InputActive = false; HoldRequested = false; Coverage = 0; FreshUncoveredPress = false;
        if (phases != null) phases.SetObservationLock(this, false);
        ApplyOverlay();
    }
public void ClearHeldInput() { requireRelease = Keyboard.current != null && Keyboard.current.spaceKey.isPressed; ResetCover(); }
}





