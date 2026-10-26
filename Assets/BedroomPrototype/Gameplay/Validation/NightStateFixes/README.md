# Night state and interaction repair

This update supersedes the earlier window prototype rule that allowed flashlight/breath outside bed. The scene remains BedroomPrototype_PlayerFigure.unity, edited in place.

Root causes found:
- NightFlashlightController and HoldBreathController checked NightGameplay rather than whether the player was in bed. The window AI and director likewise accepted window requests while standing.
- WindowNightExcursion, window closing and bed prompts did not share the director's acquisition checks. Enemy quietInterval was also applied to player actions. An active prompt could silently fail because the enemy lease/cooldown rejected it, and an active enemy lease could prevent the very return needed to defend.
- Window handle was only a 14cm collider. Nearby pane/frame surfaces could win the interaction ray; the expected return region lacked a generous interaction trigger.
- The E action is a Button with a Hold interaction, but existing code uses WasPressedThisFrame, which responds on button-down independently. Only one PlayerInteractor existed; no duplicate E subscriptions or disabled interaction map were found. Input maps are not swapped for movement transitions, because movement and interaction already use separate runtime asset clones.
- A test also caught a fresh first-frame Space press being mistaken for a held transition key. Release tracking now samples physical Input System key state at every player-state boundary and observes releases while actions are disabled.

Shared state stays inside the existing BedroomPhaseController: InBed, GettingOutOfBed, OutOfBed, ReturningToBed, with NightPlayerStateChanged and CanUseNightDefenses. No second player-state controller or input handler is introduced. Movement remains disabled through the full get-up/return fade. The same player, camera, CharacterController and authored bed pose are reused. Return never calls BeginNight or resets encounter timers.

BlanketController, HoldBreathController and NightFlashlightController share this gate. Leaving bed synchronously resets cover/breath and switches off the actual light/action/target detection. Flashlight charge is preserved without standing drain. Keys held across a transition require release; a genuinely fresh press immediately after return is valid. Out-of-bed F/Space/R have no defensive effect.

Window scheduling requires InBed at request, selection and immediate activation. Non-bed state changes clear selected/queued window requests and any exposure. Its cooldown pauses rather than resetting while away; open-window time/danger still accumulates and closing resets danger as before. Diagnostic logs are opt-in on WindowMonsterEncounter.logDiagnostics and emit on state/eligibility changes or actual pending-request removal, not every frame.

BedroomEncounterDirector separates player operation leases from enemy ownership. Enemy quiet cooldown cannot reject player get-up/close/return. Close requires no active enemy; if a warning owns the enemy lease, the player receives a clear instruction to return and defend. Return is always allowed during warning-stage ownership, subject to the shared transition/closing guard. Mandatory player transitions and closing still defer new encounters.

Under-bed and door warning sequences can start and advance while standing. Director outOfBedAttackGrace defaults to10 seconds once the threat reaches its defense stage. Returning provides returnedDefenseGrace (1.5 seconds) before the existing reaction/listening check. The warning is not reset on return; normal counters work only once in bed. Staying outside beyond grace still loses. Existing per-enemy warning timings remain in their configuration assets. The night clock is never paused for this protection.

PlayerInteractor remains the sole E handler. It resolves child colliders through parents, reports allowed/blocked feedback from the same checks used to execute, and dispatches at most once per press/frame. interactionDistance stays configurable (2m). New non-solid triggers enlarge window-handle aim to42x40cm and cover the expected bed return area. The bed trigger is enabled only OutOfBed so morning targeting remains unchanged. A blocked target never shows an active [E] prompt. Pressing during a transition, occupied opening, closing, obstruction, wrong state or excessive distance produces feedback.

For development logs enable PlayerInteractor.logInteractions. Each press reports E received, phase/player state, hit collider, resolved target, distance, Allowed and exact blocked reason. WindowMonsterEncounter.logDiagnostics reports encounter/player state, spawn/detection eligibility, open state, danger and pending request clearing. Both default off to keep normal Console output quiet.

Modified runtime scripts: BedroomPhaseController, PlayerInteractor, BedroomInteractable, BedroomEncounterDirector, WindowNightExcursion, BedroomWindowPanel, WindowCloseInteractable, WindowMonsterEncounter, NightFlashlightController, BlanketController, HoldBreathController, UnderBedMonsterAI and DoorMonsterEncounter. Added only an Editor-only NightStateRepairChecks harness; the existing WindowPlayModeChecks assertion was updated to the new standing-warning rule. No third-party assets, narrative characters or room geometry are replaced.

Repair suite: 103 real-input checks passed, including three complete standing/window/bed cycles, all four ordered states, immediate and held-key behavior, blocked E feedback, canceled pending window requests, real keyboard movement, wide child trigger resolution, preserved clock/danger reset, fair returns during both live monsters, and loss after overstaying outside grace.


Existing encounter regressions passed: 48 window checks, 29 door/breath checks, 41 under-bed/blanket checks, plus 5 victory-during-travel/closing checks. These use temporary runtime configuration/InputSettings clones with accelerated warning timers; they are not claims that full-length night balance or audio mixing has been verified.


Final validation: 70 three-day progression checks also passed, for 296 checks total (103 repair +48 window +29 door +41 under-bed +5 night-end +70 progression). The final saved scene is Edit mode, morning exploration, transition=false, window closed, all three encounters enabled, and contains one player controller/interactor/camera/listener/night manager with zero missing scripts. Standing grace10s and returned defense grace1.5s are saved Inspector defaults. Diagnostic flags are off by default; enable them for development. Manual follow-up: full-length night balance, warning audio readability and comfort of repeated camera fades on the player's hardware. No remaining automated test failure is known.

