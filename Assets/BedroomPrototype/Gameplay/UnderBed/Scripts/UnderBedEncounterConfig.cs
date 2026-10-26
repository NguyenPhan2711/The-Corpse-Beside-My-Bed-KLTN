using UnityEngine;

[CreateAssetMenu(menuName = "Bedroom/Under-bed encounter")]
public sealed class UnderBedEncounterConfig : ScriptableObject
{
    public enum EarlyReleaseRule { Attack, OneWarning }
    public bool encounterEnabled = true;
    [Tooltip("Standalone/external coordinator only. BedroomEncounterDirector uses its shared initial scheduling delay.")]
    [Min(0)] public float initialDelay = 15;
    [Min(.01f)] public float scratchingDuration = 4;
    [Min(.1f)] public float emergingDuration = 5;
    [Min(.1f)] public float reactionWindow = 1.75f;
    [Min(.1f)] public float requiredHidingDuration = 3;
    [Min(.1f)] public float retreatDuration = 2;
    public Vector2 cooldownRange = new Vector2(15, 25);
    [Min(.1f)] public float attackDuration = .5f;
    [Min(.01f)] public float headTurnDuration = .3f;
    [Min(.1f)] public float directorRetryDelay = 1;
    public EarlyReleaseRule earlyReleaseRule = EarlyReleaseRule.OneWarning;
    [Min(.1f)] public float warningReactionWindow = .85f;
    public bool showTextCues = true;
    public bool logDefenses = true;
    public AudioClip scratching, emerging, exposed, breathing, retreating, safeToUncover, attacking;
    [Tooltip("Enable only after auditioning a clean loop seam.")] public bool loopScratching=false;
}

