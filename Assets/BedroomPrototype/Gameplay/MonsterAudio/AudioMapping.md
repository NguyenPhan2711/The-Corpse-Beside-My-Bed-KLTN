# Audio mapping prepared before integration

Audio cannot be auditioned by this session. File metadata was read from Unity; sound identity and suitability remain subject to the user's requested listening confirmation. All files are MP3, stereo. Imported files and import settings will remain untouched.

| Folder | File | Seconds | Proposed event | Playback plan |
|---|---|---:|---|---|
| MainDoorMonster | Walking.mp3 | 5.112 | Approaching and retreating footsteps | One moving source at a time; fade up/down; no unverified looping |
| MainDoorMonster | GentleKnocking.mp3 | 1.560 | Gentle knocking | One clip at a time, then pause |
| MainDoorMonster | AgressiveKnocking.mp3 | 2.232 | Aggressive knocking | One clip at a time; rising volume and shorter pauses |
| MainDoorMonster | OpenDoor.mp3 | 2.520 | Door begins physically opening | One-shot; allow natural tail into Listening |
| UnderTheBedMonster | Scratching.mp3 | 8.664 | Scratching | One-shot below bed; stop when Emerging starts; loop seam not confirmed |
| WindowMonster | KnockingWindow.mp3 | 2.496 | Quiet and stronger glass knocks | Same available clip at different volumes; no overlapping copies |
| WindowMonster | OpenWindow.mp3 | 2.616 | Window starts physically opening | One-shot; allow natural tail into HeadEmerging |

No suitable independently identified clips are available for: door handle/closing/listening/attack; under-bed emerging/exposed/hiding/retreat/safe-to-uncover/attack; window latch/closing/breathing/retreat/attack; player breathing/inhale/gasp; empty battery. These must remain optional and unassigned rather than substituting an unrelated sound. Opening clips will not be reused as closing clips without listening confirmation.

For already-open window attacks: no knocks, latch, opening, approach or emergence warning audio. Future retreat/attack clips may play only after those outcome states begin. No original third-party assets will be edited.

## Confirmation and assignment

On 2026-10-26, the user confirmed all seven proposed mappings from listening. They were assigned to the existing project-owned encounter configs for Nights 1, 2 and 3. Walking is reused for approach and retreat; the single glass-knock clip is reused at different levels. No opening clip is assigned to closing/latching and no unrelated clip is substituted for missing events. No clip is set to loop without a confirmed loop seam.

