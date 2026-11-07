using UnityEngine;
public enum BedroomEncounterKind { None=-1, UnderBed=0, Door=1, Window=2, Corpse=3 }
[CreateAssetMenu(menuName="Bedroom/Night encounter schedule")]
public sealed class NightEncounterSchedule : ScriptableObject
{
    [Min(0)] public float initialDelay = 18;
    public Vector2 opportunityInterval = new Vector2(8,14);
    [Min(0)] public float underBedWeight=1, doorWeight=1, windowWeight=1;
    [Min(0)] public float corpseWeight;
    [Tooltip("Only honored on Night 1. None gives a genuinely random opening.")]
    public BedroomEncounterKind forceFirstEncounter = BedroomEncounterKind.None;
    [Tooltip("Every positive-weight threat appears once, in random order, before any repeats.")]
    public bool useShuffleBag;
    [Tooltip("If no threat left in the bag becomes eligible within this time, use weighted selection without consuming the bag.")]
    [Min(0)] public float shuffleBagFallbackSeconds=6;
    public bool allowOverlappingWarnings;
    [Range(1,4)] public int maximumWarningConcurrency=2;
    [Tooltip("A second threat reaching its defense stage starts its timer instead of waiting for the first defense to finish.")]
    public bool allowConcurrentDefense;
    [Tooltip("Extra reaction time given to a threat whose defense starts while another defense is running.")]
    [Min(0)] public float concurrentDefenseGrace=.75f;
    [Header("Hourly ramp")]
    [Tooltip("x = night progress (0 = 12 AM, 1 = 6 AM), y = multiplier. Scheduling intervals are divided by this value.")]
    public AnimationCurve intervalRamp=new AnimationCurve(new Keyframe(0,1),new Keyframe(5f/6f,1.3f),new Keyframe(1,1.3f));
    [Header("Resources")]
    [Min(0)] public float flashlightRechargePerSecond=5;
    public float RampMultiplier(float progress)=>intervalRamp==null||intervalRamp.length==0?1:Mathf.Max(.1f,intervalRamp.Evaluate(Mathf.Clamp01(progress)));
    public float Weight(BedroomEncounterKind kind) => kind==BedroomEncounterKind.UnderBed?underBedWeight:kind==BedroomEncounterKind.Door?doorWeight:kind==BedroomEncounterKind.Window?windowWeight:kind==BedroomEncounterKind.Corpse?corpseWeight:0;
}

