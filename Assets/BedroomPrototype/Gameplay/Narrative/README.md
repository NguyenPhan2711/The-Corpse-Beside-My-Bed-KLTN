# Text dialogue, night introductions and Night 3 corpse

The existing `Assets/Scenes/BedroomPrototype_PlayerFigure.unity` is extended in place. No scene, camera, player or corpse is duplicated. Narrative speech is text-only. No voice clips, voice AudioSources, TTS or speech timing are introduced. Existing monster/environment audio assignments remain in their original configs.

## Editing dialogue

- Night 1: `Dialogue/NightIntroduction1.asset` — 17 supplied lines.
- Night 2: `Dialogue/NightIntroduction2.asset` — 12 supplied lines.
- Night 3: `Dialogue/NightIntroduction3.asset` — 15 supplied lines.
- Morning Truth choices still come from the existing `MorningContent` asset. They are converted to temporary dialogue data and displayed by the same UI extension. Laptop messages retain the original modal and reply interface.
- Create future `DialogueSequence` assets for protagonist thoughts, narration or other characters. `BedroomGameUI.OpenNarrative(sequence)` uses the same presentation/input owner. There is no competing dialogue singleton.

Each line exposes speaker ID, text, optional typewriter speed (-1 uses UI default, 0 reveals immediately), event ID, before/after-line timing, animation-wait/hide-box options, simple conditions and choices. Conditions support day/night, story flags and matching recorded morning choices. Story flags and choices persist in the existing session task manager; this task does not add disk save/load.

Configure speaker labels/colors in `BedroomHUD > BedroomGameUI > Speakers`. The default IDs are TheTruth, Protagonist and Narration. The overlay Canvas keeps text outside later world-render pixelation/dithering; no pixelation shader was added.

## Manual progression and input

Left click or Enter advances. `Dialogue advance` is an Inspector-editable Input System action. The first advance while typewriting reveals the current line; a subsequent advance moves one line. Choices are explicit buttons, and may be selected deliberately without waiting for typing. No timed speech advancement exists.

A sequence can request blocked movement and allow limited look independently. The UI retains/restores the prior movement and cursor mode. E is blocked while dialogue is active and briefly after it closes, preventing input leakage. Duplicate dialogues are refused and advance is limited to one action per frame plus a configurable debounce. Dialogue choices still record morning progress and lighting milestones.

## Explicit events

`BedroomGameUI > Event Bindings` maps an event ID to an Inspector UnityEvent for future camera, Animator, lighting, flag or gameplay callbacks. The separate `Dialogue Event` signal is available for observers. `MorningTaskManager.SetStoryFlagTrue/False` can be bound without building a branching framework.

Night introductions additionally supply a coroutine event handler:

- `glass.fracture`: after Night 3 line 4 (index 3), crack/jitter the remaining glass and remove two lower fragments; wait for the presentation.
- `glass.reveal`: after line 5 (index 4), hide the box, move existing-camera attention toward the corpse, drop the remaining glass, observe, then resume the next line.

These are identifiers in the assets, not comparisons against dialogue sentences. The reveal animation duration is independent of manual line progression and contains no voice dependency.

## Introduction phase

`NightIntroduction` was appended to the existing phase enum, preserving the old enum values. Normal flow is:

Morning objectives ? light off ? bed fade/position ? NightIntroduction ? manually advanced Truth sequence ? configurable pause ? NightGameplay.

`PrototypeNightManager.PrepareNight()` sets 12 AM and stops the clock before the intro. Original actors and resources require NightGameplay, so their timers, effects and battery/breath consumption do not progress. The existing director remains inactive. Only after dialogue completion does `BeginNightGameplay()` start the clock, reset new-night resources and allow randomized scheduling. Held F/R/Space inputs are cleared through release latches.

Seen intros are tracked per night in the current play session. Retry opens the same intro with a small Tab skip prompt. `skipSeenAutomaticallyOnRetry` optionally skips it automatically; default is manual skip. Skipping always applies required Night 3 reveal state and waits the gameplay-start pause. First unseen intros cannot be skipped with the normal key. Development tools can explicitly skip any intro. No scene reload is used.

## Reveal and shared identity

The existing `StainedGlassFigurePlaceholder`, `CorpseBody`, `StainedGlassShell` and `GlassFragments` are used. The body and protagonist share the exact HumanM identity mesh; the body receives a separate muted decay prototype material. Final recognizable facial identity, wounds and decay textures still require custom art.

Twelve reusable fragment objects are under the existing initially empty GlassFragments parent. At runtime the current glass shell is baked into static fragment meshes after its existing resting pose evaluates. Original shell assets/objects are retained. Coverage stages remove lower fragments on Day/Night 2 and leave fewer for Night 3, retaining head coverage until the reveal. Reset restores fragment positions/rotations and coverage; no corpse or fragment GameObject is permanently destroyed.

