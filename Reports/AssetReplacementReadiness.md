# Asset replacement readiness — The Corpse Beside My Bed

Audit date: 2026-10-26 (Asia/Saigon). Inspected live with Unity CLI 1.0.0-beta.14 and Unity 6000.4.1f1.

**Verdict: partially ready. No character is a fully automatic drop-in replacement yet.** Stable gameplay roots and anchors exist, but renderer groups, skin bindings, direct bone references, primitive colliders and pose-fitting code still connect gameplay to the current art. Cosmetic mesh/material changes are safer than deleting/replacing whole hierarchies.

This is a read-only audit. No scene, GameObject, component, script, prefab, audio assignment, import setting or model was changed. Play mode was not entered and the scene was not saved or reopened. Report/evidence files are outside Assets. The actual active scene is `Assets/Scenes/BedroomPrototype_PlayerFigure.unity`; `BedroomPrototype.unity` also exists and was not substituted. The inspected scene was clean in Edit mode.

## 1. Readiness by object

| Object | Current separation | Safe replacement boundary | Fragility / verdict |
|---|---|---|---|
| Player | CharacterController, input, flashlight, breath and blanket are on `Player`; camera/audio/attack anchors are separate from `PlayerBody/MaleModel`. | Retain `Player`, camera and all controllers; replace only the art after rebinding `bodyModel`/`bodyAnimator`. A skin-only replacement retaining the current skeleton can be safer. | **High risk for a new rig.** Body fitting uses Humanoid hips and the first SkinnedMeshRenderer; layer 31 is excluded from the gameplay camera. Do not replace the root capsule GameObject. |
| The Truth | `TheTruth` holds `BedroomInteractable`; `TruthInteractionVolume` is a separate trigger; `TruthBody` contains the model/Animator. | Preserve parent and interaction volume, replace/rebind `TruthBody` and its Animator/controller/Avatar. | **Conditional.** Best character separation, but desk pose, body offset and 0.592 model scale are hand-fitted. A new adult-sized model requires a new fit pass. `FutureLookTarget` is only a marker, not an implemented look-at system. |
| Stained-glass corpse | Stable `StainedGlassFigurePlaceholder` parent; art under `CorpseBody`, `StainedGlassShell`, legacy primitive children and `GlassFragments`. No collider or gameplay script was found on this character. | Preserve the parent and replace the complete coordinated visual/rig assembly after planning the glass skin. | **High risk for skeleton replacement.** Both active glass-shell renderers use 52 bones/rootBone from `CorpseBody/MaleModel`. Replacing only that model leaves stale skin bindings. There is also an inactive female rig and disabled legacy primitive renderers. |
| Bed | `Bed` owns interaction script and sleeping/camera/body markers. | Preserve root, anchors and interaction volumes; art-only replacements must retain/recreate the collider proxies separately. | **Conditional.** Most parts combine MeshRenderer, MeshFilter and BoxCollider on the same scaled primitive. Player posing also directly references the protagonist pillow collider. |
| Door | Stable `MainDoorHinge`, separate monster root and anchors. | Preserve hinge, handle pivot, physical door collision and existing audio objects; replace mesh children after a pivot/scale plan. | **Conditional.** Door/handle currently combine art and colliders; handle is itself the animated primitive and parents a sound source. A whole-door FBX replacement would discard these links. |
| Window | `Window` owns panel logic; separate stationary frame, moving hinge, latch, close-interaction proxy and sources. | Preserve panel logic, hinge, latch and closing interaction; rebind pane renderer and moving collision if art changes. | **High/conditional.** Lighting directly drives the current pane renderer. Opening rotates a Unity-owned hinge. Door/window colliders are still on visible primitives. |
| Under-bed monster | `UnderBedMonster` owns AI; encounter anchors are siblings of a `MonsterVisual` prefab. | Keep AI/anchors/configs; replace the visual only after binding four reveal groups, head, rig/pose and attached audio. | **Highest risk.** Procedural Y squash, segmented skinned meshes and hard-coded reveal fractions are specific to the placeholder presentation. |
| Door monster | Encounter logic and movement anchors are outside `DoorMonsterVisual`. | Retain encounter root/anchors; rebind the imported model's head, Animator and listening audio when changing the model. | **Medium/high.** Serialized head bone, head-axis assumptions and attack head alignment remain art dependencies. Listening audio is still inside the visual hierarchy. |
| Window monster | Encounter logic, movement anchors and voice source are outside the visual. | Retain root/anchors; explicitly replace head/chest bindings, head target, upper-body mesh/Animator and visual-owned retreat audio. | **High.** Flashlight defense targets a collider under the old head bone. The model is a custom upper-body-only mesh, not an arbitrary full humanoid. |

