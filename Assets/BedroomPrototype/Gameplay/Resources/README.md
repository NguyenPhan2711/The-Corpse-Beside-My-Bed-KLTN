# Defensive resource limits

The existing `BedroomPrototype_PlayerFigure` scene and existing controllers are modified in place. No scene, player, camera, defensive controller, or monster was duplicated.

## Findings

- The flashlight battery was already enabled (100 maximum, 8/sec drain, 4/sec recharge). Detection already checked the active light and charge. The missing depletion threshold allowed repeated tiny-charge reactivation; recharge stopped outside bed.
- Breath already drained over 6 seconds, but recovered over 4 seconds even while exhausted R remained held. `ResetBreath` ran on unavailable-controls frames and state exit, allowing transitions to refill capacity. There was no timed gasp or recovery lockout. The door did not subscribe to `Gasped`.
- F/R use separate existing Input System actions. No duplicate controller or input callback bypass was found.

## Flashlight

`NightFlashlightController` defaults: battery enabled, maximum 100, drain 12.5/sec, recharge 5/sec, reactivation threshold 20%. F toggles. Drain/recharge use elapsed `Time.deltaTime`. A switched-on light also drains under the blanket. Zero charge turns off the actual Light and defensive effect, plays the optional depletion clip, and latches depletion until 20% recovers. Reaching the threshold requires another F press; it never automatically turns on.

While off, recharge continues throughout a running night, including standing and transitions. Charge persists through bed travel and successful defenses. It freezes outside a running night. Low-charge flicker starts below 15% and only modulates brightness subtly; it never randomly shuts down.

Window defense requires InBed, switched on, an active/enabled Light with positive intensity, positive charge, no depletion latch or blanket, range/angle, and an unobstructed ray to the face. `WindowEncounterConfig.resetExposureOnMiss` defaults true. Optional decay uses `exposureDecayPerSecond` (2/sec). Losing power interrupts exposure like losing the target.

## Breath

`HoldBreathController` defaults: maximum 100, hold duration 5 seconds (20 units/sec), recovery 8 seconds (12.5 units/sec), forced gasp 1 second, exhaustion lockout 1.5 seconds, release inhale 0.2 seconds. `BreathRemaining` is in units; existing `Capacity` remains normalized for compatibility.

Holding R consumes persistent capacity. Release starts an inhale and gradual recovery, rather than a refill. Exhaustion forces a gasp and releases the defense. Physical R release is mandatory before recovery; holding it continuously never recovers or restarts holding. Recovery requires release, the end of the gasp, and the end of lockout. A subsequent hold starts with only the capacity actually recovered.

The door subscribes to `Gasped` only during Listening; it also checks `IsGasping` each listening update. Release/normal inhalation fails the active check through `IsQuiet`. The existing attack and `LoseNight` flow are reused. Exhaustion outside Listening causes feedback and lockout without a global loss. Saved reaction-plus-listening requirements are 3.75/3.65/3.55 seconds, below the five-second capacity. A valid hold during return grace starts the normal timer immediately instead of adding mandatory hold time.

## State and reset rules

`PrototypeNightManager.Began` initializes both resources at a new night/retry. Explicit `ResetLight`/`ResetBreath` context menus are debug resets. Suspending controls, disabling components, phase changes, returning to bed, and monster retreat do not refill resources. Initial bed entry starts the night after its fade completes, enabling fresh F/R input immediately at the gameplay boundary.

GettingOutOfBed, OutOfBed and ReturningToBed disable the actual light, hit detection, breath hold and blanket. Held inputs must be released across transitions. Movement and E interactions remain handled by the original player and interactor.

## Feedback and Inspector settings

Existing development-only F4/F5 panels show battery/breath percentages, recharge, on/off, depletion lock, hold/gasp, release requirement, lockout, door breathing detection and face hits. They start hidden and are restricted to Editor/development builds. The existing discomfort overlay increases with low capacity.

All resource rates, durations, thresholds, flicker settings and audio references are public Inspector fields. Audio clips are optional. The project currently has no AudioClip assets: assign `normalBreathing`, `heavyBreathing`, `inhale`, `gasp`, and `emptyBatterySound` when available. Tests generate temporary silent clips to exercise playback paths; final sound quality, loudness and mix still need manual testing.

## Changes

Runtime scripts: `NightFlashlightController`, `HoldBreathController`, `PrototypeNightManager`, `BedroomPhaseController`, `DoorMonsterEncounter`, `WindowMonsterEncounter`, `WindowEncounterConfig`, `DoorEncounterHUD`, and `WindowEncounterHUD`. Scene defaults and existing debug panel dimensions were updated.

An Editor-only `DefenseResourcePlayModeChecks` harness was added. The existing `NightStateRepairChecks` expectation was updated to measure passive recharge while standing instead of requiring frozen charge. Scheduling, silent-window attacks, open-window danger, E interactions, character poses, bedroom layout and Space blanket logic are preserved.

Machine-readable results and the final saved-scene audit are in `Validation/`. See `Validation/ValidationNotes.md` for executed checks and remaining manual tests.