During the reveal, the original camera receives a temporary configurable lift and look blend. A low-range, disabled-by-default local fill light makes the placeholder face visible during observation. The fill and focus reset afterward or on skip. The normal bed camera anchor is never moved. Original body/rig and player camera remain intact.

## Corpse encounter

The controller is attached to the existing corpse parent. It is passive on Nights 1 and 2, and requires a completed Night 3 reveal plus active NightGameplay.

Config: `Config/CorpseNight3.asset`:

| Setting | Default |
|---|---:|
| First eligibility after intro | 20s |
| Beckoning | 5s |
| Reaching danger time | 4s |
| Continuous facing-Q resistance | 1.75s |
| Cooldown | 20–30s |
| Gaze tolerance | 35° |
| Maximum additive camera drop | 0.13m |
| Camera recovery speed | 0.25m/s |
| Retreat / quiet surrender | 1.2s / 2.5s |

Other Inspector settings control warning yaw/roll, tiny mattress motion and optional creak SFX. No suitable new creak clip was invented; its reference/source remain optional and unassigned.

- Dormant: waits for the existing director and cooldown.
- Beckoning: head/mattress movement plus slight additive camera drift/drop. It can overlap other warning phases.
- Reaching: begins only after the shared director grants the mandatory defense slot. Pull increases by elapsed time.
- Resisting: face the head within the configured angle and hold Q. Holding Q before encounter start requires release first. Sustained resistance visually pushes the camera upright; releasing or looking away resets the continuous hold. Aborted holds are charged back to the danger timer, so tapping cannot reset or stall it indefinitely. F/R/Space are suspended during the action with their resources preserved.
- Retreat: restores pose/camera, releases the defense reservation and applies randomized cooldown.
- Surrender: quietly brings the corpse toward the camera, lowers/turns the view and darkens the screen, then invokes the existing night loss. It never invokes a final narrative ending.

Camera effects are applied after the base night view. A pillow/near-plane safety height prevents sinking through the mattress. End-of-night reset clears camera, pose, mattress, fade and input effects.

## Existing director integration and fairness

`BedroomEncounterKind.Corpse` was appended. Existing schedule assets now expose `corpseWeight`: 0 on Nights 1/2, 1 on Night 3. Existing original weights, open-window multiplier, random selection stream and actor configs are preserved. Corpse first-delay/cooldown participates in eligibility; it is not forced first. No second scheduler exists.

Corpse warning can share warning concurrency. Its lethal reaching/resistance stage uses the same defense reservation as blanket/door/window. It waits while another mandatory defense owns that slot. Standing or transitioning pauses corpse progress/Q and releases its mandatory reservation, allowing the original threats to follow their fairness rules. Return requires fresh input and reservation reacquisition before danger resumes. Window closing operations also block selection/mandatory action. The running night clock is not reset between encounters.

## Debugging independently

Enter Play mode, then press **F6**. The Editor/development-only panel starts Night 1/2/3 intros directly, skips the current intro, forces the finished reveal, force-starts/resets the corpse, and shows phase/dialogue line/director pause/corpse pull/Q/cooldown state. It starts hidden and is disabled in ordinary release builds. Normal skip is Tab after the intro has been seen.

F3/F4/F5 encounter debug UI remains. Existing door/window test-night menu shortcuts use the current dialogue choice root and explicitly skip the intro when requested as test-night shortcuts. Automated regression launchers use a nonserialized, development-only intro bypass; it is not saved or active in normal gameplay.

## Files

New runtime files: DialogueSequence.cs, BedroomGameUI.Dialogue.cs (partial extension of the existing owner), NightIntroductionController.cs, CorpseGlassReveal.cs, CorpseEncounterConfig.cs, CorpseEncounterController.cs, NarrativeDebugHUD.cs.

Modified runtime files: BedroomGameUI.cs, BedroomPhaseController.cs, MorningTaskManager.cs, PrototypeNightManager.cs, PlayerInteractor.cs, BedroomEncounterDirector.cs, NightEncounterSchedule.cs, BlanketController.cs, HoldBreathController.cs, NightFlashlightController.cs.

Editor-only fixtures: NarrativePlayModeChecks.cs and CorpseCoordinationChecks.cs. NarrativeInputTestGuard.cs restores test-only Input System settings/devices on Play mode exit or assembly reload, and remains dormant unless a test launcher explicitly arms it. Existing fixture reply helpers now resolve the active laptop/dialogue choice root; door/window debug shortcuts were updated for the intro boundary. These do not create test scenes.

## Validation and manual review

Machine-readable results, scene/config/audio audits and development notes are in Validation/. Only executed passing checks are reported there. Captured screenshots are retained with the validation artifacts when available.

Manual review remains for narrative pacing, motion comfort, full-length four-threat difficulty, custom facial/decay art, a suitable optional mattress/creak effect, low-resolution readability after future pixelation, and standalone-build testing. The existing body is still a placeholder, not a finished decaying human model. No final endings, voice acting or final rendering effects were implemented.


