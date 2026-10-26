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
    public bool allowOverlappingWarnings;
    [Range(1,4)] public int maximumWarningConcurrency=2;
    public float Weight(BedroomEncounterKind kind) => kind==BedroomEncounterKind.UnderBed?underBedWeight:kind==BedroomEncounterKind.Door?doorWeight:kind==BedroomEncounterKind.Window?windowWeight:kind==BedroomEncounterKind.Corpse?corpseWeight:0;
}

