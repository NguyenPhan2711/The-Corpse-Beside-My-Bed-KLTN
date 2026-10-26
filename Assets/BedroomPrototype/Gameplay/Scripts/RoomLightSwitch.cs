using System;
using UnityEngine;

public sealed class RoomLightSwitch : MonoBehaviour, IInteractable
{
    public BedroomPhaseController phases;
    public Light ceilingLight;
    public Transform rocker;
    public bool initiallyOn;
    [Min(0)] public float onIntensity = 3f;
    public Color onColor = new Color(1f, .89f, .73f);
    [SerializeField] bool isOn;
    public bool IsOn => isOn;
    public event Action<bool> Changed;
    void Awake() { SetLight(initiallyOn); }
    public string GetPrompt(PlayerInteractor actor)
    {
        if (phases.State != BedroomPhase.MorningExploration) return "";
        return isOn ? "[E] Turn off light" : "[E] Turn on light";
    }
    public bool CanInteract(PlayerInteractor actor) => phases.State == BedroomPhase.MorningExploration && !phases.IsTransitioning && !phases.ui.ModalOpen;
    public void Interact(PlayerInteractor actor) { if (CanInteract(actor)) SetLight(!isOn); }
    public void SetLight(bool value)
    {
        bool changed = isOn != value; isOn = value;
        if (ceilingLight != null) { ceilingLight.color = onColor; ceilingLight.intensity = onIntensity; ceilingLight.enabled = value; }
        if (rocker != null) rocker.localRotation = Quaternion.Euler(value ? -12 : 12, 0, 0);
        if (changed) Changed?.Invoke(value);
        if (phases != null && phases.interactor != null && Application.isPlaying) phases.interactor.RefreshTarget();
    }
}
