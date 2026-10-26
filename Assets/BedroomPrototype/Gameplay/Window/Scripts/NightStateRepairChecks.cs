#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Explicitly spawned in Play mode; never placed in the saved scene or a build.
public sealed class NightStateRepairChecks : MonoBehaviour
{
    [Serializable] public class Result { public bool finished, passed; public string error; public List<string> checks = new List<string>(); }
    public Result report = new Result();
    BedroomPhaseController p; WindowMonsterEncounter window; DoorMonsterEncounter door; UnderBedMonsterAI under;
    NightFlashlightController flash; HoldBreathController breath; BlanketController blanket;
    Keyboard keyboard; Mouse mouse; InputSettings originalInput, testInput;
    WindowEncounterConfig[] oldWindow; DoorEncounterConfig[] oldDoor; UnderBedEncounterConfig[] oldUnder;
    readonly List<NightPlayerState> states = new List<NightPlayerState>();
    void Check(bool condition, string label) { report.checks.Add((condition ? "PASS: " : "FAIL: ") + label); if (!condition) throw new Exception(label); }
    void Key(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
    IEnumerator Keys(params Key[] keys) { Key(keys); yield return null; yield return null; }
    IEnumerator Wait(Func<bool> predicate, float seconds = 10) { float end = Time.realtimeSinceStartup + seconds; while (!predicate() && Time.realtimeSinceStartup < end) yield return null; }
    IEnumerator Aim(Vector3 target)
    {
        p.CaptureCursor(); var desired = Quaternion.LookRotation(target - p.view.transform.position);
        var delta = new Vector2(Mathf.DeltaAngle(p.view.transform.eulerAngles.y, desired.eulerAngles.y), -Mathf.DeltaAngle(p.view.transform.eulerAngles.x, desired.eulerAngles.x));
        InputSystem.QueueDeltaStateEvent(mouse.delta, delta / p.movement.mouseSensitivity); yield return null; yield return null;
    }
    void Reply() { p.ui.ActiveButtonRoot.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => b.gameObject.activeSelf && b != p.ui.buttonTemplate).onClick.Invoke(); }
    IEnumerator Start()
    {
        p = FindAnyObjectByType<BedroomPhaseController>(); window = FindAnyObjectByType<WindowMonsterEncounter>(); door = FindAnyObjectByType<DoorMonsterEncounter>(); under = FindAnyObjectByType<UnderBedMonsterAI>();
        SchedulingTestOverride.Configure(window.director,100);window.director.quietInterval=3;window.director.automaticScheduling=false;
        flash = window.flashlight; breath = door.breath; blanket = under.blanket;
        originalInput = InputSystem.settings; testInput = Instantiate(originalInput); testInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; testInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings = testInput;
        keyboard = InputSystem.AddDevice<Keyboard>("NightStateVerification"); mouse = InputSystem.AddDevice<Mouse>("NightStateMouse");
        oldWindow = window.nightConfigs; window.nightConfigs = oldWindow.Select(Instantiate).ToArray(); foreach (var c in window.nightConfigs) { c.initialDelay = 100; c.safeClosedCooldown = .4f; c.knockingDuration = .3f; c.minimumKnocking = .2f; }
        oldDoor = door.nightConfigs; door.nightConfigs = oldDoor.Select(Instantiate).ToArray(); foreach (var c in door.nightConfigs) { c.initialDelay = 100; c.footstepsDuration = .1f; c.approachSilence = .1f; c.gentleDuration = .1f; c.aggressiveDuration = .1f; c.silenceAfterBanging = .1f; c.handleDuration = .1f; c.openingDuration = .15f; c.reactionGrace = .2f; c.listeningDuration = .4f; c.retreatDuration = .2f; c.attackDuration = .2f; }
        oldUnder = under.nightConfigs; under.nightConfigs = oldUnder.Select(Instantiate).ToArray(); foreach (var c in under.nightConfigs) { c.initialDelay = 100; c.scratchingDuration = .15f; c.emergingDuration = .25f; c.reactionWindow = .8f; c.requiredHidingDuration = .6f; c.retreatDuration = .3f; c.attackDuration = .2f; }
        door.SetEncountersEnabled(false); under.SetEncountersEnabled(false); p.night.nightDurationSeconds = 240;
        window.director.selectionInterval = .1f; p.NightPlayerStateChanged += states.Add; p.interactor.logInteractions = true; window.logDiagnostics = true;
        var runner = Run(); while (true) { bool more = false; object next = null; try { more = runner.MoveNext(); if (more) next = runner.Current; } catch (Exception ex) { report.error = ex.ToString(); break; } if (!more) break; yield return next; }
        Key(); InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); InputSystem.settings = originalInput; Destroy(testInput);
        foreach (var c in window.nightConfigs) Destroy(c); window.nightConfigs = oldWindow; foreach (var c in door.nightConfigs) Destroy(c); door.nightConfigs = oldDoor; foreach (var c in under.nightConfigs) Destroy(c); under.nightConfigs = oldUnder;
        p.NightPlayerStateChanged -= states.Add; report.finished = true; report.passed = string.IsNullOrEmpty(report.error);
        System.IO.File.WriteAllText("Temp/BedroomPrototype/NightStateRepairChecks.json", JsonUtility.ToJson(report, true)); Debug.Log("Night state repair checks: " + report.passed + " " + report.error);
    }
    IEnumerator Run()
    {
        yield return null;
        Check(FindObjectsByType<PlayerInteractor>().Length == 1 && p.interactor.InteractionInputEnabled, "one centralized E handler remains enabled");
        p.SitAtDesk(); yield return Wait(() => p.State == BedroomPhase.MorningDesk && !p.IsTransitioning); p.ui.OpenLaptop(); for (int i = 0; i < p.tasks.Definition.messages.Length; i++) { Reply(); yield return null; } p.ui.CloseModal(); p.ui.OpenTruth(); for (int i = 0; i < p.tasks.Definition.conversation.Length; i++) { Reply(); yield return null; } p.ui.CloseModal();
        Check(p.tasks.BedUnlocked, "morning laptop and Truth interactions remain intact"); p.StandUp(); yield return Wait(() => p.State == BedroomPhase.MorningExploration && !p.IsTransitioning); p.ceilingSwitch.SetLight(false); Check(p.GoToBed(), "morning-to-night bed transition still works"); yield return Wait(() => p.CanUseNightDefenses); yield return null;
        Check(p.PlayerState == NightPlayerState.InBed && !p.movement.enabled, "InBed uses stationary night camera");
        yield return Keys(UnityEngine.InputSystem.Key.F); yield return Keys(); Check(flash.IsEmitting, "F works in bed");
        yield return Keys(UnityEngine.InputSystem.Key.Space, UnityEngine.InputSystem.Key.R); yield return new WaitForSeconds(.3f); Check(blanket.IsCovered && breath.IsHolding, "Space and R work in bed"); yield return Keys(); yield return new WaitForSeconds(.3f);
        yield return Keys(UnityEngine.InputSystem.Key.F); yield return Keys();
        for (int cycle = 0; cycle < 3; cycle++)
        {
            window.panel.SetOpenAmount(1);
            if (cycle == 0) { Check(window.director.TryAcquire(window), "prepare real director enemy lease"); window.director.Release(window); Check(p.nightExcursion.CanLeave, "enemy quiet cooldown no longer rejects a prompted get-up"); }
            window.director.RequestScheduled(window, 3); Check(window.director.HasPending(window), "window request is pending before leaving bed");
            yield return Keys(UnityEngine.InputSystem.Key.F, UnityEngine.InputSystem.Key.Space, UnityEngine.InputSystem.Key.R); int executed = p.interactor.ExecutedInteractions;
            float charge = flash.Charge, clock = p.night.Elapsed;
            yield return Keys(UnityEngine.InputSystem.Key.E, UnityEngine.InputSystem.Key.F, UnityEngine.InputSystem.Key.Space, UnityEngine.InputSystem.Key.R);
            Check(p.PlayerState == NightPlayerState.GettingOutOfBed && !p.movement.enabled, "E enters GettingOutOfBed with movement locked: cycle " + cycle);
            Check(!flash.IsEmitting && !flash.lamp.enabled && !flash.SwitchedOn && !blanket.IsCovering && !breath.IsHolding, "all held defenses clear synchronously at get-up");
            Check(!window.director.HasPending(window) && !window.ForceStartEncounter(), "queued window spawn canceled and activation blocked during get-up");
            yield return Keys(UnityEngine.InputSystem.Key.F, UnityEngine.InputSystem.Key.Space, UnityEngine.InputSystem.Key.R);
            yield return Keys(UnityEngine.InputSystem.Key.E, UnityEngine.InputSystem.Key.F, UnityEngine.InputSystem.Key.Space, UnityEngine.InputSystem.Key.R);
            Check(p.interactor.ExecutedInteractions == executed + 1, "repeated E during fade does not start another transition");
            yield return new WaitForSeconds(.4f); Check(!p.movement.enabled, "movement stays disabled through final standing fade");
            yield return Wait(() => p.PlayerState == NightPlayerState.OutOfBed && !p.IsTransitioning);
            Check(p.NightStanding && p.movement.enabled && p.capsule.enabled && p.interactor.InteractionInputEnabled, "standing restores original controller and keeps E enabled");
            charge = flash.Charge; float rechargeStart = Time.time; yield return new WaitForSeconds(.15f);
            Check(!p.CanUseNightDefenses && !flash.IsEmitting && !flash.lamp.enabled && !blanket.InputActive && !breath.InputActive && Mathf.Abs(flash.Charge - Mathf.Min(flash.maximumCharge,charge+flash.recoveryPerSecond*(Time.time-rechargeStart))) < .1f, "F/Space/R are disabled while switched-off battery passively recharges out of bed");
            Check(!window.flashlight.Illuminates(window.faceTarget) && !window.FlashlightDetectionEnabled, "standing flashlight cannot affect window counter detection");
            Check(!window.ForceStartEncounter() && !window.director.RequestScheduled(window, 3) && !window.director.HasPending(window), "standing rejects window scheduling and immediate activation");
            Check(window.State == WindowMonsterState.Dormant && p.night.Elapsed > clock, "standing window stays dormant while night clock advances");
            yield return Keys(); p.CaptureCursor(); yield return Keys(UnityEngine.InputSystem.Key.W); yield return new WaitForSeconds(.17f); yield return Keys(); yield return Keys(UnityEngine.InputSystem.Key.A); yield return new WaitForSeconds(.48f); yield return Keys();
            var handle = FindAnyObjectByType<WindowCloseInteractable>(); yield return Aim(handle.transform.position + new Vector3(.12f,.06f,0)); p.interactor.RefreshTarget();
            Check(ReferenceEquals(p.interactor.CurrentTarget, handle) && p.interactor.CurrentCollider.name == "WindowHandleInteractionVolume", "wide child trigger resolves parent window handle from natural aim");
            Check(p.interactor.TargetDistance < p.interactor.interactionDistance && p.interactor.CurrentPrompt == "[E] Close window", "close prompt and availability agree at standing distance");
            window.panel.HeadOccupiesOpening = true; p.interactor.RefreshTarget(); executed = p.interactor.ExecutedInteractions;
            Check(!handle.CanInteract(p.interactor) && !p.interactor.CurrentPrompt.StartsWith("[E]"), "occupied opening shows blocked feedback rather than active E prompt");
            yield return Keys(UnityEngine.InputSystem.Key.E); yield return Keys(); Check(p.interactor.ExecutedInteractions == executed && p.interactor.BlockedReason.Contains("occupies"), "blocked E reports exact occupancy condition without executing"); window.panel.HeadOccupiesOpening = false;
            clock = p.night.Elapsed; yield return Keys(UnityEngine.InputSystem.Key.E); yield return Keys(); Check(window.panel.IsAnimating, "E reliably starts manual window closing");
            executed = p.interactor.ExecutedInteractions; yield return Keys(UnityEngine.InputSystem.Key.E); yield return Keys(); Check(p.interactor.ExecutedInteractions == executed, "closing press cannot execute a second interaction");
            yield return Wait(() => !window.panel.IsAnimating); Check(!window.panel.IsWindowOpen && window.DangerMultiplier == 1 && window.panel.OpenSeconds == 0, "closing resets open-window danger"); Check(p.night.Elapsed > clock + .5f, "night clock continues through closing");
            yield return Aim(new Vector3(-.7f,1.6f,1)); p.CaptureCursor(); yield return Keys(UnityEngine.InputSystem.Key.W); yield return new WaitForSeconds(.16f); yield return Keys(); yield return Aim(p.protagonistPillow.transform.position); p.interactor.RefreshTarget();
            Check(p.interactor.CurrentPrompt == "[E] Return to bed" && p.interactor.CurrentTarget is BedroomInteractable, "return volume resolves bed from expected standing position");
            executed = p.interactor.ExecutedInteractions; clock = p.night.Elapsed; yield return Keys(UnityEngine.InputSystem.Key.F,UnityEngine.InputSystem.Key.Space,UnityEngine.InputSystem.Key.R); yield return Keys(UnityEngine.InputSystem.Key.E,UnityEngine.InputSystem.Key.F,UnityEngine.InputSystem.Key.Space,UnityEngine.InputSystem.Key.R);
            Check(p.PlayerState == NightPlayerState.ReturningToBed && !p.movement.enabled && !p.CanUseNightDefenses, "E enters ReturningToBed with movement and defenses locked");
            Check(!window.ForceStartEncounter(), "window cannot activate during return transition");
            yield return Wait(() => p.PlayerState == NightPlayerState.InBed && !p.IsTransitioning);
            Check(!flash.IsEmitting && !blanket.HoldRequested && !breath.IsHolding, "held standing keys do not reactivate defenses on return");
            Check(!p.movement.enabled && p.night.Elapsed > clock && p.interactor.ExecutedInteractions == executed + 1, "one E returns without movement or clock reset");
            yield return Keys(); yield return Keys(UnityEngine.InputSystem.Key.F); yield return Keys(); Check(flash.IsEmitting, "fresh F works after return");
            yield return Keys(UnityEngine.InputSystem.Key.Space,UnityEngine.InputSystem.Key.R); yield return new WaitForSeconds(.3f); Check(blanket.IsCovered && breath.IsHolding, "fresh Space and R work after return"); yield return Keys(); yield return new WaitForSeconds(.3f); yield return Keys(UnityEngine.InputSystem.Key.F); yield return Keys();
        }
        Check(states.Count == 12 && states.Take(4).SequenceEqual(new[]{NightPlayerState.GettingOutOfBed,NightPlayerState.OutOfBed,NightPlayerState.ReturningToBed,NightPlayerState.InBed}), "three cycles produce exactly four ordered states each");
        window.director.quietInterval = .1f; window.director.CurrentSchedule.initialDelay = .15f; window.director.automaticScheduling=true;window.director.RestartAutomaticScheduling(); window.ResetEncounter(); yield return Wait(() => window.State == WindowMonsterState.Knocking, 5); Check(window.State == WindowMonsterState.Knocking, "window scheduling resumes in bed without resetting the night"); window.SetEncountersEnabled(false);window.director.automaticScheduling=false;
        window.panel.SetOpenAmount(1); yield return Keys(UnityEngine.InputSystem.Key.E); yield return Keys(); yield return Wait(() => p.NightStanding && !p.IsTransitioning);
        window.director.quietInterval = .1f; window.director.outOfBedAttackGrace = 2.5f; window.director.returnedDefenseGrace = .2f; under.SetEncountersEnabled(true); yield return new WaitForSeconds(.2f);
        Check(under.ForceStartEncounter(), "under-bed warnings can start while standing"); yield return Wait(() => under.State == UnderBedState.FullyExposed); yield return new WaitForSeconds(.2f);
        Check(p.State == BedroomPhase.NightGameplay && window.director.RemainingBedGrace(under) > 0, "under-bed exposure gives director-owned return grace");
        yield return Aim(p.protagonistPillow.transform.position); p.interactor.RefreshTarget(); Check(p.interactor.CurrentPrompt == "[E] Return to bed", "active enemy lease does not block returning to bed"); yield return Keys(UnityEngine.InputSystem.Key.E); yield return Keys(); yield return Wait(() => p.CanUseNightDefenses);
        Check(under.State == UnderBedState.FullyExposed, "return preserves under-bed encounter progress"); yield return Keys(UnityEngine.InputSystem.Key.Space); yield return Wait(() => under.State == UnderBedState.Dormant);
        Check(under.SuccessfulDefenses == 1, "returned player can counter under-bed threat fairly"); yield return Keys(); yield return new WaitForSeconds(.3f); under.SetEncountersEnabled(false);
        yield return Keys(UnityEngine.InputSystem.Key.E); yield return Keys(); yield return Wait(() => p.NightStanding && !p.IsTransitioning); door.SetEncountersEnabled(true); yield return new WaitForSeconds(.2f); Check(door.ForceStartEncounter(), "door warning sequence can start while standing"); yield return Wait(() => door.State == DoorMonsterState.Listening); yield return Keys(UnityEngine.InputSystem.Key.R); Check(!breath.IsHolding, "standing cannot counter door by holding R");
        Check(window.director.RemainingBedGrace(door) > 0 && p.State == BedroomPhase.NightGameplay, "door listening grants time to return instead of instant death"); yield return Aim(p.protagonistPillow.transform.position); yield return Keys(UnityEngine.InputSystem.Key.E,UnityEngine.InputSystem.Key.R); yield return Keys(UnityEngine.InputSystem.Key.R); yield return Wait(() => p.CanUseNightDefenses); Check(door.State == DoorMonsterState.Listening && !breath.IsHolding, "return preserves door listening and clears held R"); yield return Keys(); yield return Keys(UnityEngine.InputSystem.Key.R); yield return Wait(() => door.State == DoorMonsterState.Dormant); Check(door.SuccessfulDefenses == 1, "fresh in-bed R successfully counters door after return"); yield return Keys();
        yield return Keys(UnityEngine.InputSystem.Key.E); yield return Keys(); yield return Wait(() => p.NightStanding && !p.IsTransitioning); window.director.outOfBedAttackGrace = .4f; yield return new WaitForSeconds(.2f); Check(door.ForceStartEncounter(), "standing danger test starts door warnings"); yield return Wait(() => p.State == BedroomPhase.NightLost, 5); Check(p.State == BedroomPhase.NightLost, "remaining out of bed beyond grace still causes intended loss"); Check(p.PlayerState == NightPlayerState.InBed && !p.IsTransitioning, "loss restores consistent shared bed state"); p.RetryNight(); yield return null; Check(p.State == BedroomPhase.NightGameplay && p.PlayerState == NightPlayerState.InBed, "retry restores in-bed gameplay"); door.SetEncountersEnabled(false); p.night.nightDurationSeconds = .2f; p.night.BeginNight(); yield return Wait(() => p.State == BedroomPhase.NightWon); Check(p.State == BedroomPhase.NightWon, "existing night victory still works");
    }
}
#endif