“Safe” means preserving gameplay objects and deliberately reconnecting visual references, not dragging an FBX onto any existing scaled primitive. Existing cubes use scale to express dimensions. An FBX already modeled in meters would be scaled twice if parented/replaced without compensation.

## 2. Fragile references and actual script dependencies

### Player camera, seated pose and lying pose — priority P0

Source: `Assets/BedroomPrototype/Gameplay/Scripts/BedroomPhaseController.cs:92–110`.

- `bodyModel` and `bodyAnimator` reference `Player/PlayerBody/MaleModel` directly. `Start` caches that object's original position, rotation and controller.
- Seated placement requires `bodyAnimator.GetBoneTransform(HumanBodyBones.Hips)` and places hips at chair collider maximum Y plus **0.10m**. There is no invalid-Avatar/missing-hips fallback here.
- Lying placement selects **the first** `GetComponentInChildren<SkinnedMeshRenderer>()`, calls `BakeMesh`, scans its vertices and uses the world bounds. It is not an aggregate of body, clothing, head and accessories. A different child order can make a coat or accessory determine the entire body position; an absent/empty skin is also unsafe.
- Lying orientation is hard-coded as X 90° then Y 180°. Player root rotation is set to identity. `sleepingSpot.rotation` and `nightBodyAnchor.rotation` are not used to orient the body. Rotating the bed/marker alone therefore does not correctly rotate the pose.
- Lowest model point is placed at protagonist-pillow top + **0.01m**, not mattress top. With current dimensions this targets approximately **Y=0.77m**, while mattress top is **Y=0.64m**. This is a code-derived fit constraint, not a claim of a new Play-mode visual test. Head and torso contact should be independent.
- Camera anchors are independent and correctly use anchor rotation. They do not automatically follow a replacement character's eyes. The active camera excludes **layer 31**, which currently contains the protagonist body. New body/clothing children on default layer 0 could obstruct the first-person view.

**Recommended before replacement:** a stable visual slot, explicit primary-renderer/Animator references, rig validation, and separate seat/hips, mattress/body-contact and pillow/head-contact sockets. Use anchor rotation instead of literal model rotations. These are recommendations only; no change was made.

### Under-bed emergence and retreat — priority P0

Source: `UnderBedMonsterAI.cs:108–110, 167–171, 211–237` under `Assets/BedroomPrototype/Gameplay/UnderBed/Scripts/`.

- `head` is a direct `B-head` Transform reference. Renderer arrays point to four custom skinned mesh parts: `EmergingHand`, `HeadAndNeck`, `TorsoAndArms`, `CrouchedLegs`; head group also contains two placeholder eye renderers.
- Emergence interpolates hidden ? emerging ? exposed anchors. The split at **28%**, reveal thresholds **3% / 36% / 58%**, retreat splits and **Y scale 0.18?1** are literal values in code. Hide resets the visual root to `(1,0.18,1)` and attack forces `(1,1,1)`.
- A full single-renderer creature will not reproduce the current staged reveal by merely assigning it to one array. Reusing old segmented meshes with a new skeleton does not replace their geometry/weights.
- Whole-visual nonuniform scaling also affects its rig and visual-owned source transforms. The monster follows Transform interpolation, not physics collision avoidance or navigation; a wider/new creature can clip bed legs even if furniture colliders remain correct.
- Five audio sources are children of `MonsterVisual`. Scratching/safe sources are outside it under the hidden anchor. Deleting the whole visual loses the first group.

