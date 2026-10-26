#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed class UnderBedTestCoordinator : MonoBehaviour, IMonsterEncounterCoordinator
{
    public bool allow;
    public int requests, ends;
    public bool TryStartEncounter(UnderBedMonsterAI encounter) { requests++; return allow; }
    public void EncounterEnded(UnderBedMonsterAI encounter, UnderBedOutcome outcome) { ends++; }
}

// Editor-only, explicitly spawned on a temporary Play-mode object. Never saved in the bedroom scene.
public sealed class UnderBedPlayModeChecks : MonoBehaviour
{
    [Serializable] public class Report { public bool finished, passed; public string error; public List<string> checks = new List<string>(); }
    public Report report = new Report();
    public bool debugOnly;
    UnderBedMonsterAI ai;
    BlanketController blanket;
    BedroomPhaseController phase;
    Keyboard keyboard; Mouse mouse;
    InputSettings originalInput, testInput;
    UnderBedEncounterConfig[] originals;
    float originalClock, originalCover, originalUncover;
    int began, ended, successes, failures;
    Vector3 truthPosition, corpsePosition;
    IEnumerator Start()
    {
        ai = FindAnyObjectByType<UnderBedMonsterAI>(); blanket = ai.blanket; phase = ai.phases;
        SchedulingTestOverride.Configure((BedroomEncounterDirector)ai.encounterCoordinator, debugOnly ? 60 : .35f);
        originalInput = InputSystem.settings; testInput = Instantiate(originalInput);
        testInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings = testInput;
        keyboard = InputSystem.AddDevice<Keyboard>("UnderBedVerification"); mouse = InputSystem.AddDevice<Mouse>("UnderBedMouseVerification");
        originals = ai.nightConfigs; ai.nightConfigs = originals.Select(Instantiate).ToArray();
        originalClock = phase.night.nightDurationSeconds; originalCover = blanket.coverSeconds; originalUncover = blanket.uncoverSeconds;
        phase.night.nightDurationSeconds = 120; blanket.coverSeconds = .08f; blanket.uncoverSeconds = .07f;
        var c = ai.CurrentConfig; c.initialDelay = .35f; c.scratchingDuration = .2f; c.emergingDuration = .8f; c.cooldownRange = new Vector2(.7f, .7f); c.attackDuration = .2f;
        ai.EncounterBegan += a => began++; ai.EncounterEnded += (a, o) => ended++; ai.DefenseSucceeded += a => successes++; ai.DefenseFailed += a => failures++;
        truthPosition = GameObject.Find("TheTruth").transform.position; corpsePosition = GameObject.Find("StainedGlassFigurePlaceholder").transform.position;
        var runner = debugOnly ? DebugControls() : Run();
        while (true)
        {
            bool more = false; object next = null;
            try { more = runner.MoveNext(); if (more) next = runner.Current; } catch (Exception ex) { report.error = ex.ToString(); break; }
            if (!more) break; yield return next;
        }
        Cleanup(); report.finished = true; report.passed = string.IsNullOrEmpty(report.error);
        Directory.CreateDirectory("Temp/BedroomPrototype"); File.WriteAllText("Temp/BedroomPrototype/" + (debugOnly ? "UnderBedDebugChecks" : "UnderBedPlayModeChecks") + ".json", JsonUtility.ToJson(report, true));
        Debug.Log("Under-bed verification: " + (report.passed ? "PASS" : "FAIL - " + report.error));
    }
    void Cleanup()
    {
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (originalInput != null && InputSystem.settings == testInput) InputSystem.settings = originalInput;
        if (testInput != null) Destroy(testInput);
        if (ai != null && originals != null) { foreach (var c in ai.nightConfigs) if (!originals.Contains(c)) Destroy(c); ai.nightConfigs = originals; ai.encounterCoordinator = null; }
        if (phase != null) phase.night.nightDurationSeconds = originalClock;
        if (blanket != null) { blanket.coverSeconds = originalCover; blanket.uncoverSeconds = originalUncover; }
    }
    void OnDestroy() { Cleanup(); }
    void Check(bool ok, string label) { report.checks.Add((ok ? "PASS: " : "FAIL: ") + label); if (!ok) throw new Exception(label); }
    IEnumerator Wait(Func<bool> condition, float timeout = 8) { float end = Time.realtimeSinceStartup + timeout; while (!condition() && Time.realtimeSinceStartup < end) yield return null; }
    void Key(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
    IEnumerator PressSpace(bool hold) { Key(hold ? new[] { UnityEngine.InputSystem.Key.Space } : new Key[0]); yield return null; yield return null; }
    void Reply() { phase.ui.ActiveButtonRoot.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => b.gameObject.activeSelf && b != phase.ui.buttonTemplate).onClick.Invoke(); }
    IEnumerator Picture(string name)
    {
        yield return new WaitForEndOfFrame(); var image = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes("Temp/BedroomPrototype/" + name + ".png", image.EncodeToPNG()); Destroy(image);
    }
    bool AllHidden => ai.handRenderers.Concat(ai.headRenderers).Concat(ai.bodyRenderers).Concat(ai.legRenderers).All(r => !r.enabled);
    bool HandVisible()
    {
        var r = (SkinnedMeshRenderer)ai.handRenderers[0]; var mesh = new Mesh(); r.BakeMesh(mesh);
        var points = mesh.vertices; bool visible = false;
        foreach (int index in mesh.triangles.Distinct())
        {
            var world = r.transform.TransformPoint(points[index]); var screen = phase.view.WorldToViewportPoint(world);
            if (screen.z > 0 && screen.x > 0 && screen.x < 1 && screen.y > 0 && screen.y < 1 && !Physics.Linecast(phase.view.transform.position, world, ~0, QueryTriggerInteraction.Ignore)) { visible = true; break; }
        }
        Destroy(mesh); return visible;
    }
    bool ExposedClearOfBed()
    {
        var colliders = GameObject.Find("Bed").GetComponentsInChildren<Collider>(); bool clear = true;
        foreach (var r in ai.handRenderers.Concat(ai.headRenderers).Concat(ai.bodyRenderers).Concat(ai.legRenderers).OfType<SkinnedMeshRenderer>())
        {
            var mesh = new Mesh(); r.BakeMesh(mesh); var points = mesh.vertices;
            foreach (int index in mesh.triangles.Distinct()) if (colliders.Any(c => c.bounds.Contains(r.transform.TransformPoint(points[index])))) { clear = false; break; }
            Destroy(mesh);
        }
        return clear;
    }
    IEnumerator DebugControls()
    {
        yield return null; ai.CurrentConfig.initialDelay = 60;
        var hud = phase.ui.GetComponent<UnderBedEncounterHUD>();
        Check(!hud.debugPanel.activeSelf && !hud.allowDebugInReleaseBuild, "development overlay defaults hidden and is excluded from ordinary release gameplay");
        phase.SitAtDesk(); yield return Wait(() => phase.State == BedroomPhase.MorningDesk && !phase.IsTransitioning);
        phase.ui.OpenLaptop(); Reply();
        var field = typeof(TimeOfDayLightingController).GetField("windowBlock", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.SetValue(phase.timeOfDayLighting, null); yield return null; yield return null;
        Check(field.GetValue(phase.timeOfDayLighting) != null, "lighting recreates a nonserialized material block during an active blend");
        Reply(); yield return null; phase.ui.CloseModal(); phase.ui.OpenTruth();
        for (int i = 0; i < phase.tasks.Definition.conversation.Length; i++) { Reply(); yield return null; }
        phase.ui.CloseModal(); phase.StandUp(); yield return Wait(() => phase.State == BedroomPhase.MorningExploration && !phase.IsTransitioning);
        phase.ceilingSwitch.Interact(phase.interactor); phase.GoToBed(); yield return Wait(() => phase.State == BedroomPhase.NightGameplay && !phase.IsTransitioning);
        yield return null;
        Key(UnityEngine.InputSystem.Key.F3); yield return null; yield return null; Key(); yield return null;
        Check(hud.debugPanel.activeSelf && hud.debugText.text.Contains("Dormant"), "F3 reveals the live state and timer overlay");
        hud.forceButton.onClick.Invoke(); yield return null;
        Check(ai.State == UnderBedState.Scratching, "debug Force button starts the existing encounter");
        hud.resetButton.onClick.Invoke(); yield return null;
        Check(ai.State == UnderBedState.Dormant && AllHidden, "debug Reset button hides and resets the monster");
        hud.enableButton.onClick.Invoke(); yield return null;
        Check(!ai.encountersEnabled && !ai.ForceStartEncounter(), "debug disable prevents forced attacks");
        hud.enableButton.onClick.Invoke(); yield return null; Check(ai.encountersEnabled, "debug enable restores encounter scheduling");
        yield return PressSpace(true); yield return Wait(() => blanket.IsCovered);
        Check(hud.debugPanel.activeSelf && blanket.IsCovered, "explicit debug controls remain accessible over the covered prototype view");
        Key(UnityEngine.InputSystem.Key.Space, UnityEngine.InputSystem.Key.F3); yield return null; yield return null; Key(UnityEngine.InputSystem.Key.Space); yield return null;
        Check(!hud.debugPanel.activeSelf && phase.IsObservationLocked, "closing debug retains the blanket's separate camera lock");
        yield return PressSpace(false); yield return Wait(() => !blanket.IsCovering);
        Check(!phase.IsObservationLocked, "releasing blanket clears only its own observation lock");
        Key(UnityEngine.InputSystem.Key.F3); yield return null; yield return null; Key(); yield return null;
        Check(hud.debugPanel.activeSelf, "debug can be open when a night ends");
        phase.LoseNight(); yield return null; yield return null;
        Check(!hud.debugPanel.activeSelf, "night loss closes development debug UI");
        Reply(); yield return null; yield return null;
        Check(!hud.debugPanel.activeSelf && !phase.IsObservationLocked && Cursor.lockState == CursorLockMode.Locked, "retry does not reopen debug or leave its camera lock behind");
        ai.SetEncountersEnabled(false);
    }
    IEnumerator Retry()
    {
        Reply(); yield return Wait(() => phase.State == BedroomPhase.NightGameplay && !phase.IsTransitioning); Key(); yield return null;
    }
    IEnumerator Run()
    {
        yield return null;
        Check(phase.State == BedroomPhase.MorningExploration && ai.State == UnderBedState.Dormant && AllHidden, "monster is hidden and dormant in morning");
        Check(!ai.ForceStartEncounter(), "debug force cannot trigger a morning encounter");
        yield return PressSpace(true); Check(!blanket.InputActive && !blanket.IsCovering, "Space has no blanket effect on morning controls"); yield return PressSpace(false);
        Check(originals.Length == 3 && originals[0].initialDelay == 15 && originals[0].reactionWindow == 1.75f && originals[0].requiredHidingDuration == 3, "three separate configs retain requested forgiving Night 1 defaults");
        phase.SitAtDesk(); yield return Wait(() => phase.State == BedroomPhase.MorningDesk && !phase.IsTransitioning);
        phase.ui.OpenLaptop(); for (int i = 0; i < phase.tasks.Definition.messages.Length; i++) { Reply(); yield return null; } phase.ui.CloseModal();
        phase.ui.OpenTruth(); for (int i = 0; i < phase.tasks.Definition.conversation.Length; i++) { Reply(); yield return null; } phase.ui.CloseModal();
        phase.StandUp(); yield return Wait(() => phase.State == BedroomPhase.MorningExploration && !phase.IsTransitioning);
        phase.ceilingSwitch.Interact(phase.interactor); Check(phase.GoToBed(), "existing completed morning and light switch enter the bed transition");
        Check(!ai.ForceStartEncounter(), "monster cannot start during fade or camera transition");
        yield return Wait(() => phase.State == BedroomPhase.NightGameplay && !phase.IsTransitioning);
        yield return null; Check(ai.State == UnderBedState.Dormant && AllHidden, "night begins with hidden initial-delay state");
        yield return new WaitForSeconds(.12f); Check(ai.State == UnderBedState.Dormant, "initial timer does not start an immediate attack");
        yield return Wait(() => ai.State == UnderBedState.Scratching); Check(ai.State == UnderBedState.Scratching && AllHidden, "initial delay leads to scratching with no visible monster");
        yield return PressSpace(true);
        yield return Wait(() => ai.State == UnderBedState.Emerging); Check(ai.State == UnderBedState.Emerging, "scratching warning leads to emergence");
        yield return new WaitForSeconds(.13f); Check(ai.handRenderers.Any(r => r.enabled) && ai.bodyRenderers.All(r => !r.enabled), "a hand becomes visible before the torso");
        yield return Wait(() => ai.StateElapsed >= ai.CurrentConfig.emergingDuration * .30f);
        // Early coverage intentionally blocks the room; check geometric visibility from the fixed camera origin.
        Check(HandVisible(), "emerging hand has an unobstructed viewport ray past the mattress lip");
        yield return Wait(() => ai.State == UnderBedState.FullyExposed);
        Check(ai.State == UnderBedState.FullyExposed && ai.headRenderers.All(r => r.enabled) && ai.bodyRenderers.All(r => r.enabled), "head and torso are fully revealed at bedside");
        Check(ExposedClearOfBed(), "exposed monster mesh clears mattress/frame/pillow colliders");
        Check(successes == 0 && ai.State != UnderBedState.PlayerHiding, "holding Space during emergence does not count as a counter");
        Check(blanket.IsCovered && phase.IsObservationLocked, "early blanket still obscures vision and locks observation");
        yield return PressSpace(false); yield return Wait(() => !blanket.IsCovering);
        yield return new WaitForSeconds(.3f);
        var vp = phase.view.WorldToViewportPoint(ai.head.position);
        Check(vp.z > 0 && vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1, "fully exposed head is visible from the default night camera");
        yield return Picture("UnderBedExposed");
        yield return PressSpace(true);
        Check(ai.State == UnderBedState.PlayerHiding || ai.State == UnderBedState.Retreat, "a fresh post-exposure Space press starts valid hiding");
        yield return Wait(() => blanket.IsCovered);
        phase.CaptureCursor(); var rotation = phase.view.transform.rotation; InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(300, 120)); yield return null; yield return null;
        Check(Quaternion.Angle(rotation, phase.view.transform.rotation) < .01f, "covered camera ignores mouse rotation");
        Check(blanket.overlay.gameObject.activeSelf && blanket.Coverage >= .999f, "opaque blanket reaches complete coverage");
        yield return Picture("UnderBedCovered");
        yield return Wait(() => successes == 1);
        Check(successes == 1 && failures == 0 && ai.SafeToUncover && ai.State == UnderBedState.Dormant, "three-second hold includes retreat and returns safe to dormant");
        Check(ended == 1 && began == 1 && AllHidden, "successful attempt reports begin/end exactly once and hides visual");
        yield return PressSpace(false); yield return Wait(() => !blanket.IsCovering);
        phase.CaptureCursor(); rotation = phase.view.transform.rotation; InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(100, 0)); yield return null; yield return null;
        Check(!phase.IsObservationLocked && Quaternion.Angle(rotation, phase.view.transform.rotation) > 1, "uncover restores restricted night observation (lock=" + phase.IsObservationLocked + ", cursor=" + Cursor.lockState + ", angle=" + Quaternion.Angle(rotation, phase.view.transform.rotation) + ")");
        Check(ai.State == UnderBedState.Dormant && ai.RemainingDormant > 0, "successful defense enforces cooldown");
        yield return Wait(() => ai.State == UnderBedState.Scratching); Check(ai.State == UnderBedState.Scratching, "new attempt begins only after cooldown");
        yield return Wait(() => ai.State == UnderBedState.FullyExposed);
        yield return Wait(() => ai.State == UnderBedState.Attack);
        Check(ai.State == UnderBedState.Attack, "missing the reaction window triggers the lunge");
        yield return Wait(() => phase.State == BedroomPhase.NightLost);
        Check(phase.State == BedroomPhase.NightLost && !phase.night.Running && AllHidden && !blanket.IsCovering, "attack uses existing night loss and resets monster/blanket");
        Check(failures == 1, "missed defense is reported once");
        yield return Retry();
        ai.ForceStartEncounter(); yield return Wait(() => ai.State == UnderBedState.FullyExposed);
        yield return PressSpace(true); yield return Wait(() => blanket.IsCovered); yield return PressSpace(false);
        Check(ai.State == UnderBedState.FullyExposed && phase.State == BedroomPhase.NightGameplay, "Night 1 allows one short early-release warning");
        yield return PressSpace(true); Check(ai.State == UnderBedState.FullyExposed, "rapid re-press while blanket is still up cannot bypass fresh-uncover rule");
        yield return PressSpace(false); yield return Wait(() => !blanket.IsCovering); yield return PressSpace(true);
        Check(ai.State == UnderBedState.PlayerHiding || ai.State == UnderBedState.Retreat, "fully uncovering permits a new correctly timed defense after warning");
        yield return PressSpace(false); yield return Wait(() => phase.State == BedroomPhase.NightLost);
        Check(phase.State == BedroomPhase.NightLost && failures == 2, "another early release exhausts warning and causes configured loss");
        yield return Retry();
        ai.CurrentConfig.earlyReleaseRule = UnderBedEncounterConfig.EarlyReleaseRule.Attack;
        ai.ForceStartEncounter(); yield return Wait(() => ai.State == UnderBedState.FullyExposed); yield return PressSpace(true); yield return Wait(() => blanket.IsCovered); yield return PressSpace(false);
        yield return Wait(() => phase.State == BedroomPhase.NightLost);
        Check(phase.State == BedroomPhase.NightLost && failures == 3, "strict release mode attacks without a warning");
        yield return Retry(); ai.ResetEncounter();
        var coordinator = gameObject.AddComponent<UnderBedTestCoordinator>(); ai.encounterCoordinator = coordinator;
        Check(!ai.ForceStartEncounter() && ai.State == UnderBedState.Dormant && coordinator.requests > 0, "optional director can deny a start request");
        coordinator.allow = true; Check(ai.ForceStartEncounter(), "optional director can permit an encounter");
        ai.SetEncountersEnabled(false);
        Check(AllHidden && !ai.ForceStartEncounter() && coordinator.ends == 1, "disable cancels encounter and notifies director once");
        ai.SetEncountersEnabled(true); ai.encounterCoordinator = null; yield return null;
        ai.CurrentConfig.scratchingDuration = .1f; ai.CurrentConfig.emergingDuration = 2;
        phase.night.nightDurationSeconds = .7f; phase.night.BeginNight(); ai.ForceStartEncounter();
        yield return Wait(() => ai.State == UnderBedState.Emerging);
        yield return Wait(() => phase.State == BedroomPhase.NightWon);
        Check(phase.State == BedroomPhase.NightWon && phase.night.Clock == "6 AM", "existing night clock can win while monster is emerging");
        Check(ai.State == UnderBedState.Dormant && AllHidden && !blanket.InputActive && !phase.IsObservationLocked, "night win stops AI, clears blanket and camera locks");
        phase.NextMorning(); yield return Wait(() => phase.State == BedroomPhase.MorningExploration && !phase.IsTransitioning);
        Check(phase.tasks.CurrentDay == 2 && phase.movement.enabled && phase.capsule.enabled && !blanket.InputActive && AllHidden, "next morning restores original movement with no hostile activity");
        Check(GameObject.Find("TheTruth").transform.position == truthPosition && GameObject.Find("StainedGlassFigurePlaceholder").transform.position == corpsePosition, "narrative characters were never moved or used as attackers");
        Check(FindObjectsByType<Camera>().Count(c => c.isActiveAndEnabled) == 1 && FindObjectsByType<AudioListener>().Count(a => a.isActiveAndEnabled) == 1, "all encounter paths retain one camera and AudioListener");
    }
}
#endif



