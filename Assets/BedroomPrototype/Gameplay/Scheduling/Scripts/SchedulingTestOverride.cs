#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
public sealed class SchedulingTestOverride : MonoBehaviour
{
    BedroomEncounterDirector director;NightEncounterSchedule[] originals,clones;bool oldFixed,oldAutomatic,oldNoFakeOuts;int oldSeed;
    public static SchedulingTestOverride Configure(BedroomEncounterDirector director,float initialDelay)
    {
        var scope=new GameObject("TEMP Scheduling test settings").AddComponent<SchedulingTestOverride>();scope.director=director;
        scope.originals=director.nightSchedules;scope.oldFixed=director.useFixedSeed;scope.oldSeed=director.fixedSeed;scope.oldAutomatic=director.automaticScheduling;
        scope.clones=director.nightSchedules.Select(Instantiate).ToArray();director.nightSchedules=scope.clones;
        foreach(var c in scope.clones){c.initialDelay=initialDelay;c.opportunityInterval=new Vector2(.1f,.1f);c.allowOverlappingWarnings=false;c.allowConcurrentDefense=false;c.intervalRamp=AnimationCurve.Constant(0,1,1);c.useShuffleBag=false;c.forceFirstEncounter=BedroomEncounterKind.None;}
        scope.oldNoFakeOuts=director.disableFakeOuts;director.disableFakeOuts=true;
        director.useFixedSeed=true;director.fixedSeed=4281;director.automaticScheduling=true;director.quietInterval=.1f;director.selectionInterval=.05f;return scope;
    }
    void OnDestroy(){if(director){director.nightSchedules=originals;director.useFixedSeed=oldFixed;director.fixedSeed=oldSeed;director.automaticScheduling=oldAutomatic;director.disableFakeOuts=oldNoFakeOuts;}if(clones!=null)foreach(var c in clones)if(c)Destroy(c);}
}
#endif
