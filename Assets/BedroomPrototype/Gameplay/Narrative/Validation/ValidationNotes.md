# Validation notes

Tests use the existing scene in Play mode, virtual Input System devices and temporary test configs. No test scene is created. Full results are in the JSON reports alongside this file. Only completed reports with `passed: true` count as passes.

During development, delayed asset imports caused assembly reloads during two door regression attempts. Those interrupted attempts are excluded from results. One interrupted fixture left a transient test InputSettings instance; the Editor input cache and test devices were restored. The door fixture now restores input on destruction, and an explicitly armed Editor-only test guard also restores input before reload or Play exit. All source imports and compilation were settled before the final reruns. The fresh door rerun completed 29 checks successfully.

The new narrative fixture completed 61 checks, including physical left-click/Enter inputs, typewriter behavior, morning choices, all three introductions, paused resources and clock, reveal events, retries, Q release/gaze/continuous-hold requirements, rapid tapping, camera recovery, corpse success/failure, and a single active camera/listener. The coordination fixture completed 14 checks for director selection, competing defenses, cooldowns and leaving/returning to bed.

Automated tests exercise shortened encounters/configs. They do not establish full-length difficulty balance, narrative pacing, camera comfort, final corpse appearance, future pixelation readability, or standalone-build behavior. Those require manual review. No suitable new creak clip was supplied; the optional corpse cue remains unassigned. No voice sources or speech assets were introduced.

The shared HumanM mesh proves identity reuse in this prototype. It does not provide a finished recognizable face or final decay artwork. Fragment meshes are reusable runtime-baked partitions of the existing shell, rather than final authored fracture geometry.

Final rerun: all 9 suites passed, 451 checks total (61 narrative, 14 coordination, 52 resources, 29 door, 41 under-bed, 48 window, 103 states, 70 morning flow, 33 scheduling). Scene save/reopen bindings passed. Original monster audio references were unchanged; 26 AudioSources, one player/camera/AudioListener/dialogue owner/director, no missing scripts. Other scene hashes are unchanged.
