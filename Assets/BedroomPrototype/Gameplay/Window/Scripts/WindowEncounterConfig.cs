using UnityEngine;
[CreateAssetMenu(menuName="Bedroom/Window encounter configuration")]
public sealed class WindowEncounterConfig : ScriptableObject
{
 public bool encounterEnabled=true;
 [HideInInspector] public float initialDelay=30;
 [Min(.1f)] public float knockingDuration=5,openingDuration=2,emergingDuration=2,reactionWindow=2,requiredExposure=.8f,retreatDuration=1.5f,attackDuration=.6f,successCooldown=20,safeClosedCooldown=20;
 [Min(.1f)] public float minimumKnocking=3,minimumEmerging=1.4f;
 public float earlyOpenMultiplier=1.5f,middleOpenMultiplier=2,maximumOpenMultiplier=3,firstDangerThreshold=15,secondDangerThreshold=30;
 public AudioClip quietKnock,aggressiveKnock,latchRattle,opening,monsterBreathing,retreating,closing;
 public AudioClip attacking;
 [Range(0,1)] public float quietKnockVolume=.2f,aggressiveKnockVolume=.65f;
 [Range(0,1)] public float monsterBreathingVolume=.14f,attackVolume=.8f;
 [Range(0,256)] public int breathingPriority=100,attackPriority=20;
 [Min(0)] public float knockPauseStart=.4f,knockPauseEnd=.12f;
 public bool textCues=true,logResults=true;
 public bool resetExposureOnMiss=true;
 [Min(0)] public float exposureDecayPerSecond=2;
 [Min(.1f)] public float silentApproachDuration=1.2f;
 [Min(.1f)] public float silentEmergingDuration=2.5f, silentReactionWindow=2.5f;
 public bool showSilentAttackDebugCue=false;
 public float DangerWeight(float openSeconds)=>openSeconds<firstDangerThreshold?earlyOpenMultiplier:openSeconds<secondDangerThreshold?middleOpenMultiplier:maximumOpenMultiplier;
}



