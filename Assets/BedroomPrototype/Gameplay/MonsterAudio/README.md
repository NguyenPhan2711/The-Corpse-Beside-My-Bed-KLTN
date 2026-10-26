# Monster audio integration

Work stays in the existing `Assets/Scenes/BedroomPrototype_PlayerFigure.unity`. No imported audio, import settings, scenes, characters, counters, resource rules, or scheduling settings are replaced.

See `AudioMapping.md` for the seven-file inventory and assignments. **No clip has been personally auditioned by the agent.** The tool rejected audio input as unsupported. On 2026-10-26 the user confirmed all seven mappings from listening; those clips are now assigned in all three nights' existing project-owned configs (27 event references, seven unique files). Generated silent clips exercise missing event hooks only in cloned test configurations; missing production clips remain null.

## Playback changes

- Door: one moving footstep source, configurable fade-up/down, no duplicate distant/approaching tracks; full knock clips with configurable pauses and escalating volume; opening starts at physical swing, not each frame; optional handle and closing hooks use existing sources; natural opening tail can finish into Listening.
- Under-bed: scratching defaults to one-shot because a clean loop seam has not been auditioned; it stops at Emerging. Existing exposed/hiding/retreat/attack hooks are retained. Missing clips stay optional.
- Window: full knock clips never overlap; opening checks closed state and preserves its natural tail into emergence. Already-open attacks remain silent through approach, emergence and the defensive check. Optional attack audio reuses the voice source only after Attack starts. Manual closing has a configurable normalized sound trigger and cancels pending playback on phase reset.
- Loss: incompatible loops stop immediately. Optional attack one-shots can finish naturally without delaying gameplay loss. `preserveAttackTailOnLoss` is configurable on each encounter. Door/window voice source parenting is changed only on scene instances so hidden visuals do not disable their intentional tails. The under-bed prefab hierarchy stays intact and preserves the tail's last world position.

## Sources and Inspector controls

25 existing sources are configured; no sources or mixers are created. Door sources use 3D logarithmic attenuation, minimum 2m/maximum 12m. Under-bed sources use 1m/7m, window sources 1.5m/10m. Environmental sounds have zero Doppler, no Play On Awake, and conservative levels. Warning sources have priority 80; attack sources 20. Player breathing/gasp are 2D at the existing camera (priority 100/25).

Existing source volume, attenuation and priority remain editable. Door configs expose approach/retreat volumes, knock start/end levels, pauses and optional footstep looping. Window configs expose knock levels and pauses, plus separate breathing/attack levels and priorities. Under-bed configs expose `loopScratching` (off until a loop seam is confirmed). `BedroomWindowPanel.closingAudioNormalizedTime` controls the closing/latch trigger. Existing state durations remain unchanged in saved configs.

## Modified runtime scripts

`DoorMonsterEncounter`, `DoorEncounterConfig`, `UnderBedMonsterAI`, `UnderBedEncounterConfig`, `WindowMonsterEncounter`, `WindowEncounterConfig`, `BedroomWindowPanel`. Added only an Editor-only `MonsterAudioPlayModeChecks` test fixture.

## Missing sounds / manual checklist

- The seven primary clip identities were confirmed by the user. Still inspect whether opening clips also contain a separate handle/latch or closing section before reusing them for any additional event.
- Door: dedicated handle, closing, listening and attack clips.
- Under-bed: emergence, distinctive exposed cue, hiding breathing, retreat, safe-to-uncover and attack clips. The missing exposed cue is especially important for an audio-driven counter.
- Window: latch, closing, breathing, retreat and attack clips. Do not assign these as advance warnings to already-open attacks.
- Player: normal/heavy breathing, inhale, gasp and depleted-flashlight sound.
- Audition headphone direction, attenuation, loudness, pauses and combined warnings during real-length nights. Playback/state tests do not assess sound identity, intelligibility, atmosphere or final mix.

See `Validation/` for executed checks and the saved-scene audit. No standalone build or headphone audition is claimed.


