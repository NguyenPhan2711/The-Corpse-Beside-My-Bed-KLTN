using UnityEngine;
[CreateAssetMenu(menuName="Bedroom/Corpse encounter configuration")]
public sealed class CorpseEncounterConfig:ScriptableObject
{
 [Min(0)]public float initialDelay=20;[Min(.1f)]public float beckoningSeconds=5,reachingSeconds=4,resistanceSeconds=1.75f,retreatSeconds=1.2f,surrenderSeconds=2.5f;
 public Vector2 cooldown=new Vector2(20,30);[Range(5,90)]public float gazeDegrees=35;[Range(0,.3f)]public float maximumCameraDrop=.13f;[Min(.01f)]public float cameraRecoverySpeed=.25f;
 [Range(0,10)]public float warningRoll=1.5f,warningYaw=3;[Range(0,.02f)]public float mattressMotion=.004f;public AudioClip creak;
}