**Decision needed before final creature topology:** retain four separately skinned reveal groups, or later refactor to authored crawl/emerge animation or a deliberate reveal shader. Prefer a real low crawl pose over squash. This decision affects modeling and rigging, not just Inspector wiring.

### Door hinge, listening position and head alignment — priority P1

Source: `DoorMonsterEncounter.cs:22, 65–71, 77–93` in `Gameplay/Door/Scripts/`.

- `hinge`, `handle`, `visual`, `head` and four movement/attack anchors are serialized Transform references. Opening rotates hinge **local Y**; handle turns/rattles around **local Z**. Initial rotations are captured in `Awake`.
- Monster movement interpolates visual-root position to `DoorwayListening`; it does not find the doorway from the custom door's dimensions or calculate standing clearance.
- Listening head look runs after Animator evaluation, directly setting head world rotation with `Quaternion.LookRotation`. It assumes a compatible head forward axis; a different Blender bone orientation needs a corrective socket/rotation offset.
- Attack positioning subtracts the visual-local head offset from the camera attack anchor. This is partly proportion-aware, but assumes the visual-root transform/scale and head pose agree with the calculation. New root motion or animated head offsets can change the result.
- Listening audio lives under the old visual. Attack and moving footstep sources are outside it; hinge sounds are outside the monster but still depend on door/handle objects.

Preserve the Unity hinge and listening anchors. Reconnect head/look/audio sockets explicitly; do not author a replacement door animation that fights the scripted hinge.

### Window head target and flashlight obstruction — priority P0

Sources: `Gameplay/Window/Scripts/WindowMonsterEncounter.cs:28,77–94` and `NightFlashlightController.cs:66–74`.

- `head` references `B-head`. The field named `upperChest` actually references **B-chest**; the current Humanoid Avatar has no mapped UpperChest. New binding should support Chest/optional UpperChest rather than demand an identical skeleton path.
- `LateUpdate` applies a literal **35° local-X chest bend** and directly looks the head at the camera. These overwrite part of the Animator pose and depend on bone axes/rest rotations.
- `WindowFaceTarget` is a sphere trigger attached under the existing head, local offset **(0,0.07,0.08)m**, radius **0.13m** before parent scale. The AI references that exact Collider instance.
- Flashlight aiming uses `face.bounds.center`, angle/range, and a sorted raycast including triggers. It succeeds only when the ray reaches that exact collider before a solid obstruction. Adding a solid head MeshCollider, a whole-body collider, or an extra solid window pane can block the beam before the target. Transparent glass materials do not make their physics colliders transparent to this ray.
- Current mesh is `WindowUpperBody.asset` on the HumanM rig. An unmasked full-body replacement may expose legs/torso outside the intended opening even when the script still works.
- Motion anchors are visual-root placements, not head-center locations. Do not use `WindowHeadPosition` as the intended face center without posing the rig.
- Retreat audio remains under the visual. Voice/attack source is on the stable encounter root and follows the head.

Plan a head-following gameplay target/socket outside the replaceable FBX, with explicit binding to the new head; preserve collider identity where possible. Recalibrate radius, offset, bone-axis correction and upper-body silhouette after import. Keep the silent-open-window branch, exposure duration, battery and audio rules intact.

### Bed interactions, window art and collider hierarchy — priority P1

- `Bed` carries `BedroomInteractable`; `NightBedInteractionVolume` is a separate trigger, normally disabled in Edit mode and enabled by player state. Other bed colliders allow ancestor-based interaction resolution.
- `PlayerInteractor.cs:88–101` raycasts and searches `GetComponentsInParent<MonoBehaviour>()` for `IInteractable`. Names are not the key, **ancestry and colliders are**. Putting an interactable on a sibling of its collider, or adding a nearer non-interactive solid collider, can break E interaction.
- `CorpseSleepingSpot` is currently a marker with no runtime consumer found. It does not automatically position the corpse. `PlayerSleepingSpot` is consumed, but its rotation is not honored by body placement.
- Mattress, pillows, frame, blanket, headboard and legs have colliders on their visible primitive objects. The chair-seat collider is also used for player placement. Removing art objects wholesale removes collision/contact data.
- `BedroomWindowPanel` controls hinge and latch directly. The visible animated latch and the stationary **Window closing handle/WindowHandleInteractionVolume** are distinct objects. Do not merge them during replacement.
- `TimeOfDayLightingController.cs:77–78,149` directly reads/writes `_BaseColor` and `_EmissionColor` on the referenced pane renderer/material. A new pane requires explicit renderer reassignment and a compatible URP shader/material. `BedroomPhaseController` also retains a pane reference.

