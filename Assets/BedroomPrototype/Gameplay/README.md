Under-bed encounter update: the first hostile threat and Space blanket counter are implemented. See UnderBed/README.md for timing, debug controls, audio slots and validation.

Lighting update: story-driven Morning/Afternoon/Evening/Late Evening and the physical light-switch bed requirement are implemented. See Lighting.md for current configuration and 103 successful Play-mode checks.

# Morning desk interaction and night transition

Scene: Assets/Scenes/BedroomPrototype_PlayerFigure.unity. This is the existing active bedroom scene, edited in place. No scene was created, renamed, replaced, or loaded for phase changes.

## Play the prototype

1. Enter Play mode. Use the existing WASD/mouse/Shift development controller.
2. Approach the chair or a clear desk surface within 2m and aim at it. Press E when [E] Sit at desk appears.
3. While seated, use the mouse to look around (yaw +/-65 degrees, pitch +/-40 degrees relative to MorningCameraAnchor). WASD is suspended.
4. Aim at the laptop and press E. Read each sample message and click a reply. There are two required replies per morning. Escape or Close closes the panel.
5. Aim at The Truth and press E. Click through its three sample conversation replies. The checklist updates only after real replies finish the objective.
6. Escape with no panel open stands up and smoothly restores the saved standing location and existing controller. You can stand before finishing; partial progress remains.
7. Before both tasks are complete, the bed reads "I'm not sleepy yet." After both are complete, it reads "I should turn off the light first." Approach the wall switch beside the door and press E to turn off the ceiling lamp. Then return to the bed and press E at [E] Go to bed.
8. The same camera moves to NightCameraAnchor, the existing player body takes a static bed pose, a short fade plays, lighting darkens and the clock starts at 12 AM. WASD and stand-up are disabled. Mouse-look is restricted to +/-165 yaw and +/-65 pitch relative to the night anchor.
9. At 6 AM, click Begin next morning. The same room returns to morning, new-day objectives reset and earlier choices remain. Night 3 ends the prototype without creating day 4.

Default night duration: 180 real seconds for 12 AM-6 AM. Adjust BedroomGameplay > PrototypeNightManager > Night Duration Seconds for testing. The under-bed encounter is active; missing its blanket counter can cause night loss. See UnderBed/README.md. LoseNight() and retry state are available for future threats.

## Components and data

All new code is project-owned under Gameplay/Scripts:

- PlayerInteractor: reusable camera raycast interaction, configurable distance/mask, sorted trigger/solid hits and occlusion. Uses an independent runtime clone of the EXISTING InputSystem_Actions asset, including Player/Interact (E), Player/Look, UI/Cancel, UI/Click. Does not edit the imported input asset.
- IInteractable and BedroomInteractable: generic interaction contract plus furniture adapters for sit, laptop, conversation and bed.
- MorningContent: ScriptableObject data definition. Data/ThreeMorningPrototype.asset contains three editable days, required objective definitions, two sample messages per day, replies and conversation nodes. Labels/content/requirements are configurable in the Inspector.
- MorningTaskManager: current day, per-day objective/reply/conversation progress and separate choice history. CompleteObjective(id) is the integration point for later real systems. Current placeholder objectives complete only after the UI calls ReplyToMessage/ReplyToTruth with a valid outstanding reply.
- BedroomPhaseController: central MorningExploration, MorningDesk, NightGameplay, NightWon and NightLost states; smooth same-camera transitions, body pose switching, cursor ownership, lighting/fade and safe standing restore.
- PrototypeNightManager: configurable six-hour prototype clock and victory event. No duplicate NightManager existed in the inspected project.
- BedroomGameUI: TextMeshPro prompts, reticle, morning checklist, visibly labeled sample laptop/conversation panels, reply/close buttons, fade, clock and win/retry UI.
- BedroomFlowPlayModeChecks: Editor-only verification utility, explicitly added to a temporary Play-mode object during validation, never saved in the scene and excluded from builds.

BedroomGameplay is the single scene root for phase/tasks/night components and BedroomHUD. BedroomEventSystem uses InputSystemUIInputModule. The TMP essentials were imported non-interactively; no example content was imported.

## Reused scene objects

Existing Player, CharacterController, BedroomPlayerController script, PlayerBody model, first-person camera and AudioListener preserved. The existing controller is disabled temporarily while seated or in bed and re-enabled for exploration; its source was not replaced. The first-person camera retains its PlayerBodyVisual layer exclusion.

Existing bedroom, bed, corpse, desk, laptop, chair, door, window, TheTruth and lighting preserved. Existing solid colliders are reused. Interaction components are on Study desk, Chair, Laptop, TheTruth and Bed. TheTruth has a dedicated child trigger named TruthInteractionVolume, because its rigged visual had no raycast collider.

Existing MorningCameraAnchor/MorningBodyAnchor under Study desk and NightCameraAnchor/NightBodyAnchor under Bed are reused. PlayerSleepingSpot selects the protagonist side; CorpseSleepingSpot and the corpse remain on the other side. The camera never becomes a second camera. The capsule is disabled while its body occupies furniture, then restored at the cached valid standing position. Development, seated and lying Animator Controllers are switched only on the player's existing model, with root motion disabled.

Gameplay/Animations contains PlayerDeskStatic.anim plus PlayerDesk.controller and PlayerBed.controller. These use project-authored temporary poses; no imported seated/sleeping clip was invented. Third-party character/animation assets were not modified.

## Validation

Two successful Play-mode reports are preserved in Validation:

- MorningNightPlayModeChecks.json: actual new Input System W movement; actual E plus chair raycast; camera transitions; disabled seated/night movement; actual laptop/Truth raycasts; visible reply-button callbacks; partial progress across stand/sit; both objective gates; no automatic night; bed interaction; fixed night position; single camera/listener; six-AM victory; loss/retry; all three days and separate choice history.
- DeskControlChecks.json: new Input System seated mouse-look, yaw/pitch limits, no seated WASD, cursor release for laptop, graphic raycast to visible reply button, Escape closes panel first, cursor recapture, second Escape safely stands up.

The reports use accelerated night durations only in the temporary test session; the saved default remains 180 seconds. The UI was visually inspected at desktop and night-result sizes. An obsolete find API warning in the test harness was corrected; final compilation and current Console checks have zero errors/warnings. No missing scene scripts, materials, references, extra cameras or AudioListeners were found.

## Remaining prototype work

Fine-tune camera comfort, seated/lying hand/foot contact and look limits with physical input and final character models. The messages and dialogue are explicitly temporary. Add story branching, relationship consequences, enemies and sound separately. Choice history is kept independently of objective flags, but no consequences are applied yet.

Progress is session-only: stopping Play/restarting the application starts morning 1 again. No disk save system was requested or added. No pixelation, dithering, shader/post-processing work or full threat AI was added.


