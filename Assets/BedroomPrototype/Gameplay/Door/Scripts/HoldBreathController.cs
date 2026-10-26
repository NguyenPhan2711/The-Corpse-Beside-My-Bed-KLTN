using System;
using UnityEngine;
using UnityEngine.InputSystem;
[DefaultExecutionOrder(-20)]
public sealed class HoldBreathController : MonoBehaviour
{
    public BedroomPhaseController phases;
    public AudioSource breathingSource,gaspSource;
    public AudioClip normalBreathing,heavyBreathing,inhale,gasp;
    public CanvasGroup discomfort;
    [Min(1)]public float maximumCapacity=100;
    [Min(.1f)]public float maximumHoldSeconds=5,recoverySeconds=8;
    [Min(0)]public float forcedGaspSeconds=1,exhaustionLockoutSeconds=1.5f,inhaleSeconds=.2f;
    [Range(0,1)]public float normalVolume=.2f,heavyVolume=.45f,gaspVolume=.9f;
    public InputAction holdAction=new InputAction("Hold breath",InputActionType.Button,"<Keyboard>/r");
    public float BreathRemaining {get;private set;}=100;
    public float Capacity=>BreathRemaining/Mathf.Max(1,maximumCapacity); // Existing normalized API retained.
    public bool IsHolding {get;private set;}
    public bool InputActive {get;private set;}
    public bool IsGasping=>gaspRemaining>0;
    public bool IsInhaling=>inhaleRemaining>0;
    public bool IsQuiet=>phases.CanUseNightDefenses&&IsHolding&&!IsGasping&&!IsInhaling;
    public bool IsRecovering {get;private set;}
    public bool RequiresRelease=>exhaustedRelease||requireRelease;
    public float LockoutRemaining=>lockoutRemaining;
    public int GaspCount {get;private set;}
    public event Action Gasped;
    bool exhaustedRelease,requireRelease;
    float gaspRemaining,lockoutRemaining,inhaleRemaining;
    void Awake(){BreathRemaining=maximumCapacity;holdAction.wantsInitialStateCheck=true;}
    void OnEnable(){if(phases){phases.PhaseChanged+=PhaseChanged;phases.NightPlayerStateChanged+=PlayerStateChanged;if(phases.night)phases.night.Began+=ResetBreath;}}
    void OnDisable(){if(phases){phases.PhaseChanged-=PhaseChanged;phases.NightPlayerStateChanged-=PlayerStateChanged;if(phases.night)phases.night.Began-=ResetBreath;}SuspendBreath();}
    void OnDestroy(){holdAction.Dispose();}
    void PhaseChanged(BedroomPhase state){if(state!=BedroomPhase.NightGameplay){SuspendBreath();if(gaspSource)gaspSource.Stop();}}
    void PlayerStateChanged(NightPlayerState value){requireRelease=Keyboard.current!=null&&Keyboard.current.rKey.isPressed;if(value!=NightPlayerState.InBed)SuspendBreath();else EnableIfAvailable();}
    void EnableIfAvailable(){if(phases&&phases.CanUseNightDefenses&&!requireRelease){holdAction.Enable();InputActive=true;}}
    public void SuspendBreath(){holdAction.Disable();InputActive=false;IsHolding=false;IsRecovering=false;if(breathingSource)breathingSource.Stop();if(discomfort)discomfort.alpha=0;}
    [ContextMenu("Debug reset breath resource")]
    public void ResetBreath(){SuspendBreath();BreathRemaining=maximumCapacity;exhaustedRelease=false;gaspRemaining=lockoutRemaining=inhaleRemaining=0;requireRelease=Keyboard.current!=null&&Keyboard.current.rKey.isPressed;if(gaspSource)gaspSource.Stop();EnableIfAvailable();}
    void StartInhale(){inhaleRemaining=Mathf.Max(inhaleRemaining,inhaleSeconds);if(gaspSource&&inhale)gaspSource.PlayOneShot(inhale,.5f);}
    void Exhaust()
    {
        BreathRemaining=0;IsHolding=false;exhaustedRelease=true;gaspRemaining=forcedGaspSeconds;lockoutRemaining=exhaustionLockoutSeconds;GaspCount++;
        if(breathingSource)breathingSource.Stop();if(gaspSource&&gasp)gaspSource.PlayOneShot(gasp,gaspVolume);Gasped?.Invoke();
    }
    void Update()
    {
        bool running=phases.State==BedroomPhase.NightGameplay&&phases.night.Running;
        bool physical=Keyboard.current!=null&&Keyboard.current.rKey.isPressed;
        if(!physical){requireRelease=false;exhaustedRelease=false;}
        if(!running){SuspendBreath();return;}
        gaspRemaining=Mathf.Max(0,gaspRemaining-Time.deltaTime);lockoutRemaining=Mathf.Max(0,lockoutRemaining-Time.deltaTime);inhaleRemaining=Mathf.Max(0,inhaleRemaining-Time.deltaTime);
        bool allowed=phases.CanUseNightDefenses;
        bool wasHolding=IsHolding;
        if(!allowed){holdAction.Disable();InputActive=false;IsHolding=false;}
        else
        {
            if(!InputActive){holdAction.Enable();InputActive=true;}
            IsHolding=holdAction.IsPressed()&&!requireRelease&&!exhaustedRelease&&!IsGasping&&!IsInhaling&&lockoutRemaining<=0&&BreathRemaining>0;
        }
        if(wasHolding&&!IsHolding&&!IsGasping)StartInhale();
        IsRecovering=false;
        if(IsHolding){BreathRemaining=Mathf.Max(0,BreathRemaining-maximumCapacity/Mathf.Max(.1f,maximumHoldSeconds)*Time.deltaTime);if(BreathRemaining<=0)Exhaust();}
        else if(!physical&&!exhaustedRelease&&!IsGasping&&lockoutRemaining<=0&&BreathRemaining<maximumCapacity){BreathRemaining=Mathf.Min(maximumCapacity,BreathRemaining+maximumCapacity/Mathf.Max(.1f,recoverySeconds)*Time.deltaTime);IsRecovering=true;}
        // Gasp callbacks may end the night synchronously; never restart breathing audio after a loss.
        if(phases.State!=BedroomPhase.NightGameplay||!phases.night.Running){SuspendBreath();return;}
        if(IsQuiet||IsGasping){if(breathingSource)breathingSource.Stop();}
        else if(breathingSource)
        {
            AudioClip clip=Capacity<.25f&&heavyBreathing?heavyBreathing:normalBreathing;
            if(clip){if(breathingSource.clip!=clip||!breathingSource.isPlaying){breathingSource.clip=clip;breathingSource.loop=true;breathingSource.Play();}breathingSource.volume=Mathf.Lerp(heavyVolume,normalVolume,Capacity);breathingSource.pitch=Mathf.Lerp(1.3f,1,Capacity);}
        }
        if(discomfort)discomfort.alpha=allowed?(1-Capacity)*.22f:0;
    }
public void ClearHeldInput() { requireRelease = Keyboard.current != null && Keyboard.current.rKey.isPressed; SuspendBreath(); }
}