### Corpse skin, animation reuse and hierarchy — priority P0/P1

- Active `StainedGlassShell/DarkLeadBacking` and `ColoredGlassPanels` are **SkinnedMeshRenderers**, each bound to **52 bones** and rootBone `CorpseBody/MaleModel/.../B-hips`. They are not independent material-only overlays. A new skeleton requires re-skinning/rebinding or new coordinated meshes.
- Active male rig plus glass shell coexist with an inactive female `VisualModel` and disabled legacy primitive renderers. Do not treat every enabled Renderer property as visible: some are under inactive parents.
- `CorpseBody` is already rotated 90° and its imported model has additional rotation/offset compensation. These are prototype fit transforms, not a desirable new FBX bind pose.
- Preserve the empty parent and intentionally select the authoritative future visual. Do not delete legacy objects in this audit or assume the marker drives placement.

### Animation and names

All **seven inspected Animator components**, including the inactive legacy female one, have valid Humanoid Avatars and root motion disabled. They mostly reference the same third-party `HumanM_Model.fbx`; the female legacy uses HumanF. **Do not overwrite that shared FBX with a custom model**: player, Truth, corpse and all monsters depend on it. Add new art assets later and rebind each project-owned wrapper separately.

| Controller/character | Current motion | Portability concern |
|---|---|---|
| PlayerBody; door/window idle | HumanM@Idle01 | Humanoid muscle motion plus extra hierarchy-bound Transform curves. Core retargeting can work; helper/prop curves need review. |
| PlayerDesk | PlayerDeskStatic | 102 Animator bindings, Humanoid static pose; new proportions still require seat fitting. |
| PlayerBed and corpse | Corpse_RestingStatic_Humanoid | Shared Humanoid static pose. Editing it for the corpse also changes the protagonist's bed pose. |
| TruthDesk | Truth_DeskPerch base; Talk01/Idle01 extra layers | Static fitted perch. Extra Talk/Idle layers have confirmed default weight 0; no runtime gameplay script found driving Truth Animator parameters or conversation animation. Imported talking motion is not equivalent to automatic lip sync. |
| UnderBedMonster | UnderBedCrouchStatic | Humanoid static crouch plus procedural root movement/scale/reveal, not a complete crawl/emerge animation sequence. |

Core runtime gameplay scripts inspected do not use placeholder GameObject/mesh names through `GameObject.Find`, `Transform.Find` or `GetChild` to resolve these models. Most dependencies are **serialized object references**, renderer selection order, bone axes, skin bindings and clip binding paths. Renaming a retained GameObject is different from deleting/replacing its referenced Transform. Exact Kevin Iglesias bone names are not intrinsically required for Humanoid retargeting, but the current direct references must be rebound deliberately.

### Which prefab assets actually own the current scene art

Use these observed connections when planning a future replacement; similar names elsewhere are not proof of a live link:

- `Player` is an instance of `Assets/BedroomPrototype/Prefabs/Player.prefab`; `Player/PlayerBody` is nested from `Assets/BedroomPrototype/Characters/Player/PlayerBody.prefab`.
- `TheTruth` uses `Assets/BedroomPrototype/Characters/TheTruth/TheTruthDesk.prefab`.
- The corpse parent uses `Assets/BedroomPrototype/Characters/Corpse/StainedGlassCorpse.prefab`, with additional nested visual/model instances.
- Under-bed `MonsterVisual` uses `Assets/BedroomPrototype/Gameplay/UnderBed/Visuals/UnderBedMonsterVisual.prefab`.
- Door and window `.../MonsterVisual` scene containers have **no connected prefab source**; their imported HumanM model children do. Project-owned DoorMonsterVisual/WindowMonsterVisual prefab files exist, but editing those files alone is not a reliable way to update these current scene containers. Inspect scene references/overrides explicitly in the later implementation.
- `Characters/Prefabs/TheTruth.prefab`, `Prefabs/StainedGlassFigurePlaceholder.prefab` and other similarly named legacy assets should not be assumed authoritative for this scene.

