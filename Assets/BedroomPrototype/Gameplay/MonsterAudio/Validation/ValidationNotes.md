# Validation details

Executed through Unity CLI against the same Unity 6000.4.1f1 Editor and existing BedroomPrototype_PlayerFigure scene. No test scenes were created.

The audio test fixture temporarily clones encounter configs and uses the seven imported candidate clips for playback timing. Generated silent sentinels cover events for which no suitable clip is imported. This verifies AudioSource state, sample/time advancement, transitions, counters and cleanup; it does not identify the recorded sound, audition the clip, prove a clean loop seam or validate the headphone mix. These test-only assignments are discarded on leaving Play mode.

Tests cover spatial source settings; no morning playback; one approaching footstep track and moving fade; non-overlapping complete knock groups/pauses; rising aggressive level; handle/opening/closing animation points; listening silence; R success/failure; attack once; optional attack tails; scratching under mattress and stopping at emergence; exposed/hiding/retreat hooks; Space defense; closed-window knocking/opening; real F counter; complete silence in already-open approach/emergence/check even when warning sentinel clips are supplied; manual closing once at configured time; danger reset; bed travel; loss/victory cleanup; single camera/listener.

Initial fixture runs found setup issues: a retry quiet interval needed a short wait, a single world-Euler mouse correction was insufficient for the existing tilted bed camera, and nested coroutine exceptions needed explicit collection in the fixture. Those test helper issues were fixed without changing player controls or defense rules. One failed fixture timeout and temporary test-compilation diagnostics were logged during development; final compilation/Console state is reported separately.

The existing accelerated scheduling fixture uses a 1.4-second knocking phase with a one-second sentinel clip. Its temporary knock pause is now explicitly 0.05 seconds so it can test both quiet/aggressive groups with the new clip-aware playback. Saved gameplay timings and pauses are not changed to satisfy that fixture.

SaveReloadCheck.json verifies the original scene was saved and reopened with identical source settings and hierarchy. ImportedFilesUnchanged.json records unchanged hashes for all seven imported MP3s. SceneAudioAudit.json records every actual serialized event/clip/source mapping, including intentional nulls.

Manual checks remain: confirm clip identities, assign missing exposed/attack/closing/breathing/etc. sounds, audition warning direction and relative mix on headphones, check overlaps at real-night pacing, and test a standalone build. No headphone audition or build test is claimed.

## Final results

60 audio assertions passed, plus 33 scheduling/silent-window assertions and 52 resource assertions (145 total across those suites). A separate direct state-entry smoke test verified the configurable quiet-voice and attack volume/priority settings. All seven file hashes remained unchanged. Saving/reopening the same scene preserved all 26 existing source settings/hierarchy and 27 confirmed clip references across the three nights. No source was created. Missing event clips remain intentionally null. The user confirmed all seven audio identities before production assignment.

