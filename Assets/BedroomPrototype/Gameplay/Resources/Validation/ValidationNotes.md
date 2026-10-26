# Validation notes — 2026-10-25

Executed using Unity CLI against Unity 6000.4.1f1, the current project, and the existing BedroomPrototype_PlayerFigure scene. Play mode frame advancement was verified. Tests use temporary Input System devices and cloned encounter configs; no test scene was created. Temporary timing overrides are discarded on leaving Play mode.

## Scope

- Resource suite: saved defaults, elapsed-time drain/recharge, rapid F/R tapping, depletion latch and threshold, active Light requirement, sustained face exposure and interruption, breath exhaustion and lockout, release requirement, outside-listening safety, door gasp/release/correct defense, bed travel persistence, state restrictions, retry/new-night initialization, clock, victory and controller count.
- Existing door suite: full warning/open/listen/retreat/attack sequence with real R input and normal three-second listening.
- Existing window suite: real F aiming, sustained exposure, empty light, window danger, E interactions and actual WASD movement/collision through the room.
- Existing under-bed suite: blanket defense, early-release warning, attack, retry and victory.
- Existing state suite: three bed travel cycles, held-input clearing, disabled standing defenses, recharge and active enemy return grace.
- Existing flow suite: three days of morning desk/laptop/Truth/lighting/bed/night progression.
- Existing scheduling suite: silent open-window attacks, three seeded sequences with different first monsters and actual overlapping-warning defenses. This fixture disables battery drain to isolate scheduler behavior and shortens encounter durations; the dedicated resource suite tests real resource defaults and normal door listening separately.

## Test harness changes

The old state test required battery charge to freeze while standing. It now measures recharge against elapsed time while continuing to require disabled F/R/Space effects. Gameplay was not changed to satisfy the obsolete expectation.

The resource suite uses temporary zero-sample AudioClips to verify optional playback calls. These clips are not saved as assets. No finished audio clips are currently imported.

## Remaining manual checks

- Import and assign suitable normal/heavy breathing, inhale, forced-gasp and empty-battery sounds; tune loudness and final mix in headphones. Playback calls were tested, not the quality or recognizability of absent audio.
- Review low-battery flicker and breath discomfort overlay visually for comfort and atmosphere.
- Play full-length nights with human timing to tune resource rates, encounter spacing and recovery pressure. Accelerated fixtures do not establish long-session difficulty or player comfort.
- A standalone build/platform performance and input-focus test was not run.

The batch test runner encountered a transient Pipeline disconnect at a Play-mode domain reload. It was stopped, readiness/compilation were checked, and suites were executed individually. This was a tooling/setup issue, not a reported gameplay exception. One CLI warning said the Editor was already in Edit mode; final Console state is checked separately.

## Results

All seven suites passed: resource 52, door 29, window 48, under-bed 41, player-state 103, morning/night flow 70, silent scheduling 33. Total: 376 checks. The original scene was saved in Edit mode with requested resource defaults, automatic scheduling enabled, fixed seed disabled, all encounters enabled, one player/camera/listener/controller of each type, and zero missing scripts. The final scene audit is in SceneValidation.json.

