using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum NightPlayerState { InBed, GettingOutOfBed, OutOfBed, ReturningToBed }
public enum BedroomPhase { MorningExploration, MorningDesk, NightGameplay, NightWon, NightLost, NightIntroduction }

public sealed class BedroomPhaseController : MonoBehaviour
{
    public BedroomPlayerController movement;
    public CharacterController capsule;
    public Camera view;
    public PlayerInteractor interactor;
    public MorningTaskManager tasks;
    public PrototypeNightManager night;
    public TimeOfDayLightingController timeOfDayLighting;
    public RoomLightSwitch ceilingSwitch;
    public WindowNightExcursion nightExcursion;
    public Collider nightBedInteractionVolume;
    public NightPlayerState PlayerState { get; private set; } = NightPlayerState.InBed;
    public bool NightStanding => PlayerState == NightPlayerState.OutOfBed;
    public event Action<NightPlayerState> NightPlayerStateChanged;
    public bool IsNightPhase => State == BedroomPhase.NightGameplay || State == BedroomPhase.NightIntroduction;
    public NightIntroductionController introductions; public CorpseEncounterController corpse;
    Transform narrativeFocus; float narrativeFocusBlend; public float narrativeFocusLift=.18f;
    public bool CanUseNightActions => State == BedroomPhase.NightGameplay && PlayerState == NightPlayerState.InBed && !IsTransitioning && night.Running && !ui.ModalOpen && !ui.GameplayInputSuppressed;
    public bool CanUseNightDefenses => (!corpse || !corpse.BlocksOtherDefenses) && State == BedroomPhase.NightGameplay && PlayerState == NightPlayerState.InBed && !IsTransitioning && night.Running && !ui.ModalOpen && !ui.GameplayInputSuppressed;
    void SetPlayerState(NightPlayerState value) { if (PlayerState == value) return; PlayerState = value; if (nightBedInteractionVolume) nightBedInteractionVolume.enabled = value == NightPlayerState.OutOfBed && State == BedroomPhase.NightGameplay; NightPlayerStateChanged?.Invoke(value); }
    Coroutine nightTravel;
    public bool CanStartNight => tasks.BedUnlocked && ceilingSwitch != null && !ceilingSwitch.IsOn;
    public BedroomGameUI ui;
    public Transform morningCameraAnchor, nightCameraAnchor, morningBodyAnchor, nightBodyAnchor, sleepingSpot;
    public Transform bodyModel;
    public Animator bodyAnimator;
    public RuntimeAnimatorController seatedController, lyingController;
    public Collider chairSeat, protagonistPillow;
    public Renderer windowPane;
    [Min(.05f)] public float cameraTransitionSeconds = .65f;
    public float deskYawLimit = 65, deskPitchLimit = 40, nightYawLimit = 165, nightPitchLimit = 65;
    BedroomPhase state = BedroomPhase.MorningExploration;
    public event Action<BedroomPhase> PhaseChanged;
    public BedroomPhase State { get => state; private set { if (state == value) return; state = value; PhaseChanged?.Invoke(value); } }
    readonly HashSet<object> observationLocks = new HashSet<object>();
    public bool IsObservationLocked => observationLocks.Count > 0;
    public void SetObservationLock(object owner, bool locked) { if (locked) observationLocks.Add(owner); else observationLocks.Remove(owner); }
    public bool IsTransitioning { get; private set; }
    Vector3 standingPosition, cameraLocalPosition, modelLocalPosition;
    Quaternion standingRotation, cameraLocalRotation, modelLocalRotation;
    RuntimeAnimatorController developmentController;
    float yaw, pitch;
    void Start()
    {
        cameraLocalPosition = view.transform.localPosition; cameraLocalRotation = view.transform.localRotation;
        modelLocalPosition = bodyModel.localPosition; modelLocalRotation = bodyModel.localRotation;
        developmentController = bodyAnimator.runtimeAnimatorController;
        standingPosition = movement.transform.position; standingRotation = movement.transform.rotation;
        night.Won += OnNightWon;
    }
    void OnDestroy() { if (night != null) night.Won -= OnNightWon; }
    void Update()
    {
        if (IsTransitioning || State == BedroomPhase.MorningExploration || NightStanding) return;
        if (State == BedroomPhase.MorningDesk && interactor.CancelPressed)
        {
            if (ui.ModalOpen) ui.CloseModal(); else StandUp();
            return;
        }
        if ((ui.ModalOpen && !ui.DialogueAllowsLook) || (State != BedroomPhase.MorningDesk && State != BedroomPhase.NightGameplay && State != BedroomPhase.NightIntroduction)) return;
        if (IsNightPhase && IsObservationLocked) return;
        if (interactor.ClickPressed) CaptureCursor();
        if (Cursor.lockState != CursorLockMode.Locked) return;
        var look = interactor.Look * movement.mouseSensitivity;
        yaw = Mathf.Clamp(yaw + look.x, -(State == BedroomPhase.MorningDesk ? deskYawLimit : nightYawLimit), State == BedroomPhase.MorningDesk ? deskYawLimit : nightYawLimit);
        pitch = Mathf.Clamp(pitch - look.y, -(State == BedroomPhase.MorningDesk ? deskPitchLimit : nightPitchLimit), State == BedroomPhase.MorningDesk ? deskPitchLimit : nightPitchLimit);
    }
    void LateUpdate()
    {
        if (IsTransitioning || NightStanding || (State != BedroomPhase.MorningDesk && State != BedroomPhase.NightGameplay && State != BedroomPhase.NightIntroduction)) return;
        var anchor = State == BedroomPhase.MorningDesk ? morningCameraAnchor : nightCameraAnchor;
        view.transform.SetPositionAndRotation(anchor.position, anchor.rotation * Quaternion.Euler(pitch, yaw, 0));
        if (State == BedroomPhase.NightIntroduction && narrativeFocus) { view.transform.position += Vector3.up*(narrativeFocusLift*narrativeFocusBlend); view.transform.rotation = Quaternion.Slerp(view.transform.rotation, Quaternion.LookRotation(narrativeFocus.position-view.transform.position, Vector3.up), narrativeFocusBlend); }
        if (State == BedroomPhase.NightGameplay && corpse) corpse.ApplyCameraEffect(view.transform);
    }
    public void CaptureCursor() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    public void ReleaseCursor() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    public bool SitAtDesk()
    {
        if (State != BedroomPhase.MorningExploration || IsTransitioning || ui.ModalOpen) return false;
        CacheStanding(); StartCoroutine(SitRoutine()); return true;
    }
    void CacheStanding()
    {
        standingPosition = movement.transform.position; standingRotation = movement.transform.rotation;
        cameraLocalPosition = view.transform.localPosition; cameraLocalRotation = view.transform.localRotation;
    }
    void SuspendMovement() { movement.enabled = false; capsule.enabled = false; }
    void SetBody(bool lying)
    {
        movement.transform.SetPositionAndRotation(lying ? sleepingSpot.position : morningBodyAnchor.position, Quaternion.identity);
        bodyAnimator.runtimeAnimatorController = lying ? lyingController : seatedController;
        bodyModel.localPosition = modelLocalPosition;
        bodyModel.localRotation = lying ? Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        bodyAnimator.applyRootMotion = false; bodyAnimator.Rebind(); bodyAnimator.Update(0);
        if (!lying)
        {
            var hips = bodyAnimator.GetBoneTransform(HumanBodyBones.Hips);
            bodyModel.position += new Vector3(morningBodyAnchor.position.x - hips.position.x, chairSeat.bounds.max.y + .10f - hips.position.y, morningBodyAnchor.position.z - hips.position.z);
        }
        else
        {
            var r = bodyAnimator.GetComponentInChildren<SkinnedMeshRenderer>(); var mesh = new Mesh(); r.BakeMesh(mesh);
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue); var max = -min;
            foreach (var v in mesh.vertices) { var p = r.transform.TransformPoint(v); min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
            Destroy(mesh); var center = (min + max) * .5f;
            bodyModel.position += new Vector3(nightBodyAnchor.position.x - center.x, protagonistPillow.bounds.max.y + .01f - min.y, nightBodyAnchor.position.z - center.z);
        }
    }
    IEnumerator SitRoutine()
    {
        IsTransitioning = true; var startP = view.transform.position; var startR = view.transform.rotation;
        SuspendMovement(); SetBody(false); view.transform.SetPositionAndRotation(startP, startR);
        yield return MoveCamera(morningCameraAnchor.position, morningCameraAnchor.rotation);
        yaw = pitch = 0; State = BedroomPhase.MorningDesk; IsTransitioning = false; CaptureCursor();
    }
    public bool StandUp()
    {
        if (State != BedroomPhase.MorningDesk || IsTransitioning || ui.ModalOpen) return false;
        StartCoroutine(StandRoutine()); return true;
    }
    IEnumerator StandRoutine()
    {
        IsTransitioning = true;
        yield return MoveCamera(standingPosition + standingRotation * cameraLocalPosition, standingRotation * cameraLocalRotation);
        RestoreStanding(); State = BedroomPhase.MorningExploration; IsTransitioning = false;
    }
    void RestoreStanding() => RestoreStandingAt(standingPosition, standingRotation);
    void RestoreStandingAt(Vector3 destination, Quaternion rotation, bool enableMovement = true)
    {
        movement.transform.SetPositionAndRotation(destination, rotation);
        bodyModel.localPosition = modelLocalPosition; bodyModel.localRotation = modelLocalRotation;
        bodyAnimator.runtimeAnimatorController = developmentController; bodyAnimator.Rebind(); bodyAnimator.Update(0);
        view.transform.localPosition = cameraLocalPosition; view.transform.localRotation = cameraLocalRotation;
        movement.ResetViewPitch(); Physics.SyncTransforms(); capsule.enabled = enableMovement; movement.enabled = enableMovement; if (enableMovement) CaptureCursor();
    }
    IEnumerator MoveCamera(Vector3 destination, Quaternion rotation)
    {
        var p = view.transform.position; var q = view.transform.rotation; float time = 0;
        while (time < cameraTransitionSeconds) { time += Time.unscaledDeltaTime; float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / cameraTransitionSeconds)); view.transform.SetPositionAndRotation(Vector3.Lerp(p, destination, t), Quaternion.Slerp(q, rotation, t)); yield return null; }
        view.transform.SetPositionAndRotation(destination, rotation);
    }
    public bool GoToBed()
    {
        if (State != BedroomPhase.MorningExploration || IsTransitioning || !CanStartNight || ui.ModalOpen) return false;
        CacheStanding(); StartCoroutine(BedRoutine()); return true;
    }
    IEnumerator BedRoutine()
    {
        IsTransitioning = true;
        SuspendMovement();
        yield return Fade(0, 1);
        SetBody(true);
        yield return MoveCamera(nightCameraAnchor.position, nightCameraAnchor.rotation);
        timeOfDayLighting.EnterNight(); yaw = pitch = 0;
        night.PrepareNight(); State = BedroomPhase.NightIntroduction; yield return Fade(1, 0); IsTransitioning = false; ClearHeldDefenses(); if(introductions)introductions.BeginIntroduction();else BeginNightGameplay(); CaptureCursor();
    }
    IEnumerator Fade(float from, float to)
    {
        float t = 0; while (t < .4f) { t += Time.unscaledDeltaTime; ui.SetFade(Mathf.Lerp(from, to, t / .4f)); yield return null; } ui.SetFade(to);
    }
    void OnNightWon() { ResetNightPosture(); State = BedroomPhase.NightWon; ui.ShowNightResult(true); ReleaseCursor(); }
    public void LoseNight()
    {
        if (State != BedroomPhase.NightGameplay || IsTransitioning) return;
        ResetNightPosture(); night.StopNight(); ui.SetFade(0); State = BedroomPhase.NightLost; ui.ShowNightResult(false); ReleaseCursor();
    }
    public bool LeaveBedAtNight(Transform exit)
    {
        if (State != BedroomPhase.NightGameplay || IsTransitioning || PlayerState != NightPlayerState.InBed || !night.Running) return false;
        nightTravel = StartCoroutine(NightTravelRoutine(exit, false)); return true;
    }
    public bool ReturnToNightBed()
    {
        if (State != BedroomPhase.NightGameplay || IsTransitioning || PlayerState != NightPlayerState.OutOfBed || !night.Running) return false;
        nightTravel = StartCoroutine(NightTravelRoutine(null, true)); return true;
    }
    IEnumerator NightTravelRoutine(Transform exit, bool returning)
    {
        IsTransitioning = true; SetPlayerState(returning ? NightPlayerState.ReturningToBed : NightPlayerState.GettingOutOfBed); SuspendMovement(); yield return Fade(0, 1);
        if (returning) { SetBody(true); view.transform.SetPositionAndRotation(nightCameraAnchor.position, nightCameraAnchor.rotation); yaw = pitch = 0; }
        else { RestoreStandingAt(exit.position, exit.rotation, false); }
        yield return Fade(1, 0); IsTransitioning = false; nightTravel = null;
        if (!returning) { Physics.SyncTransforms(); capsule.enabled = true; movement.enabled = true; }
        SetPlayerState(returning ? NightPlayerState.InBed : NightPlayerState.OutOfBed); CaptureCursor();
    }
    void ResetNightPosture()
    {
        if (PlayerState == NightPlayerState.InBed && nightTravel == null) return;
        if (nightTravel != null) StopCoroutine(nightTravel);
        nightTravel = null; SetPlayerState(NightPlayerState.InBed); IsTransitioning = false; ui.SetFade(0);
        SuspendMovement(); SetBody(true); view.transform.SetPositionAndRotation(nightCameraAnchor.position, nightCameraAnchor.rotation); yaw = pitch = 0;
    }
    public void RetryNight()
    {
        if (State != BedroomPhase.NightLost) return;
        ui.DismissResult(); night.PrepareNight(); State = BedroomPhase.NightIntroduction; ClearHeldDefenses(); if(introductions)introductions.BeginIntroduction(true);else BeginNightGameplay(); CaptureCursor();
    }
    public void NextMorning()
    {
        if (State != BedroomPhase.NightWon || tasks.CurrentDay >= Mathf.Min(3, tasks.content.days.Length) || IsTransitioning) return;
        StartCoroutine(MorningRoutine());
    }
    IEnumerator MorningRoutine()
    {
        IsTransitioning = true; yield return Fade(0, 1); ui.DismissResult(); tasks.BeginNextDay(); RestoreStanding();
        State = BedroomPhase.MorningExploration; yield return Fade(1, 0); IsTransitioning = false;
    }
    public void SetNarrativeFocus(Transform target,float blend){narrativeFocus=target;narrativeFocusBlend=Mathf.Clamp01(blend);}
    public void ClearHeldDefenses(){movement.GetComponent<BlanketController>()?.ClearHeldInput();movement.GetComponent<HoldBreathController>()?.ClearHeldInput();movement.GetComponent<NightFlashlightController>()?.ClearHeldInput();}
    public void BeginNightGameplay(){if(State!=BedroomPhase.NightIntroduction||IsTransitioning)return;State=BedroomPhase.NightGameplay;ClearHeldDefenses();night.BeginNight();CaptureCursor();}
    public void DebugBeginIntroduction(int day)
    {
        if(!(Application.isEditor||Debug.isDebugBuild))return;if(introductions)introductions.CancelIntroduction();StopAllCoroutines();nightTravel=null;IsTransitioning=false;night.StopNight();ui.CancelDialogue();ui.DismissResult();State=BedroomPhase.MorningExploration;tasks.DebugSetDay(day);SuspendMovement();SetBody(true);view.transform.SetPositionAndRotation(nightCameraAnchor.position,nightCameraAnchor.rotation);yaw=pitch=0;SetPlayerState(NightPlayerState.InBed);timeOfDayLighting.EnterNight();night.PrepareNight();State=BedroomPhase.NightIntroduction;ClearHeldDefenses();ui.SetFade(0);introductions.BeginIntroduction();CaptureCursor();
    }
}











