using UnityEngine;
using UnityEngine.InputSystem;
[DefaultExecutionOrder(-15)]
public sealed class NightFlashlightController : MonoBehaviour
{
    public BedroomPhaseController phases;public BlanketController blanket;public Light lamp;
    public InputAction toggleAction=new InputAction("Night flashlight",InputActionType.Button,"<Keyboard>/f");
    public bool batteryEnabled=true;
    [Min(1)]public float maximumCharge=100;
    [Min(0)]public float drainPerSecond=12.5f,recoveryPerSecond=5;
    [Range(.01f,1)]public float minimumReactivationFraction=.2f;
    public bool lowBatteryFlicker=true;
    [Range(.01f,1)]public float lowBatteryFraction=.15f;
    [Range(0,.3f)]public float flickerDepth=.12f;
    public AudioSource feedbackSource;public AudioClip emptyBatterySound;
    public float range=8,intensity=2.2f,spotAngle=38,faceToleranceDegrees=7;
    public LayerMask obstructionMask=~0;
    public float Charge {get;private set;}
    public float ChargeFraction=>Charge/Mathf.Max(1,maximumCharge);
    public bool SwitchedOn {get;private set;}
    public bool DepletedLockout {get;private set;}
    public bool IsRecharging {get;private set;}
    public bool IsEmitting=>phases.CanUseNightDefenses&&lamp&&lamp.isActiveAndEnabled&&lamp.intensity>0&&SwitchedOn&&Charge>0&&!DepletedLockout&&!blanket.IsCovering;
    public int DepletionCount {get;private set;}
    bool active,requireRelease;
    void Awake(){Charge=maximumCharge;if(lamp)lamp.enabled=false;}
    void OnEnable(){if(phases){phases.PhaseChanged+=PhaseChanged;phases.NightPlayerStateChanged+=PlayerStateChanged;if(phases.night)phases.night.Began+=ResetLight;}}
    void OnDisable(){if(phases){phases.PhaseChanged-=PhaseChanged;phases.NightPlayerStateChanged-=PlayerStateChanged;if(phases.night)phases.night.Began-=ResetLight;}SuspendLight();}
    void OnDestroy(){toggleAction.Dispose();}
    void PhaseChanged(BedroomPhase phase){if(phase!=BedroomPhase.NightGameplay)SuspendLight();}
    [ContextMenu("Debug reset flashlight resource")]
    public void ResetLight(){SuspendLight();Charge=maximumCharge;DepletedLockout=false;IsRecharging=false;requireRelease=Keyboard.current!=null&&Keyboard.current.fKey.isPressed;EnableIfAvailable();}
    void PlayerStateChanged(NightPlayerState value){requireRelease=Keyboard.current!=null&&Keyboard.current.fKey.isPressed;if(value!=NightPlayerState.InBed)SuspendLight();else EnableIfAvailable();}
    void EnableIfAvailable(){if(phases&&phases.CanUseNightDefenses&&!requireRelease){toggleAction.Enable();active=true;}}
    public void SuspendLight(){toggleAction.Disable();active=false;SwitchedOn=false;IsRecharging=false;if(lamp)lamp.enabled=false;}
    void Deplete(){Charge=0;SwitchedOn=false;DepletedLockout=true;DepletionCount++;if(lamp)lamp.enabled=false;if(feedbackSource&&emptyBatterySound)feedbackSource.PlayOneShot(emptyBatterySound);}
    void Update()
    {
        bool running=phases.State==BedroomPhase.NightGameplay&&phases.night.Running;
        bool allowed=running&&phases.CanUseNightDefenses;
        if(requireRelease&&(Keyboard.current==null||!Keyboard.current.fKey.isPressed))requireRelease=false;
        if(!allowed)SuspendLight();
        else if(!requireRelease)
        {
            if(!active){toggleAction.Enable();active=true;}
            if(toggleAction.WasPressedThisFrame())
            {
                if(SwitchedOn)SwitchedOn=false;
                else if(!batteryEnabled||(Charge>0&&!DepletedLockout))SwitchedOn=true;
            }
        }
        IsRecharging=false;
        if(running&&batteryEnabled)
        {
            // Switched-on drain continues under the blanket; covering cannot recharge an enabled torch.
            if(SwitchedOn){Charge=Mathf.Max(0,Charge-drainPerSecond*Time.deltaTime);if(Charge<=0)Deplete();}
            else if(Charge<maximumCharge){Charge=Mathf.Min(maximumCharge,Charge+recoveryPerSecond*Time.deltaTime);IsRecharging=recoveryPerSecond>0;}
            if(DepletedLockout&&Charge>=maximumCharge*minimumReactivationFraction)DepletedLockout=false;
        }
        if(!batteryEnabled)DepletedLockout=false;
        lamp.range=range;lamp.spotAngle=spotAngle;lamp.innerSpotAngle=spotAngle*.65f;
        float flicker=lowBatteryFlicker&&ChargeFraction<lowBatteryFraction?1-flickerDepth*(.5f+.5f*Mathf.Sin(Time.time*47)*Mathf.Sin(Time.time*29)):1;
        lamp.intensity=intensity*flicker;
        lamp.enabled=allowed&&SwitchedOn&&Charge>0&&!DepletedLockout&&!blanket.IsCovering;
    }
    public bool Illuminates(Collider face)
    {
        if(!IsEmitting||!face||!face.enabled)return false;
        Vector3 delta=face.bounds.center-phases.view.transform.position;
        if(delta.magnitude>range||Vector3.Angle(phases.view.transform.forward,delta)>Mathf.Min(faceToleranceDegrees,spotAngle*.45f))return false;
        var hits=Physics.RaycastAll(phases.view.transform.position,delta.normalized,delta.magnitude+.2f,obstructionMask,QueryTriggerInteraction.Collide);
        System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits){if(hit.transform.IsChildOf(phases.movement.transform))continue;if(hit.collider==face)return true;if(!hit.collider.isTrigger)return false;}return false;
    }
public void ClearHeldInput() { requireRelease = Keyboard.current != null && Keyboard.current.fKey.isPressed; SuspendLight(); }
}




