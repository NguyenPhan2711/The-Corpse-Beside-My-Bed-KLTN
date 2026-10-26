# Under-bed monster prototype

The encounter is implemented in the SAME existing Assets/Scenes/BedroomPrototype_PlayerFigure.unity scene. No scenes were created, duplicated, renamed or replaced. The Truth and stained-glass corpse remain separate narrative characters and are never used as the attacker.

## Manual playtest

1. Enter Play. Explore normally, sit at the desk with E, reply to the two laptop messages and finish the three The Truth conversation replies.
2. Stand with Escape, turn off the wall switch beside the door, and press E at the bed.
3. Night 1 starts with the monster completely hidden for 15 seconds. The scratching warning lasts 4 seconds, then emergence lasts 5 seconds.
4. Look toward the protagonist's open bedside/foot corner. A hand slides out, the head appears, then the shoulders/body rise. Do NOT hold Space yet.
5. When it stops and faces you, press and HOLD Space. The default response window is 1.75 seconds. A hold that started earlier is invalid: release, fully uncover, and make a fresh press within the exposed window.
6. Keep holding until the retreat-safe cue. About 3 seconds FULLY covered is required with the default settings, plus the initial 0.25s blanket rise. The final 2 seconds of that hold include the retreat animation; it is not an additional 2-second wait.
7. Release Space after the safe cue. Restricted night mouse-look resumes. The next attempt waits a random 15-25 second cooldown.
8. Night 1 permits one short warning/recovery window for an early release. You must fully uncover and time another fresh hold. Another early release, or missing the window, causes the lunge and the EXISTING NightLost/retry screen. Nights 2/3 default to immediate attack for early release.

No audio clips currently exist in Assets. Visible prototype text substitutes for the scratch/exposed/safe cues so the mechanic can be tested. Audio assignment is required for sound-driven playtesting; see below.

## State machine and controls

UnderBedMonsterAI has Dormant, Scratching, Emerging, FullyExposed, PlayerHiding, Retreat and Attack states. It only runs when the existing BedroomPhaseController is in NightGameplay, the transition is finished, and PrototypeNightManager is running. Morning, fade, win and loss stop/reset the threat. The 12 AM-6 AM clock keeps running while the player hides.

The FullyExposed deadline uses absolute game time. BlanketController supplies a monotonic press sequence and whether each press began from a fully uncovered state. Defense requires a NEW press after exposure. An early hold does not immediately kill the player during emergence, but it never earns defense credit. Rapid tapping while partly covered cannot bypass timing or the minimum covered hold.

BlanketController is independent of this AI. Its serialized new Input System Cover action binds Space, enables only during active night play, and exposes HoldRequested, Coverage, IsCovering, IsCovered, CoverStarted and CoverReleased. The opaque dark fabric-colored panel slides upward/downward smoothly. It hides the room and ordinary HUD, while a temporary text cue remains above it because sound clips are absent. It never blocks morning laptop UI.

The existing phase controller now exposes PhaseChanged and owner-based observation locks. Blanket, attack and debug controls acquire/release their own locks, so one cannot accidentally unlock the others. This does not add another player, camera, AudioListener, movement script, Game Over screen or night clock.

## Scene hierarchy and positions

- UnderBedMonster: independent enemy root with UnderBedMonsterAI.
- EncounterAnchors/HiddenPosition: (-1.10, 0.025, -0.38), underneath the bed.
- EncounterAnchors/EmergingPosition: (0.78, 0.025, -0.85), at the visible aisle/foot corner.
- EncounterAnchors/ExposedPosition: (0.42, 0.025, -0.38), beside the protagonist half, facing the night viewpoint.
- AttackPosition: child of the EXISTING first-person camera, local (0, 0, 0.43), used as a close-face target for the short lunge.
- MonsterVisual: project-owned reusable visual prefab, separate from all narrative models.
- Player/BlanketController, BedroomHUD/BlanketOverlay, UnderBedCue and UnderBedDebug: reuse the existing camera, Canvas and EventSystem.

The hidden visual is disabled. Procedural staged movement first slides a compressed silhouette/hand through the cavity, then reveals the head and torso and raises the crouched humanoid. The exposed humanoid is about 1.05m tall in its crouched pose. The head turns toward the protagonist for the exposed cue. Attack is a short procedural lunge, not an imported attack clip.

The previous bed support was a solid plinth with only 3cm underneath. The SAME Bed frame object and BoxCollider, footprint and 0.45m top were preserved. The support is now 0.14m thick with a 0.31m cavity and four 0.31m support legs. Mattress, pillows, blanket, sleeping anchors, bed layout and narrative corpse were not moved. No bed-root shake was added, keeping those references stable.

## Assets and configuration

All new encounter assets are project-owned under Gameplay/UnderBed:

