# Final monster audio assignments

The seven clip identities were confirmed by the user on 2026-10-26. The same mappings are saved in all three nights. All imported files are MP3/stereo. The agent did not audition them.

| Monster | State/event | Assigned file | Duration | AudioSource GameObject (full hierarchy) | Playback | Spatial |
|---|---|---|---:|---|---|---|
| Door  | Approaching | Walking.mp3 | 5.112s | DoorMonsterEncounter/AUDIO - Approaching footsteps - assign clip | one-shot moving/fading | 3D |
| Door  | GentleKnocking | GentleKnocking.mp3 | 1.560s | Room - 4m x 5m/MainDoorHinge/AUDIO - Gentle knocks - assign clip | one-shot groups with pauses | 3D |
| Door  | AggressiveKnocking | AgressiveKnocking.mp3 | 2.232s | Room - 4m x 5m/MainDoorHinge/AUDIO - Aggressive bangs - assign clip | one-shot groups with increasing level/shorter pauses | 3D |
| Door  | Handle | Unassigned (clip unavailable) | — | Room - 4m x 5m/MainDoorHinge/Door handle/AUDIO - Handle turn - assign clip | one-shot | 3D |
| Door  | DoorOpening | OpenDoor.mp3 | 2.520s | Room - 4m x 5m/MainDoorHinge/AUDIO - Door creak - assign clip | one-shot on swing | 3D |
| Door  | Listening | Unassigned (clip unavailable) | — | DoorMonsterEncounter/DoorMonsterVisual/AUDIO - Monster listening - assign clip | loop if assigned | 3D |
| Door  | Retreat | Walking.mp3 | 5.112s | DoorMonsterEncounter/AUDIO - Retreat footsteps - assign clip | one-shot moving/fading | 3D |
| Door  | Closing | Unassigned (clip unavailable) | — | Room - 4m x 5m/MainDoorHinge/AUDIO - Door creak - assign clip | one-shot on closing motion | 3D |
| Door  | Attack | Unassigned (clip unavailable) | — | DoorMonsterEncounter/AUDIO - Attack - assign clip | one-shot; intentional loss tail allowed | 3D |
| Under-bed  | Scratching | Scratching.mp3 | 8.664s | UnderBedMonster/EncounterAnchors/HiddenPosition/AUDIO - Scratching - assign clip | one-shot stopped at Emerging | 3D |
| Under-bed  | Emerging | Unassigned (clip unavailable) | — | UnderBedMonster/MonsterVisual/AUDIO - Emerging - assign clip | one-shot | 3D |
| Under-bed  | FullyExposed | Unassigned (clip unavailable) | — | UnderBedMonster/MonsterVisual/AUDIO - Exposed cue - assign clip | one-shot | 3D |
| Under-bed  | Hiding | Unassigned (clip unavailable) | — | UnderBedMonster/MonsterVisual/AUDIO - Breathing - assign clip | loop if assigned | 3D |
| Under-bed  | Retreat | Unassigned (clip unavailable) | — | UnderBedMonster/MonsterVisual/AUDIO - Retreating - assign clip | one-shot | 3D |
| Under-bed  | Safe | Unassigned (clip unavailable) | — | UnderBedMonster/EncounterAnchors/HiddenPosition/AUDIO - Safe to uncover - assign clip | one-shot | 3D |
| Under-bed  | Attack | Unassigned (clip unavailable) | — | UnderBedMonster/MonsterVisual/AUDIO - Attack - assign clip | one-shot; intentional loss tail allowed | 3D |
| Window  | Quiet knocking (closed only) | KnockingWindow.mp3 | 2.496s | Window/AUDIO - Quiet glass tapping - assign clip | one-shot groups with pauses | 3D |
| Window  | Aggressive knocking (closed only) | KnockingWindow.mp3 | 2.496s | Window/AUDIO - Aggressive glass knocking - assign clip | one-shot groups with rising volume | 3D |
| Window  | Latch (closed only) | Unassigned (clip unavailable) | — | Window/AUDIO - Window latch rattling - assign clip | one-shot | 3D |
| Window  | Opening (closed only) | OpenWindow.mp3 | 2.616s | Window/AUDIO - Window opening creak - assign clip | one-shot | 3D |
| Window  | Breathing (closed only) | Unassigned (clip unavailable) | — | WindowMonsterEncounter/AUDIO - Window monster breathing - assign clip | loop if assigned | 3D |
| Window  | Retreat | Unassigned (clip unavailable) | — | WindowMonsterEncounter/WindowMonsterVisual/AUDIO - Window monster retreat - assign clip | one-shot | 3D |
| Window  | Manual closing | Unassigned (clip unavailable) | — | Window/AUDIO - Window manually closing - assign clip | one-shot at configured normalized animation time | 3D |
| Window  | Attack | Unassigned (clip unavailable) | — | WindowMonsterEncounter/AUDIO - Window monster breathing - assign clip | one-shot; intentional loss tail allowed | 3D |

Already-open window: no approach, knock, latch, opening, emergence or defensive-check warning audio. This suppression was tested with warning sentinel clips assigned. Optional outcome sounds may play only in Retreat/Attack.

Distant footsteps source is retained but idle: the existing approaching source moves from the distant anchor and uses Walking.mp3. This prevents two copies of the same footsteps. Player breathing/gasp sources remain 2D and clips are unassigned. All missing clips are optional; no filename was invented or opening clip repurposed for closing.

Source levels, attenuation, priorities, clip-aware pauses, optional looping and manual closing cue time remain configurable in the Inspector. All active environmental clips are one-shots; scratching/footstep loop seams were not confirmed. A state exit can truncate a knock group to stop it immediately.