## 3. Recommended fixes before committing final models

These are proposed follow-up changes for review, not implemented work.

1. **P0 — agree on an asset contract:** stable unit-scale gameplay roots, explicit visual slots, Unity-owned collider/interaction proxies and anchor sockets. Never make imported FBX hierarchy the owner of gameplay components.
2. **P0 — player pose adapter:** replace first-renderer bounds fitting with explicit rig/contact references, valid-Avatar checks and anchor-based orientation; separate torso/mattress contact from head/pillow contact.
3. **P0 — choose under-bed reveal strategy before final topology:** either four skinned groups matching the present reveal contract or authored emergence with a new presentation adapter. Remove squash only in an approved follow-up.
4. **P0 — define corpse glass rig contract:** body, lead backing and colored panels must share the new skeleton and bind poses. Reuse materials where appropriate, not old mesh bone arrays.
5. **P0 — stable window head target:** a gameplay proxy with explicit head attachment and bone-axis correction; prevent new render/collision geometry from blocking its raycast.
6. **P1 — character bindings:** bind semantic Hips/Head/Chest and optional look/eye/audio sockets, support Chest fallback, validate missing references in the Editor. Retargeting an Avatar alone does not reconnect scripts or skin renderers.
7. **P1 — keep all sources outside replaceable art:** preserve source objects/config assets and attach moving sound positions through sockets. Under-bed visual sources, door listening source, window retreat source and door-handle source need particular care.
8. **P1 — separate furniture collision from art; standardize pivots:** keep contact heights/interaction triggers and hinge/latch mechanics independent. Preserve pane shader property support or introduce a reviewed lighting adapter.
9. **P2 — add a replacement checklist/validator:** refs, avatar validity, mesh groups, layers, source counts, skin-bone ownership, collider reach/occlusion, anchor clearance and root-motion settings. Test normal and already-open window modes after any future swap.

A proposed target layout is `GameplayRoot / VisualSlot`, `GameplayRoot / Collision`, `GameplayRoot / Anchors`, and `GameplayRoot / Audio`. These are conceptual responsibilities; existing objects should be preserved/reorganized only after approval, not recreated automatically.

## 4. Practical Blender modeling specification

### Common delivery contract

- Use real metric dimensions: design for **1 Unity unit = 1 meter**. Export a test FBX and verify dimensions, upright **Unity +Y**, facing **Unity +Z**, with +X to the right. Blender's authoring up axis differs; verify the imported result rather than relying on one untested export-axis preset.
- Prefer final Unity art/root scale **(1,1,1)**, with intentional object rotations/scale resolved in the export workflow. Do not encode scene/world placement in the mesh or animation. Keep a documented export preset and a 1m reference object outside the final delivery selection.
- Export selected meshes/armature, not Blender cameras, lights or the whole bedroom. Supply the .blend master and FBX/textures separately. Add custom assets under a new project-owned art location later; preserve original third-party imports.
- Separate moving door/window parts and provide clear semantic names. Names help humans but must not become new string-based gameplay lookups.
- Deliver UVs, normals and sensible material slots. Use URP-compatible Unity materials; do not rely on Blender-only procedural shaders. For glass, separate lead/backing and colored panels, and preserve the ability to author `_BaseColor`/emission on the window pane. Transparency/glow needs a later visual review.
- Do not model collision as detailed render geometry by default. Supply a documented simple collision envelope; Unity-owned Box/Capsule/trigger proxies should remain the gameplay authority.

### Measured scene targets and proposed pivots

The dimensions below are current geometry/contact constraints, not a requirement to bake prototype offsets into Blender origins. All coordinates are current Unity world coordinates; X/Y/Z means meters.

