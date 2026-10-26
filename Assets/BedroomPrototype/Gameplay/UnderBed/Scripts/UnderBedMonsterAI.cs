using System;
using UnityEngine;

public enum UnderBedState { Dormant, Scratching, Emerging, FullyExposed, PlayerHiding, Retreat, Attack }
public enum UnderBedOutcome { Canceled, Defended, Failed }

// A future shared director can implement this contract; no other threat locations are created here.
public interface IMonsterEncounterCoordinator
{
    bool TryStartEncounter(UnderBedMonsterAI encounter);
    void EncounterEnded(UnderBedMonsterAI encounter, UnderBedOutcome outcome);
}

[DefaultExecutionOrder(25)]
public sealed class UnderBedMonsterAI : MonoBehaviour
{
    public BedroomPhaseController phases;
    public BlanketController blanket;
    public UnderBedEncounterConfig[] nightConfigs;
    public Transform visualRoot, head, hiddenPosition, emergingPosition, exposedPosition, attackPosition;
    public Renderer[] handRenderers, headRenderers, bodyRenderers, legRenderers;
    public AudioSource scratchingSource, emergingSource, exposedSource, breathingSource, retreatSource, safeCueSource, attackSource;
    [Tooltip("Optional future director implementing IMonsterEncounterCoordinator.")] public MonoBehaviour encounterCoordinator;
    public bool encountersEnabled = true;
    public bool attacksEnabled = true;
    public bool preserveAttackTailOnLoss = true;
    [SerializeField] UnderBedState currentState = UnderBedState.Dormant;
    [SerializeField] float stateTime, dormantRemaining, hiddenHoldTime;
    public UnderBedState State => currentState;
    public float StateElapsed => stateTime;
    public UnderBedEncounterConfig CurrentConfig => nightConfigs == null || nightConfigs.Length == 0 ? null : nightConfigs[Mathf.Clamp(phases.tasks.CurrentDay - 1, 0, nightConfigs.Length - 1)];
    public float RemainingReaction => State == UnderBedState.FullyExposed ? Mathf.Max(0, (float)(reactionDeadline - Time.timeAsDouble)) : 0;
    public float RemainingDormant => dormantRemaining;
    public float HiddenHoldTime => hiddenHoldTime;
    public bool SafeToUncover { get; private set; }
    public int SuccessfulDefenses { get; private set; }
    public int FailedDefenses { get; private set; }
    public event Action<UnderBedState> StateChanged;
    public event Action<UnderBedMonsterAI> EncounterBegan, DefenseSucceeded, DefenseFailed;
    public event Action<UnderBedMonsterAI, UnderBedOutcome> EncounterEnded;
    public event Action SafeToUncoverCue;
    bool sessionActive, encounterActive;
    int releaseWarnings;
    float safeCueUntil;
    double reactionDeadline;
    uint exposureSequence;
    Vector3 retreatStart, attackStart, attackGoal, retreatScale;
    Quaternion retreatRotation, attackRotation, attackGoalRotation, headStartRotation;
    IMonsterEncounterCoordinator Coordinator => encounterCoordinator as IMonsterEncounterCoordinator;
    bool NightActive => phases.State == BedroomPhase.NightGameplay && phases.night.Running;
    void OnEnable()
    {
        if (blanket != null) { blanket.CoverStarted += OnCoverStarted; blanket.CoverReleased += OnCoverReleased; }
        if (phases != null) phases.PhaseChanged += OnPhaseChanged;
    }
    void Start() { HideVisual(); }
    void OnDisable()
    {
        if (blanket != null) { blanket.CoverStarted -= OnCoverStarted; blanket.CoverReleased -= OnCoverReleased; }
        if (phases != null) phases.PhaseChanged -= OnPhaseChanged;
        StopSession(); if (attackSource) attackSource.Stop();
    }
    void OnPhaseChanged(BedroomPhase state) { if (state != BedroomPhase.NightGameplay) StopSession(); }
    void Update()
    {
        var config = CurrentConfig;
        if (!NightActive || !encountersEnabled || config == null || !config.encounterEnabled) { if (sessionActive || encounterActive) StopSession(); return; }
        if (!sessionActive) { sessionActive = true; ResetEncounter(); }
        stateTime += Time.deltaTime;
        switch (State)
        {
            case UnderBedState.Dormant:
                dormantRemaining -= Time.deltaTime;
                if (dormantRemaining <= 0 && !(encounterCoordinator is BedroomEncounterDirector)) TryBegin();
                break;
            case UnderBedState.Scratching:
                if (stateTime >= config.scratchingDuration) ChangeState(UnderBedState.Emerging);
                break;
            case UnderBedState.Emerging:
                AnimateEmergence(Mathf.Clamp01(stateTime / config.emergingDuration));
                if (stateTime >= config.emergingDuration && (!(encounterCoordinator is BedroomEncounterDirector sharedDirector) || sharedDirector.TryBeginDefense(this))) Expose(config.reactionWindow);
                break;
            case UnderBedState.FullyExposed:
                if (encounterCoordinator is BedroomEncounterDirector director && director.DeferBedAttack(this)) { reactionDeadline += Time.deltaTime; break; }
                if (phases.PlayerState != NightPlayerState.InBed) { FailDefense("Return-to-bed grace expired"); break; }
                if (Time.timeAsDouble >= reactionDeadline) FailDefense("Missed the exposed reaction window");
                break;
            case UnderBedState.PlayerHiding:
                if (blanket.IsCovered && blanket.HoldRequested) hiddenHoldTime += Time.deltaTime;
                // Retreat occupies the final part of the required hold: default total is about 3 seconds, not 3+2.
                if (hiddenHoldTime >= Mathf.Max(0, config.requiredHidingDuration - config.retreatDuration)) BeginRetreat();
                break;
            case UnderBedState.Retreat:
                if (blanket.IsCovered && blanket.HoldRequested) hiddenHoldTime += Time.deltaTime;
                AnimateRetreat(Mathf.Clamp01(stateTime / config.retreatDuration));
                if (stateTime >= config.retreatDuration && hiddenHoldTime >= config.requiredHidingDuration) Succeed();
                break;
            case UnderBedState.Attack:
                if (!attacksEnabled) { Finish(UnderBedOutcome.Failed); ReturnDormant(); break; }
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(stateTime / config.attackDuration));
                visualRoot.SetPositionAndRotation(Vector3.Lerp(attackStart, attackGoal, t), Quaternion.Slerp(attackRotation, attackGoalRotation, t));
                if (stateTime >= config.attackDuration) { Finish(UnderBedOutcome.Failed); phases.LoseNight(); }
                break;
        }
    }
    void LateUpdate()
    {
        if (State == UnderBedState.Attack && attackSource) attackSource.transform.position = head.position;
        if (State != UnderBedState.FullyExposed || !NightActive) return;
        var target = Quaternion.LookRotation((phases.view.transform.position - head.position).normalized, Vector3.up);
        head.rotation = Quaternion.Slerp(headStartRotation, target, Mathf.Clamp01(stateTime / CurrentConfig.headTurnDuration));
    }
    bool TryBegin(bool forced = false)
    {
        if (!NightActive || !encountersEnabled || CurrentConfig == null || !CurrentConfig.encounterEnabled) return false;
        if (Coordinator != null && !(forced && encounterCoordinator is BedroomEncounterDirector shared ? shared.TryAcquire(this) : Coordinator.TryStartEncounter(this))) { dormantRemaining = CurrentConfig.directorRetryDelay; return false; }
        encounterActive = true; SafeToUncover = false; hiddenHoldTime = 0; releaseWarnings = 0;
        ChangeState(UnderBedState.Scratching); HideVisual(); StopAudio(); Play(scratchingSource, CurrentConfig.scratching, CurrentConfig.loopScratching);
        EncounterBegan?.Invoke(this); return true;
    }
    void ChangeState(UnderBedState value)
    {
        currentState = value; stateTime = 0; StateChanged?.Invoke(value);
        if (value == UnderBedState.Emerging) { Stop(scratchingSource); Play(emergingSource, CurrentConfig.emerging); }
    }
    void Expose(float window)
    {
        AnimateEmergence(1); ChangeState(UnderBedState.FullyExposed); reactionDeadline = Time.timeAsDouble + window;
        if (encounterCoordinator is BedroomEncounterDirector director) director.BeginBedDefense(this);
        exposureSequence = blanket.PressSequence; headStartRotation = head.rotation;
        Stop(scratchingSource); Stop(emergingSource); Play(exposedSource, CurrentConfig.exposed); Play(breathingSource, CurrentConfig.breathing, true);
    }
    void OnCoverStarted(BlanketController cover)
    {
        if (!NightActive || State != UnderBedState.FullyExposed || Time.timeAsDouble >= reactionDeadline) return;
        if (cover.PressSequence <= exposureSequence || !cover.FreshUncoveredPress) return;
        hiddenHoldTime = 0; ChangeState(UnderBedState.PlayerHiding);
    }
    void OnCoverReleased(BlanketController cover)
    {
        if (!NightActive || (State != UnderBedState.PlayerHiding && State != UnderBedState.Retreat)) return;
        if (CurrentConfig.earlyReleaseRule == UnderBedEncounterConfig.EarlyReleaseRule.OneWarning && releaseWarnings == 0)
        {
            releaseWarnings++; hiddenHoldTime = 0; Expose(CurrentConfig.warningReactionWindow);
            if (CurrentConfig.logDefenses) Debug.Log("Under-bed warning: blanket released early. Uncover fully and time a fresh hold.");
        }
        else FailDefense("Released blanket before the retreat cue");
    }
    void BeginRetreat()
    {
        retreatStart = visualRoot.position; retreatRotation = visualRoot.rotation; retreatScale = visualRoot.localScale;
        ChangeState(UnderBedState.Retreat); Stop(breathingSource); Play(retreatSource, CurrentConfig.retreating);
    }
    void Succeed()
    {
        SafeToUncover = true; safeCueUntil = Time.time + 3; SuccessfulDefenses++;
        Stop(retreatSource); Play(safeCueSource, CurrentConfig.safeToUncover);
        SafeToUncoverCue?.Invoke(); DefenseSucceeded?.Invoke(this);
        if (CurrentConfig.logDefenses) Debug.Log("Under-bed defense succeeded. Safe to uncover.");
        Finish(UnderBedOutcome.Defended); ReturnDormant();
    }
    void FailDefense(string reason)
    {
        FailedDefenses++; DefenseFailed?.Invoke(this);
        if (CurrentConfig.logDefenses) Debug.Log("Under-bed defense failed: " + reason);
        if (!attacksEnabled) { Finish(UnderBedOutcome.Failed); ReturnDormant(); return; }
        attackStart = visualRoot.position; attackRotation = visualRoot.rotation;
        attackGoalRotation = Quaternion.LookRotation(-phases.view.transform.forward, Vector3.up);
        var headLocal = visualRoot.InverseTransformPoint(head.position);
        attackGoal = attackPosition.position - attackGoalRotation * Vector3.Scale(headLocal, Vector3.one);
        visualRoot.localScale = Vector3.one; SetVisible(true, true, true, true);
        phases.SetObservationLock(this, true); ChangeState(UnderBedState.Attack); StopAudio(); Play(attackSource, CurrentConfig.attacking);
    }
    void ReturnDormant()
    {
        ChangeState(UnderBedState.Dormant); HideVisual(); phases.SetObservationLock(this, false);
        dormantRemaining = encounterCoordinator is BedroomEncounterDirector scheduler ? scheduler.SampleCooldown(CurrentConfig.cooldownRange) : UnityEngine.Random.Range(Mathf.Max(0, CurrentConfig.cooldownRange.x), Mathf.Max(CurrentConfig.cooldownRange.x, CurrentConfig.cooldownRange.y));
    }
    void Finish(UnderBedOutcome outcome)
    {
        if (!encounterActive) return; encounterActive = false;
        Coordinator?.EncounterEnded(this, outcome); EncounterEnded?.Invoke(this, outcome);
    }
    void StopSession()
    {
        bool keepAttack = preserveAttackTailOnLoss && phases && phases.State == BedroomPhase.NightLost && State == UnderBedState.Attack;
        Vector3 attackAudioPosition = attackSource ? attackSource.transform.position : Vector3.zero;
        if (encounterCoordinator is BedroomEncounterDirector shared) shared.CancelRequest(this);
        Finish(UnderBedOutcome.Canceled); sessionActive = false; currentState = UnderBedState.Dormant; stateTime = 0; SafeToUncover = false;
        HideVisual(); StopAudio(keepAttack ? attackSource : null); if (phases != null) phases.SetObservationLock(this, false);
        if (keepAttack && attackSource) attackSource.transform.position = attackAudioPosition;
    }
    public bool StartScheduledEncounter() { if (State != UnderBedState.Dormant) return false; sessionActive = true; return TryBegin(true); }
    public bool ForceStartEncounter()
    {
        if (!Application.isPlaying || !NightActive || !encountersEnabled || CurrentConfig == null) return false;
        if (!sessionActive) sessionActive = true;
        ResetEncounter(); return TryBegin(true);
    }
    [ContextMenu("Force encounter (Play mode night only)")]
    void DebugForceStart() { ForceStartEncounter(); }
    [ContextMenu("Reset encounter")]
    public void ResetEncounter()
    {
        Finish(UnderBedOutcome.Canceled); ChangeState(UnderBedState.Dormant); HideVisual(); StopAudio();
        SafeToUncover = false; hiddenHoldTime = 0; releaseWarnings = 0;
        dormantRemaining = encounterCoordinator is BedroomEncounterDirector ? 0 : CurrentConfig == null ? 15 : CurrentConfig.initialDelay;
        if (phases != null) phases.SetObservationLock(this, false);
    }
    public void SetEncountersEnabled(bool value) { encountersEnabled = value; if (!value) StopSession(); }
    void AnimateEmergence(float t)
    {
        float slide = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .28f));
        float rise = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.25f, 1, t));
        visualRoot.position = t < .28f ? Vector3.Lerp(hiddenPosition.position, emergingPosition.position, slide) : Vector3.Lerp(emergingPosition.position, exposedPosition.position, rise);
        visualRoot.rotation = Quaternion.Slerp(emergingPosition.rotation, exposedPosition.rotation, rise);
        visualRoot.localScale = new Vector3(1, Mathf.Lerp(.18f, 1, rise), 1);
        SetVisible(t > .03f, t >= .36f, t >= .58f, t >= .58f);
    }
    void AnimateRetreat(float t)
    {
        float shrink = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .55f));
        visualRoot.localScale = Vector3.Lerp(retreatScale, new Vector3(1, .18f, 1), shrink);
        visualRoot.position = t < .55f ? Vector3.Lerp(retreatStart, emergingPosition.position, shrink) : Vector3.Lerp(emergingPosition.position, hiddenPosition.position, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.55f, 1, t)));
        visualRoot.rotation = Quaternion.Slerp(retreatRotation, emergingPosition.rotation, t);
        SetVisible(t < 1, t < .55f, t < .5f, t < .5f);
    }
    void HideVisual()
    {
        SetVisible(false, false, false, false);
        if (visualRoot != null && hiddenPosition != null) { visualRoot.SetPositionAndRotation(hiddenPosition.position, hiddenPosition.rotation); visualRoot.localScale = new Vector3(1, .18f, 1); }
    }
    void SetVisible(bool hand, bool face, bool body, bool legs)
    {
        Visible(handRenderers, hand); Visible(headRenderers, face); Visible(bodyRenderers, body); Visible(legRenderers, legs);
    }
    static void Visible(Renderer[] renderers, bool value) { if (renderers != null) foreach (var r in renderers) if (r != null) r.enabled = value; }
    static void Play(AudioSource source, AudioClip clip, bool loop = false) { if (source == null || clip == null) return; source.clip = clip; source.loop = loop; source.Play(); }
    static void Stop(AudioSource source) { if (source != null) source.Stop(); }
    void StopAudio(AudioSource preserve = null) { foreach (var source in new[] { scratchingSource, emergingSource, exposedSource, breathingSource, retreatSource, safeCueSource, attackSource }) if (source != preserve) Stop(source); }
    public string Cue
    {
        get
        {
            if (CurrentConfig == null || !CurrentConfig.showTextCues || !NightActive) return "";
            if (phases.PlayerState != NightPlayerState.InBed && State != UnderBedState.Dormant) return "Something is under the bed. Return to bed to hide!";
            if (SafeToUncover && Time.time < safeCueUntil) return "The scraping has faded. You can release Space.";
            if ((State == UnderBedState.Emerging || State == UnderBedState.FullyExposed) && blanket.HoldRequested) return "Too soon. Uncover fully, then time a fresh hold.";
            if (State == UnderBedState.Scratching) return "Something is scratching underneath the bed...";
            if (State == UnderBedState.Emerging) return "Wait until it stops and faces you.";
            if (State == UnderBedState.FullyExposed) return releaseWarnings > 0 ? "Stay calm. Uncover fully, then hold Space again." : "It stopped. Hold Space now.";
            if (State == UnderBedState.PlayerHiding || State == UnderBedState.Retreat) return "Keep holding Space. Wait for it to leave.";
            return "";
        }
    }
}