- Scripts/UnderBedMonsterAI.cs: state machine, interpolation, head turn, events and optional director contract.
- Scripts/BlanketController.cs: independent night-only Space input, smooth coverage and observation lock.
- Scripts/UnderBedEncounterConfig.cs: data-only per-night tuning and audio slots.
- Scripts/UnderBedEncounterHUD.cs: temporary text cues and development debug controls.
- Scripts/UnderBedPlayModeChecks.cs: Editor-only, explicitly spawned test object; excluded from builds and never saved to the bedroom scene.
- Config/UnderBedNight1.asset, UnderBedNight2.asset, UnderBedNight3.asset.
- Visuals/UnderBedMonsterVisual.prefab, UnderBedMonster.controller, UnderBedCrouchStatic.anim, project-owned split skinned meshes and URP materials.

The model is a distinct instance of Kevin Iglesias HumanM_Model.fbx with a valid Humanoid Avatar and root motion disabled. Original third-party model/animation/material assets are not edited. The authored static crouch and procedural root movement are used because the imported set has no suitable crawl, emergence or attack clips. Mesh visibility groups reveal right hand, head/neck, torso/arms and legs in order. This is a replaceable placeholder, not final creature animation.

Default configurations:

| Parameter | Night 1 | Night 2 | Night 3 |
|---|---:|---:|---:|
| Initial delay | 15s | 12s | 10s |
| Scratching | 4s | 3.5s | 3s |
| Emergence | 5s | 4.3s | 3.8s |
| Reaction | 1.75s | 1.5s | 1.25s |
| Fully-covered hold | 3s | 3s | 3.5s |
| Retreat animation | 2s | 2s | 2s |
| Success cooldown | 15-25s | 12-20s | 10-18s |
| Early release | One warning | Attack | Attack |

Attack duration, warning window, head-turn duration, director retry delay, text cues, defense logging, blanket rise/lower time, action binding and movement anchors are also configurable. The existing night duration remains 180 seconds.

## Audio slots requiring assets

No suitable AudioClip assets were available. Seven labeled spatial AudioSources are assigned, with playOnAwake off, doppler off and conservative volume/ranges. Config clip fields are intentionally empty, not broken/missing file references.

Assign licensed clips to each night config for:

1. Scratching underneath the bed (loop).
2. Emerging movement.
3. Fully-exposed stop/breath cue.
4. Nearby breathing (loop).
5. Retreating movement.
6. A distinct safe-to-uncover cue.
7. Attack.

The AI safely skips unassigned clips. Once audio is ready, Show Text Cues may be disabled for less explicit playtesting. Sound mixing, an authored crawl/attack, hand-to-blanket animation, final creature art and visual polish remain future work. No shaders/dithering or other three monster locations were implemented.

## Future director and debugging

No multi-monster director was created. Optional encounterCoordinator accepts a MonoBehaviour implementing IMonsterEncounterCoordinator: TryStartEncounter and EncounterEnded. The AI also reports EncounterBegan, EncounterEnded, DefenseSucceeded and DefenseFailed. Starts can be denied, and all endings are reported once. EncountersEnabled disables/resets the threat; AttacksEnabled can suppress damage during development. Force cannot bypass morning/transition/director gates.

In Editor/development builds, F3 toggles the debug panel. It shows state, remaining reaction/dormant timers, blanket state and success/failure totals. Buttons force, reset and enable/disable. Inspector context menus also provide force/reset. Close F3 to return to normal observation/defense. Ordinary release builds hide debug controls unless Allow Debug In Release Build is explicitly enabled. The panel resets on night end and does not reopen after retry.

## Executed validation

- UnderBedPlayModeChecks.json: 41 passing checks using the real Input System Space action, real reply buttons and existing morning/light/bed transitions. Covers early holds, real post-exposure defense, 3s hold/retreat, covered look lock, restored observation, cooldown, missed timing lunge, warning and strict early release, anti-spam, director permission/cancel notifications, win/loss cleanup, next-morning movement and unchanged narrative characters.
- UnderBedDebugChecks.json: 13 passing F3/buttons/lock ownership and lighting reload recovery checks, including night-loss/retry panel cleanup.
- ThreeDayRegression.json: 70 passing checks preserving movement, E/raycast desk interactions, tasks, light switch, bed, existing clock/retry, history and all three days with the threat components present.
- UnderBedExposed.png and UnderBedCovered.png: inspected composited Play-mode captures.

The hand has a tested unobstructed viewport ray past the mattress lip before the head appears. Exposed skinned-mesh vertices were checked against bed/mattress/pillow collider bounds with no overlaps. The default night viewpoint sees the exposed head. All tested states retain one active camera and AudioListener.

Tests use temporary config/Input System settings clones for fast scheduling/background input, then restore originals. The actual 3-second covered hold is exercised; saved gameplay defaults are not accelerated. Earlier development issues were corrected: cursor capture in an unattended camera test, reading the readable baked mesh rather than a read-only source, and a TimeOfDayLightingController material-block recreation after domain reload. Final test runs produce no new exceptions; historical failed-run log entries remain in the tool buffer.