| Asset | Size / clearance target | Preferred Blender pivot and parts |
|---|---|---|
| Bedroom shell | Approximately **5.0 × 5.5m** interior; wall height **2.8m**. Existing room name still says 4m × 5m. | Floor-level modular origins; keep doorway/window openings aligned with existing proxies. |
| Double bed | Mattress **2.10 × 2.10m**, thickness **0.20m**, top **Y=0.64**. Frame **2.16 × 2.18m**, underside **Y=0.31**. Blanket top **0.69**; pillow tops **0.76**. | Bed art origin at floor-level footprint center; separate frame, mattress, two pillows, blanket and headboard. Current `Bed` gameplay root is at world origin while mattress center is **(-1.30,0.54,0.25)**, so a future visual-slot offset is needed. Do not move the gameplay root to compensate. |
| Door leaf | Approximately **1.06m wide × 2.20m high × 0.08m thick**, within a **1.10 × 2.20m** wall opening. | Vertical hinge-edge pivot at floor; separate leaf and handle/spindle. Current hinge **(-0.34,0,-2.79)** rotates local Y; handle turn is local Z. Placeholder leaf extends about 4cm behind the hinge: review that tolerance rather than blindly baking it into final geometry. |
| Window | Moving pane **2.00 × 1.40m**, current thickness **0.04m**; vertical extent about **Y=1.00–2.40**. | Stationary frame/sill separate from sash/pane/crossbars; pivot at left vertical edge, mid-height. Current hinge **(-0.96,1.70,2.85)** rotates local Y. Separate latch pivot and closing-interaction proxy. Do not add a fixed solid pane that blocks the opened window. |
| Chair/desk contact | Chair seat top **Y=0.515m**; desktop top **Y=0.82m**. | Furniture origins at floor-level footprint center; seat and desktop contact surfaces independent from decorative geometry. |
| Player | Existing capsule **1.80m high**, radius **0.25m**, center local **Y=0.90**. Eye local **Y=1.60m**. | Human model origin at floor midpoint between feet; neutral upright bind pose. Proposed adult height **1.7–1.8m**; fit art to controller, not controller scale to art. Preserve first-person body layer 31 or explicitly review a different visibility solution. |
| The Truth | Proposed adult scale **1.7–1.8m** unless deliberate stylization. Current model scale **0.592** is a special desk-perch fit, not a recommended adult export scale. | Floor-centered character root; seated/perched pose delivered separately. Fit hips, hands and gaze to desk after import. Parent is **(1.80,0,2.04)**, facing 180°. |
| Corpse | About **1.7m standing** as the initial art target; lying silhouette roughly **0.7–0.8m wide**, within one **1.05m** bed half, and no more than the mattress length. Existing shell is about **0.70 × 1.93m** in width/length. | Upright rig bind pose with floor-centered root; lying pose as animation, not a permanently sideways armature. Shared skinning for lead and glass. Arms should fit the lying silhouette, independent of bind-pose T-pose width. |
| Under-bed creature | Genuine hidden/crawl thickness should fit about **0.28m** above its **Y=0.025m** anchor beneath the **0.31m** frame clearance. Current squash is a workaround. | Floor-level movement root; skinned limbs/face with usable crawl pose. If retaining current code, four separately assignable skin groups and staged visibility are required. Validate elbows, head and bed-leg clearance. |
| Door monster | Keep standing silhouette inside the **1.10 × 2.20m** opening; suggested initial height **1.7–1.9m**, width under about **0.8m**. Larger designs need a reviewed lean/crouch and anchor fit. | Floor-centered movement root and defined head/look/audio attachment. In-place idle; root motion off. |
| Window monster | Head/upper-body silhouette must fit the **2.0 × 1.4m** opening and allow a roughly **0.26m-diameter** gameplay head target to follow the visible face. | Floor-centered character root for compatibility, or a clearly documented upper-body root with a future offset adapter. Deliver upper-body-only/masked rendering or a plan to hide lower body. Current final root anchor is **(-0.55,0.60,2.95)**, not the face center. |

### Rig and animation requirements

