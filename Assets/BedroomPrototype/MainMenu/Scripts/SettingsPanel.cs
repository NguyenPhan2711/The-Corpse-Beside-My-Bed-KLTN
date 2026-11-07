using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Overlay panel. Each setting is a row under rowsRoot (label + control); add new rows there for volume, sensitivity, etc.
public sealed class SettingsPanel : MonoBehaviour
{
    public Transform rowsRoot;
    public Button backButton;
    [Tooltip("Selected when the panel opens, for keyboard/gamepad navigation.")] public Selectable firstSelected;
    public InputAction closeAction = new InputAction("Close settings", InputActionType.Button, "<Keyboard>/escape");
    public bool IsOpen => gameObject.activeSelf;
    Action closed;
    // The panel starts inactive in the scene, so Awake runs on the first Open.
    void Awake() { backButton.onClick.AddListener(Close); }
    void OnEnable() { closeAction.Enable(); }
    void OnDisable() { closeAction.Disable(); }
    void OnDestroy() { closeAction.Dispose(); }
    void Update() { if (closeAction.WasPressedThisFrame()) Close(); }
    public void Open(Action onClosed) { closed = onClosed; gameObject.SetActive(true); MainMenuController.Select(firstSelected); }
    public void Close()
    {
        if (!IsOpen) return;
        gameObject.SetActive(false); var callback = closed; closed = null; callback?.Invoke();
    }
}
