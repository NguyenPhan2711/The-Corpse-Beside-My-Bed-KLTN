using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class UnderBedEncounterHUD : MonoBehaviour
{
    public UnderBedMonsterAI monster;
    public BlanketController blanket;
    public TMP_Text cue, debugText;
    public GameObject debugPanel;
    public UnityEngine.UI.Button forceButton, resetButton, enableButton;
    public bool developmentDebugControls = true;
    public bool allowDebugInReleaseBuild;
    public InputAction toggleDebug = new InputAction("Toggle under-bed debug", InputActionType.Button, "<Keyboard>/f3");
    bool visible;
    void Awake()
    {
        forceButton.onClick.AddListener(() => monster.ForceStartEncounter());
        resetButton.onClick.AddListener(monster.ResetEncounter);
        enableButton.onClick.AddListener(() => monster.SetEncountersEnabled(!monster.encountersEnabled));
        debugPanel.SetActive(false);
    }
    void OnEnable() { toggleDebug.Enable(); }
    void OnDisable() { toggleDebug.Disable(); if (monster != null) monster.phases.SetObservationLock(this, false); }
    void OnDestroy() { toggleDebug.Dispose(); }
    void Update()
    {
        bool night = monster.phases.State == BedroomPhase.NightGameplay && !monster.phases.IsTransitioning;
        if (!night) visible = false;
        cue.text = night ? monster.Cue : "";
        bool allowed = developmentDebugControls && (Application.isEditor || Debug.isDebugBuild || allowDebugInReleaseBuild);
        if (allowed && night && toggleDebug.WasPressedThisFrame()) { visible = !visible; if (visible) monster.phases.ReleaseCursor(); else monster.phases.CaptureCursor(); }
        debugPanel.SetActive(allowed && night && visible);
        monster.phases.SetObservationLock(this, allowed && night && visible);
        if (allowed && night && visible)
            debugText.text = "UNDER-BED DEBUG [F3]\nState: " + monster.State + "\nReaction: " + monster.RemainingReaction.ToString("F2") + "s  Dormant: " + monster.RemainingDormant.ToString("F1") + "s\nBlanket: " + (blanket.IsCovered ? "covered" : blanket.IsCovering ? "moving" : "down") + "  Held: " + blanket.HoldRequested + "\nDefended: " + monster.SuccessfulDefenses + "  Failed: " + monster.FailedDefenses;
    }
}