- For easiest reuse, deliver valid **Unity Humanoid** rigs for player, Truth, corpse and humanoid monsters, with mapped Hips, spine/chest, head, arms/hands and legs/feet. UpperChest is optional; the current rig uses Chest. Provide a clean neutral T/A pose and stable bind poses.
- Bone names may be your own consistent convention. Provide a semantic bone/socket map, especially Hips, Head, Chest/UpperChest, eye/look direction and audio/head-hit locations. The imported head bone's local axis need not match the existing `B-head`; a later adapter should correct it.
- Every skinned part must use the same intended armature, compatible bone array and bind poses. Do not attach old glass/segmented skin meshes to a new armature without re-skinning.
- Keep animations **in place**, with root motion disabled in Unity. Existing scripts own movement to encounter anchors and camera contact. Do not combine Blender root translation with these scripted moves.
- Required starting presentations: player idle/seated/lying; Truth static desk perch or seated idle; corpse static resting pose; under-bed crouch/crawl presentation; door/window stationary idle plus procedural look. Custom attack/emergence clips will not automatically play merely because they are imported; current monster controllers largely have one static/idle state and no transition parameters.
- A non-humanoid/Generic monster is possible later, but requires bespoke clips/controller and explicit presentation bindings. It is not compatible with the present Humanoid pose clips by default. Facial blendshapes and lip sync are not required by current gameplay.

### Anchor reference sheet for later fitting

| Anchor | Current position | Notes |
|---|---|---|
| PlayerSleepingSpot / NightBodyAnchor | **(-0.775,0.70,0.18)** | Current marker rotation X90; body code does not honor it. |
| CorpseSleepingSpot | **(-1.825,0.70,0.18)** | Marker only, no runtime follower found. |
| NightCameraAnchor | **(-0.775,1.00,0.95)** | Euler **(12,180,0)**; independent from model eyes. |
| MorningBodyAnchor | **(1.20,0.03,1.48)** | Seated hips subsequently fitted to chair. |
| MorningCameraAnchor | **(1.20,1.22,1.52)** | Euler about **(10,15,0)**. |
| WindowNightStandingExit | **(0.09,0.03,1.77)** | Keep capsule clearance and walking aisle. |
| Under-bed hidden / emerging / exposed | **(-1.10,0.025,-0.38)** / **(0.78,0.025,-0.85)** / **(0.42,0.025,-0.38)** | Yaw about **318.06°**; positions describe movement root, not head. |
| Door outside / listening / distant | **(0.15,0.02,-3.65)** / **(0.15,0.02,-2.66)** / **(0.15,0,-7)** | Preserve doorway clearance and outside sound direction. |
| Window hidden / approach / head / retreat | **(-0.55,0.60,4.30)** / **(-0.55,0.60,3.65)** / **(-0.55,0.60,2.95)** / **(-0.55,0.60,4.30)** | Yaw **180°**; final pose also includes procedural chest bend. |
| Under-bed / door / window attack anchors | Camera-local Z **0.43 / 0.50 / 0.45m** | Preserve camera children; actual monster head alignment is recalculated from the rig. |

## 5. Verification and review boundary

Inspected 725 transforms, 191 renderers, 53 colliders, 7 Animator components, 27 project MonoBehaviour instances and 10 project-owned Animator Controllers. Zero missing scripts or unresolved non-null serialized object references were found. Current Console reports no compilation failure, errors or warnings. Final live state remained clean in Edit mode with the same 725 transforms, 26 AudioSources, one active camera and one active AudioListener. SHA-256 hashes of all three existing scene assets matched the pre-audit hashes. Some colliders under inactive/disabled objects report zero world bounds in Edit mode; local dimensions and hierarchy were used instead. Skinned renderer bounds in Edit mode are not reliable posed body measurements.

No Play-mode tests, new-model imports, runtime pose baking, camera movement, scene saves or structure changes were performed. Runtime clipping, lighting, skin deformation and animation retargeting must be tested after a reviewed replacement. The next implementation step should be the agreed separation/binding fixes above, followed by one representative custom character and one hinged prop import, not a simultaneous replacement of every placeholder.

Evidence: `ReplacementAudit/SceneInspection.json`, `Details.json`, `Controllers.json`, and `PreservationCheck.json`. Scene file hashes and final live dirty state are checked separately. Existing scenes, objects, scripts, encounter scheduling and audio assignments remain intact.



