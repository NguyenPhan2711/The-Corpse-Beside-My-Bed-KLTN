# Story-driven time of day and the bedside light requirement

Implemented in the SAME existing Assets/Scenes/BedroomPrototype_PlayerFigure.unity scene. No scene was created, duplicated, renamed or replaced. The bedroom, player, characters, existing camera, task manager, input asset and prompt UI remain in place.

## Bed interaction priority

The existing BedroomInteractable and BedroomPhaseController now use the same two conditions:

1. Narrative requirements incomplete: I'm not sleepy yet.
2. Narrative complete, ceiling lamp on: I should turn off the light first.
3. Narrative complete, ceiling lamp off: [E] Go to bed

Both the interactable and the phase-controller entry point enforce the gate. Turning the lamp back on immediately prevents bed entry again. Finishing objectives or switching the light off never starts night automatically. The player must look at the bed within the existing 2m distance and press E.

MorningTaskManager.BedUnlocked remains the existing narrative-completion property for compatibility. BedroomPhaseController.CanStartNight is the combined narrative-plus-lamp gate.

## Lighting architecture

TimeOfDayLightingController is on BedroomGameplay, alongside the existing managers. It subscribes once to MorningTaskManager.Changed and re-evaluates completed story milestones, rather than running a time-of-day clock. It unsubscribes when disabled. The realtime night clock remains the existing PrototypeNightManager.

Inspector-configurable profiles: Morning, Afternoon, Evening, Late Evening, and a separate stationary-night profile. Each profile has directional color/intensity/rotation, shadow strength, ambient color and window base/emission colors. Transition Seconds defaults to 3 seconds. Intermediate blends start from the currently displayed lighting, so interrupted blends remain smooth.

The original Ceiling light is reused unchanged in position, range and basic warm character. It is realtime. RoomLightSwitch owns its on/off state, warm color and on intensity (3). The lighting controller does not continuously overwrite the switch. The lamp starts OFF each morning; stage entry to Evening and Late Evening turns it on once. A manual off state during Late Evening is preserved across repeated notifications.

The inspected scene had no directional light. One realtime directional source, Window daylight and moonlight, was added to provide pale morning daylight, warmer/lower afternoon sunlight, fading violet evening light and faint moonlight. Soft shadows remain enabled. The opaque placeholder Daylight pane no longer casts shadows, allowing light through its existing opening. Its colors use a MaterialPropertyBlock; original material assets are not changed.

Laptop subtle glow is a small realtime point source parented to the existing laptop, with 1.1m range, 0.12 intensity and no shadows. Turning off the ceiling does not disable the window source, ambient illumination or this glow. No URP renderer, shadow-atlas, post-processing or project-wide rendering settings were changed.

## Milestones and order

The default prototype has five equal-weight unique milestones per day:

- Reply to laptop message class.
- Reply to laptop message friend.
- Complete the first, second and third The Truth reply respectively.

At 20% progress the stage becomes Afternoon; at 60% it becomes Evening. Completing ALL required narrative objectives always reaches Late Evening, even if milestone configuration or valid conversation order differs. This uses the same sample content already in the project, not additional story.

Each milestone has a unique ID, optional day number (0 applies to every day), condition type, weight, message/objective ID or reply count. ObjectiveComplete milestones support future important narrative events. Duplicate IDs are grouped; already earned IDs cannot earn extra weight. Opening/closing UI has no milestone, and the existing task manager rejects duplicate replies. Progress and choices are kept separate.

On the next day, the controller clears only its current-day milestone set, returns to Morning, resets the ceiling to its configured initial state, and leaves dialogue history intact. Each of the three mornings uses the same components and scene.

## Physical switch

BedroomLightSwitch is on the front wall beside the door, at world position (0.9, 1.25, -2.715), approximately 1.25m above the floor. Its plate and rocker are simple modular cubes with project-owned URP Lit materials and raycastable colliders. It implements the existing IInteractable contract and uses the existing PlayerInteractor / E action / prompt UI.

Prompts alternate between [E] Turn off light and [E] Turn on light. The rocker tilts to indicate state. Switch interaction is available during morning exploration; it cannot allow the stationary night player to leave bed or control distant objects.

## Transition and tests

The existing bed transition now disables movement and fades before repositioning the body/camera, concealing the collision-free placement at PlayerSleepingSpot and NightCameraAnchor. It delegates environmental night blending to the lighting controller instead of dimming every light. The ceiling is already off before entry. Night still uses one camera/listener and the existing 12 AM-6 AM clock, with no added enemy AI.

Successful Play-mode reports in Validation:

- StoryLightingChecks.json: 33 checks. Includes physical E at switch/bed, all three exact bed messages, repeated panel openings, duplicate replies/notifications, mixed and conversation-first ordering, late-evening force-on, manual lamp persistence, retained faint light, stationary night entry, next-day reset and safe event re-subscription.
- MorningNightPlayModeChecks.json: 70 checks across all three days/nights. Includes original movement/desk/laptop/dialogue/stand-up behavior, the lamp gate every day, clock victory, retry, choice history, lighting resets and one camera/listener.

StoryMorning.png, StoryAfternoon.png, StoryEvening.png and StoryNight.png are inspected composited Play-mode captures. Tests temporarily accelerate lighting/night duration and use a runtime clone of Input System settings to accept synthetic input while the Editor is unfocused. Original settings and saved default durations (3s lighting / 180s night) are restored. Test objects are temporary and excluded from builds.

The initial unfocused test rejected synthetic E input; this was resolved in the test harness without changing the project's saved input behavior. The earlier compiler/domain-reload blockage is resolved. Final Console and compiler checks report zero current errors/warnings.

## Manual tuning

No required wiring remains. Adjust stage colors, transition duration, source intensity/angle, milestone weights/thresholds and switch starting state in the Inspector. Final models may require camera/pose and exposure balancing. Progress is still session-only, as in the previous prototype. No additional storyline, enemy AI or save system was added.
