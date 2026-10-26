using UnityEngine;
[CreateAssetMenu(menuName="Bedroom/Door encounter configuration")]
public sealed class DoorEncounterConfig : ScriptableObject
{
 public bool encounterEnabled=true;
 [HideInInspector] public float initialDelay=25;
 [Min(0)] public float footstepsDuration=5, approachSilence=.7f, gentleDuration=4, aggressiveDuration=5, silenceAfterBanging=1.1f, handleDuration=.5f, openingDuration=2, reactionGrace=.75f, listeningDuration=3, retreatDuration=3, attackDuration=.7f;
 public Vector2 cooldown=new Vector2(20,30);
 public float openingAngle=95;
 public AudioClip distantFootsteps, approachingFootsteps, gentleKnocking, aggressiveKnocking, handleTurning, doorCreaking, monsterListening, retreating, attacking;
 public AudioClip doorClosing;
 [Header("Audio levels and pauses")]
 [Range(0,1)] public float approachStartVolume=.12f,approachEndVolume=.55f,retreatStartVolume=.5f;
 [Range(0,1)] public float gentleStartVolume=.18f,gentleEndVolume=.32f,aggressiveStartVolume=.42f,aggressiveEndVolume=.72f;
 [Min(0)] public float gentlePauseStart=.65f,gentlePauseEnd=.4f,aggressivePauseStart=.3f,aggressivePauseEnd=.1f;
 public bool loopFootsteps=false;
 public bool textCues=true, logResults=true;
}

