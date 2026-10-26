Gameplay update: the morning-to-night prototype is now implemented. See ../Gameplay/README.md for current phase, interaction and validation details. The notes below describe the character setup milestone.

# Bedroom character setup

Existing scene: Assets/Scenes/BedroomPrototype_PlayerFigure.unity. Edited and saved in place; no scenes created, renamed or duplicated.

## Imported assets

Kevin Iglesias Human Basic Motions FREE supplies HumanM_Model.fbx and HumanF_Model.fbx, both with valid Humanoid Avatars. Demo prefabs: Human_BasicMotionsDummy_M and Human_BasicMotionsDummy_F. The demo also includes HumanBasicMotions_SceneBase.fbx and HumanBasicMotionsScene.controller. These are demonstration mannequins, not finished college-student characters. Meshes include head and jaw bones but zero facial blendshapes and no recognizable production face. A detailed face, age-specific appearance, facial performance, and rot will require a later shared character asset replacement.

ImportedAssetInventory.json lists all 118 real animation clips and paths: male/female Idle01, Idle02 and transitions, Talk01, directional walk/run/sprint (including root-motion variants), turns, jump/fall/begin/land, plus hand grip/closed poses. No imported sitting, lying or sleeping animation exists. Third-party assets were not modified.

## Player

Existing Player, CharacterController, input actions, BedroomPlayerController and First Person Camera preserved. PlayerBody is a nested project-owned prefab referencing HumanM_Model, at approximately 1.73m animated mesh height. PlayerBody.controller uses the real HumanM@Idle01 as a development idle, with root motion disabled. No animation-driven movement or phase switching was added.

The project layer PlayerBodyVisual (31) covers the visual hierarchy. Only the existing first-person camera excludes this layer, preventing head/body interior clipping. Scene view and other cameras can show the body. The hidden original capsule renderer remains hidden.

## Phase anchors

MorningBodyAnchor and MorningCameraAnchor are children of Study desk. NightBodyAnchor and NightCameraAnchor are children of Bed. They intentionally remain fixed in the world while the development Player moves. No cameras or AudioListeners were added. MorningBodyAnchor marks the floor/root position near the desk chair; NightBodyAnchor marks the lying body's center on the protagonist half. Camera anchors are prospective viewpoints, not active cameras. Existing sleeping markers remain available.

## Corpse

Existing StainedGlassFigurePlaceholder root preserved and connected to the project-owned Corpse/StainedGlassCorpse.prefab. CorpseBody reuses the former HumanoidVisual wrapper, now containing a male model that references exactly the same original mesh and Avatar as PlayerBody. Its normal body/face material is the same ProtagonistBody material as the player. CorpseBody.controller holds the project-authored static Humanoid resting pose; root motion is off.

The prior female model is retained inactive inside CorpseBody as a legacy fallback. Original primitive renderers, materials and root references are retained. StainedGlassShell is a separate sibling using inflated skinned lead backing and colored inset panels. It shares the male rig's bones and reuses the muted red/purple/blue/green URP palette. Disable StainedGlassShell to reveal the normal male body and face underneath. GlassFragments is an empty future container. No break, rot, twitch or breathing mechanic implemented. The right bed half is clear.

## TheTruth

Existing parent reused. TruthBody is a male mannequin at 60% of the player's scale, facing the desk chair from the right/front desktop edge. Truth_DeskPerch is an authored Humanoid static seated pose with a slight hunch. Leg muscles were fitted for approximately horizontal thighs and vertical shins. Lowest supported mesh vertices are at desktop height + 2mm; no baked vertices were inside the desktop in the contact check.

TruthDesk.controller defaults to the static perch. Separate masked upper-body Talk01 and Idle01 layers are prepared at weight zero. Their masks exclude root and legs; future logic can blend them deliberately. No standing idle automatically overrides the pose. FutureLookTarget points toward the morning camera anchor. Head tracking, twitching, dialogue and AI are not implemented.

## Current reusable prefabs and controllers

- Characters/Player/PlayerBody.prefab and PlayerBody.controller
- Characters/Corpse/StainedGlassCorpse.prefab and CorpseBody.controller
- Characters/TheTruth/TheTruthDesk.prefab and TruthDesk.controller, Truth_DeskPerch.anim, UpperBody.mask

Earlier project-owned prefabs/controllers remain available; the scene uses the paths above. ValidationReport.json records current references, anchor positions and counts.

## Validation and remaining checks

Play mode ran with advancing frames. All three active Animators had valid Humanoid Avatars and root motion disabled; static poses held. Synthetic W input moved the player from spawn to the furniture and stopped, with grounded floor contact. Camera body-layer exclusion was confirmed. One player controller, one active camera and one AudioListener. No missing scripts, materials or prefab assets; no compilation failures, Console errors or warnings at final check.

The CLI's synthetic pointer/Escape events did not produce a measurable look/cursor response. Physical mouse-look, Escape release/click recapture and sprint feel still require a Game-view check. Minimum manual check: enter Play, click Game, use WASD/Shift and mouse, then Escape and click again. No movement script was changed. Fine-tune close-up desk/perch and pillow contact when replacing the mannequins. Future seated talking gestures may need mask/weight tuning to avoid the laptop. A recognizable identical-face reveal requires replacing both PlayerBody and CorpseBody with the same detailed character model. No full morning/night, fixed night camera, threat or glass-breaking system was implemented.

