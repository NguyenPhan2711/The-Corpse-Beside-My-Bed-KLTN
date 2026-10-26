#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Explicitly added only to a temporary Play-mode object by the Editor validation command.
// This component is never saved to the bedroom scene and is excluded from builds.
public sealed class BedroomFlowPlayModeChecks : MonoBehaviour
{
    [Serializable] public class Report { public bool finished, passed; public string error; public List<string> checks = new List<string>(); }
    public Report report = new Report();
    public bool controlsOnly;
    BedroomPhaseController phase;
    Keyboard keyboard;
    Mouse mouse;
    InputSettings originalInputSettings, testInputSettings;
    float duration;
    IEnumerator Start()
    {
        phase = FindAnyObjectByType<BedroomPhaseController>(); duration = phase.night.nightDurationSeconds;
        originalInputSettings = InputSystem.settings;
        testInputSettings = Instantiate(originalInputSettings);
        testInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = testInputSettings;
        keyboard = InputSystem.AddDevice<Keyboard>("BedroomFlowVerification");
        var runner = controlsOnly ? Controls() : Run();
        while (true)
        {
            bool more = false; object current = null;
            try { more = runner.MoveNext(); if (more) current = runner.Current; }
            catch (Exception ex) { report.error = ex.ToString(); break; }
            if (!more) break;
            yield return current;
        }
        InputSystem.RemoveDevice(keyboard); phase.night.nightDurationSeconds = duration;
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        RestoreInputSettings();
        report.finished = true; report.passed = string.IsNullOrEmpty(report.error);
        Directory.CreateDirectory("Temp/BedroomPrototype");
        File.WriteAllText("Temp/BedroomPrototype/" + (controlsOnly ? "DeskControlChecks" : "MorningNightPlayModeChecks") + ".json", JsonUtility.ToJson(report, true));
        Debug.Log("Bedroom flow verification: " + (report.passed ? "PASS" : "FAIL - " + report.error));
    }
    void RestoreInputSettings() { if (originalInputSettings != null && InputSystem.settings == testInputSettings) InputSystem.settings = originalInputSettings; if (testInputSettings != null) Destroy(testInputSettings); }
    void OnDestroy() { RestoreInputSettings(); }
    void Check(bool condition, string label)
    {
        report.checks.Add((condition ? "PASS: " : "FAIL: ") + label);
        if (!condition) throw new Exception(label);
    }
    void Key(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
    IEnumerator Wait(Func<bool> condition, float seconds = 5)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < end) yield return null;
    }
    void ReplyButton()
    {
        var button = phase.ui.ActiveButtonRoot.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => b.gameObject.activeSelf && b != phase.ui.buttonTemplate);
        button.onClick.Invoke();
    }
    void Use(string name) { GameObject.Find(name).GetComponent<BedroomInteractable>().Interact(phase.interactor); }
    void RayUse(string name, Vector3 target) { phase.view.transform.LookAt(target); phase.interactor.RefreshTarget(); Check(phase.interactor.CurrentTarget == (IInteractable)GameObject.Find(name).GetComponent<BedroomInteractable>(), "actual raycast reaches " + name); phase.interactor.CurrentTarget.Interact(phase.interactor); }
    IEnumerator Controls()
    {
        yield return null;
        phase.SitAtDesk(); yield return Wait(() => phase.State == BedroomPhase.MorningDesk && !phase.IsTransitioning);
        Check(phase.State == BedroomPhase.MorningDesk, "seated control check starts at morning anchor");
        mouse = InputSystem.AddDevice<Mouse>("BedroomControlVerification");
        phase.CaptureCursor(); InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(200, 100));
        yield return null; yield return null;
        Check(Quaternion.Angle(phase.view.transform.rotation, phase.morningCameraAnchor.rotation) > 1, "new Input System seated mouse-look rotates camera");
        InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(100000, 100000)); yield return null; yield return null;
        var relative = Quaternion.Inverse(phase.morningCameraAnchor.rotation) * phase.view.transform.rotation;
        Check(Mathf.Abs(Mathf.DeltaAngle(0, relative.eulerAngles.y)) <= phase.deskYawLimit + .1f && Mathf.Abs(Mathf.DeltaAngle(0, relative.eulerAngles.x)) <= phase.deskPitchLimit + .1f, "seated look clamps pitch and yaw");
        var seatedPosition = phase.movement.transform.position;
        Key(UnityEngine.InputSystem.Key.W); yield return new WaitForSeconds(.15f); Key(); yield return null;
        Check(Vector3.Distance(seatedPosition, phase.movement.transform.position) < .001f, "W does not move seated player");
        phase.ui.OpenLaptop(); yield return null; yield return new WaitForEndOfFrame();
        var screenshot = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes("Temp/BedroomPrototype/LaptopUI.png", screenshot.EncodeToPNG()); Destroy(screenshot);
        Check(phase.ui.ModalOpen && Cursor.lockState == CursorLockMode.None, "laptop releases cursor for reply buttons");
        var first = phase.ui.ActiveButtonRoot.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => b.gameObject.activeSelf && b != phase.ui.buttonTemplate);
        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        pointer.position = RectTransformUtility.WorldToScreenPoint(null, first.GetComponent<RectTransform>().TransformPoint(first.GetComponent<RectTransform>().rect.center));
        var results = new List<UnityEngine.EventSystems.RaycastResult>(); UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, results);
        Check(results.Any(r => r.gameObject == first.gameObject || r.gameObject.transform.IsChildOf(first.transform)), "UI graphic raycaster reaches visible reply button");
        Key(UnityEngine.InputSystem.Key.Escape); yield return null; yield return null; Key(); yield return null;
        Check(!phase.ui.ModalOpen && phase.State == BedroomPhase.MorningDesk, "Escape closes laptop without leaving desk");
        Check(Cursor.lockState == CursorLockMode.Locked, "closing laptop recaptures cursor");
        Key(UnityEngine.InputSystem.Key.Escape); yield return null; yield return null; Key();
        yield return Wait(() => phase.State == BedroomPhase.MorningExploration && !phase.IsTransitioning);
        Check(phase.State == BedroomPhase.MorningExploration && phase.movement.enabled && phase.capsule.enabled, "Escape outside dialogue safely stands up");
    }
    IEnumerator Run()
    {
        yield return null;
        Check(phase.State == BedroomPhase.MorningExploration, "starts in morning exploration");
        Check(!phase.GoToBed(), "bed rejects incomplete objectives");
        Check(phase.movement.enabled && phase.capsule.enabled, "original controller and collision enabled in morning");
        var original = phase.movement.transform.position;
        phase.CaptureCursor(); Key(UnityEngine.InputSystem.Key.W); yield return new WaitForSeconds(.25f); Key(); yield return null;
        Check(Vector3.Distance(original, phase.movement.transform.position) > .1f, "new Input System W moves existing player");
        // Place the capsule in a clear aisle and look at the actual chair collider to exercise E + raycast.
        phase.capsule.enabled = false; phase.movement.transform.position = new Vector3(.45f, .03f, .25f); phase.capsule.enabled = true;
        phase.ReleaseCursor(); var seat = GameObject.Find("Seat").GetComponent<Collider>().bounds.center;
        phase.view.transform.LookAt(seat); Physics.SyncTransforms(); phase.interactor.RefreshTarget();
        Check(phase.interactor.CurrentPrompt == "[E] Sit at desk", "chair raycast gives sit prompt within 2m");
        Key(UnityEngine.InputSystem.Key.E); yield return null; yield return null; Key();
        yield return Wait(() => phase.State == BedroomPhase.MorningDesk && !phase.IsTransitioning);
        Check(phase.State == BedroomPhase.MorningDesk, "E at actual raycast target sits at desk");
        Check(!phase.movement.enabled && !phase.capsule.enabled, "sitting suspends WASD and capsule motion");
        Check(Vector3.Distance(phase.view.transform.position, phase.morningCameraAnchor.position) < .01f, "same camera reaches morning anchor");
        Check(!phase.tasks.BedUnlocked && phase.tasks.Current.completedObjectives.Count == 0, "sitting does not complete objectives");
        RayUse("Laptop", GameObject.Find("Laptop lit screen").transform.position); yield return null;
        Check(phase.ui.ModalOpen && phase.ui.modalTitle.text.Contains("PROTOTYPE LAPTOP"), "visible sample laptop modal opens");
        ReplyButton(); yield return null;
        Check(phase.tasks.Current.repliedMessages.Count == 1 && !phase.tasks.IsComplete("laptop"), "one actual reply preserves partial laptop progress");
        phase.ui.CloseModal(); var returnPosition = new Vector3(.45f, .03f, .25f);
        Check(phase.StandUp(), "can stand before finishing objectives");
        yield return Wait(() => phase.State == BedroomPhase.MorningExploration && !phase.IsTransitioning);
        Check(phase.movement.enabled && phase.capsule.enabled && Vector3.Distance(returnPosition, phase.movement.transform.position) < .1f, "standing restores saved safe position/controller");
        Check(phase.tasks.Current.repliedMessages.Count == 1, "standing preserves task progress");
        for (int day = 1; day <= 3; day++)
        {
            Use("Chair"); yield return Wait(() => phase.State == BedroomPhase.MorningDesk && !phase.IsTransitioning);
            Check(phase.State == BedroomPhase.MorningDesk, "day " + day + " desk available");
            RayUse("Laptop", GameObject.Find("Laptop lit screen").transform.position); yield return null;
            int remaining = phase.tasks.Definition.messages.Length - phase.tasks.Current.repliedMessages.Count;
            for (int i = 0; i < remaining; i++) { ReplyButton(); yield return null; }
            Check(phase.tasks.IsComplete("laptop") && !phase.tasks.BedUnlocked, "day " + day + " laptop complete but bed still locked");
            phase.ui.CloseModal(); RayUse("TheTruth", GameObject.Find("TruthInteractionVolume").transform.position); yield return null;
            for (int i = 0; i < phase.tasks.Definition.conversation.Length; i++) { ReplyButton(); yield return null; }
            Check(phase.tasks.IsComplete("truth") && phase.tasks.BedUnlocked, "day " + day + " conversation replies unlock bed");
            Check(phase.State == BedroomPhase.MorningDesk, "objectives never start night automatically");
            phase.ui.CloseModal(); phase.StandUp(); yield return Wait(() => phase.State == BedroomPhase.MorningExploration && !phase.IsTransitioning);
            Check(phase.ceilingSwitch.IsOn && !phase.CanStartNight, "late evening requires turning the ceiling light off");
            Check(!phase.GoToBed(), "night entry cannot bypass the ceiling-light gate");
            phase.ceilingSwitch.Interact(phase.interactor);
            Check(!phase.ceilingSwitch.IsOn && phase.CanStartNight, "manual switch interaction permits bed use");
            phase.night.nightDurationSeconds = 2f; Use("Bed");
            yield return Wait(() => phase.State == BedroomPhase.NightGameplay && !phase.IsTransitioning);
            Check(phase.State == BedroomPhase.NightGameplay, "day " + day + " bed interaction starts night");
            Check(Vector3.Distance(phase.view.transform.position, phase.nightCameraAnchor.position) < .01f, "night camera remains at head anchor");
            Check(!phase.movement.enabled && !phase.capsule.enabled && !phase.StandUp(), "night cannot stand or freely walk");
            var fixedPosition = phase.movement.transform.position;
            Key(UnityEngine.InputSystem.Key.W); yield return new WaitForSeconds(.15f); Key(); yield return null;
            Check(Vector3.Distance(fixedPosition, phase.movement.transform.position) < .001f, "night W cannot move player");
            Check(FindObjectsByType<Camera>().Count(c => c.isActiveAndEnabled) == 1 && FindObjectsByType<AudioListener>().Count(a => a.isActiveAndEnabled) == 1, "one camera/listener throughout transitions");
            if (day == 2)
            {
                phase.LoseNight(); Check(phase.State == BedroomPhase.NightLost && phase.ui.ModalOpen, "temporary night-loss result supported");
                ReplyButton(); yield return null; Check(phase.State == BedroomPhase.NightGameplay && !phase.ui.ModalOpen, "retry restarts night in same scene");
            }
            yield return Wait(() => phase.State == BedroomPhase.NightWon);
            Check(phase.State == BedroomPhase.NightWon && phase.night.Clock == "6 AM", "day " + day + " prototype clock reaches 6 AM/victory");
            if (day < 3)
            {
                int previousChoices = phase.tasks.Choices.Count; ReplyButton();
                yield return Wait(() => phase.State == BedroomPhase.MorningExploration && !phase.IsTransitioning);
                Check(phase.tasks.CurrentDay == day + 1 && !phase.tasks.BedUnlocked && phase.tasks.Current.completedObjectives.Count == 0, "next morning resets only new-day objectives");
                Check(phase.tasks.Choices.Count == previousChoices, "dialogue history persists across days");
                Check(phase.timeOfDayLighting.CurrentStage == StoryTimeOfDay.Morning && !phase.ceilingSwitch.IsOn && phase.timeOfDayLighting.MilestoneCount == 0, "new day resets lighting and lamp without old milestones");
            }
        }
        Check(phase.tasks.CurrentDay == 3 && phase.tasks.Choices.Count == 15, "three-day sequence ends with separate choice history");
        Check(phase.ui.ActiveButtonRoot.GetComponentsInChildren<UnityEngine.UI.Button>().All(b => !b.gameObject.activeSelf || b == phase.ui.buttonTemplate), "third night ends without creating a fourth day");
    }
}
#endif


