# Main door encounter

The existing BedroomPrototype_PlayerFigure scene is edited in place. No scene is created or replaced.

Controls: hold R to hold breath; Space remains the independent blanket defense. F4 opens door debugging in Editor/development builds; F3 remains under-bed debugging. Debug panels start hidden. Opening a debug panel deliberately pauses camera look, not the encounter clock.

Quick test: enter Play mode, then Bedroom Prototype > Debug > Start Door Test Night (Play mode). This explicit Editor-only shortcut completes the current morning through existing UI callbacks, enters bed through the normal fade, and requests a door encounter. It never writes progress or scene changes. Alternatively complete morning normally and press F4 for Force / Reset / Enable buttons. A force request respects the shared director lease and can be refused while another monster owns it; reset/disable that encounter first.

DoorMonsterEncounter runs Dormant -> Approaching -> GentleKnocking -> AggressiveKnocking -> DoorOpening -> Listening -> Retreat or Attack. Aggressive banging ends with a configurable silence. The separate handle rotates before the actual door swings outward around MainDoorHinge. Door and handle meshes/colliders are preserved under the hinge. No NavMesh or imported animation clip is claimed for this procedural sequence. Root motion is disabled. HumanM_Model uses a project-owned idle Animator controller.

Anchors (world metres): MainDoorHinge (-0.34,0,-2.79), DistantFootsteps (0.15,0,-7), OutsideDoor (0.15,0.02,-3.65), DoorwayListening (0.15,0.02,-2.66). DoorAttackPosition follows the existing camera at local (0,0,0.5). Anchors and difficulty assets are editable. The outside sound anchor does not imply a navigable hallway.

DoorNight1/2/3 assets configure delays, warning lengths, opening angle, reaction grace, listening, retreat, attack, cooldown, cues, logging and nine audio slots. Night 1 defaults: delay25, steps5, gentle4, aggressive5, handle0.5, opening2, grace0.75, active listening3, retreat3, cooldown20–30 seconds. Additional short silences separate approach/knocking and banging/opening. Later nights reduce warnings and cooldowns while retaining reaction grace.

HoldBreathController on the original Player uses a new Input System R action without changing movement or existing action assets. Capacity drains over six seconds, recovers over four seconds, and early holds spend real capacity. Exhaustion emits Gasped, resumes breathing, and requires releasing R before another hold. No exact transition-frame press is required. The listening grace precedes the three-second check. Breath holding does not lock look. A subtle full-screen darkening communicates discomfort; the numerical meter is confined to F4 debugging.

BedroomEncounterDirector implements the existing IMonsterEncounterCoordinator for under-bed, plus an owner-based lease for door/future locations. Only one threat sequence owns the lease; there is a three-second quiet interval after release, and no new lease while the blanket is raised. Both enemies retain their own configurable cooldowns. Loss/victory stop both via the existing phase lifecycle and use the existing result UI.

No AudioClips were present. Import and assign these nine door sounds in each configuration: distant footsteps, approaching footsteps, gentle knocking, aggressive knocking/banging, handle turning, door creaking, monster breathing/listening cue, retreat footsteps, attack vocalization/movement. Also assign player normal breathing and gasp on HoldBreathController. Sources are separately labeled; door/environment sources are spatial; player breath is 2D. Missing clips are safely skipped. Text cues keep the prototype testable, but audio mix/readability cannot be evaluated until clips exist.

Manual tuning: listen on headphones after assigning clips; verify escalating knock cadence and silence with production audio, doorway silhouette, placeholder head turn and lunge, comfort darkening, three full-length nights with both enemies enabled. Imported mannequin has no authored knock/listen/attack animations. The visible monster currently uses idle plus procedural root/head movement.


Created runtime scripts: DoorMonsterEncounter, DoorEncounterConfig, HoldBreathController, BedroomEncounterDirector and DoorEncounterHUD. DoorNightDebugEntry and DoorPlayModeChecks are Editor-only. Existing movement, morning, phase, clock and under-bed source files are preserved; only the scene's under-bed coordinator reference is connected to the shared director. The project-owned DoorMonsterIdle controller and visual prefab are separate from imported assets.


Validation: 29 door Play-mode checks and 41 existing under-bed regression checks passed. Warning timers were accelerated in temporary runtime configuration clones; the three-second listening check was exercised. F4 hidden/display/close and post-Animator head facing were verified. Door swing sampled every 5 degrees through 95 degrees clears adjacent walls (maximum residual lintel contact 0.00000024 metres). Morning laptop/Truth callbacks, loss/retry and victory cleanup were exercised. Reports and a night-camera screenshot are in Validation. Audio playback quality and full-length Nights 1–3 still require manual playtesting with imported clips.


Current player-state rule: R is now bed-only. A standing player must return before defending. Warning progress is preserved and the shared director provides configurable return grace; see Gameplay/Validation/NightStateFixes/README.md.

