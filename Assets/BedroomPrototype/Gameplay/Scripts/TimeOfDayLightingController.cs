using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum StoryTimeOfDay { Morning, Afternoon, Evening, LateEvening }

public sealed class TimeOfDayLightingController : MonoBehaviour
{
    [Serializable] public class LightingProfile
    {
        public Color daylightColor = Color.white;
        [Min(0)] public float daylightIntensity = 1;
        public Vector3 lightRotation = new Vector3(35, 180, 0);
        [Range(0, 1)] public float shadowStrength = .55f;
        public Color ambient = new Color(.25f, .3f, .4f);
        public Color windowColor = new Color(.5f, .7f, .9f);
        [ColorUsage(false, true)] public Color windowEmission = new Color(.2f, .4f, .6f);
    }
    public enum MilestoneKind { LaptopReply, TruthReplyCount, ObjectiveComplete }
    [Serializable] public class StoryMilestone
    {
        public string id;
        [Tooltip("0 applies to every day; otherwise 1, 2 or 3.")] public int day;
        public MilestoneKind kind;
        public string messageOrObjectiveId;
        [Min(1)] public int replyCount = 1;
        [Min(0)] public float weight = 1;
    }
    public MorningTaskManager tasks;
    public RoomLightSwitch ceilingSwitch;
    public Light windowDaylight;
    public Renderer windowPane;
    [Min(.01f)] public float transitionSeconds = 3f;
    [Range(0, 1)] public float afternoonThreshold = .2f;
    [Range(0, 1)] public float eveningThreshold = .6f;
    public LightingProfile morning = new LightingProfile { daylightColor = new Color(.76f, .86f, 1), daylightIntensity = 1.8f, ambient = new Color(.28f, .33f, .40f), windowColor = new Color(.54f, .72f, .86f), windowEmission = new Color(.27f, .42f, .60f) };
    public LightingProfile afternoon = new LightingProfile { daylightColor = new Color(1, .72f, .43f), daylightIntensity = 1.15f, lightRotation = new Vector3(18, 205, 0), shadowStrength = .45f, ambient = new Color(.23f, .20f, .18f), windowColor = new Color(.72f, .48f, .27f), windowEmission = new Color(.44f, .27f, .12f) };
    public LightingProfile evening = new LightingProfile { daylightColor = new Color(.53f, .45f, .78f), daylightIntensity = .30f, lightRotation = new Vector3(9, 195, 0), ambient = new Color(.09f, .075f, .14f), windowColor = new Color(.18f, .15f, .32f), windowEmission = new Color(.07f, .045f, .16f) };
    public LightingProfile lateEvening = new LightingProfile { daylightColor = new Color(.43f, .55f, .83f), daylightIntensity = .11f, lightRotation = new Vector3(25, 180, 0), ambient = new Color(.045f, .055f, .09f), windowColor = new Color(.035f, .045f, .09f), windowEmission = new Color(.012f, .018f, .045f) };
    public LightingProfile night = new LightingProfile { daylightColor = new Color(.40f, .52f, .82f), daylightIntensity = .08f, lightRotation = new Vector3(25, 180, 0), ambient = new Color(.04f, .05f, .085f), windowColor = new Color(.025f, .035f, .075f), windowEmission = new Color(.01f, .016f, .04f) };
    public StoryMilestone[] milestones =
    {
        new StoryMilestone { id = "message-class", kind = MilestoneKind.LaptopReply, messageOrObjectiveId = "class" },
        new StoryMilestone { id = "message-friend", kind = MilestoneKind.LaptopReply, messageOrObjectiveId = "friend" },
        new StoryMilestone { id = "truth-1", kind = MilestoneKind.TruthReplyCount, replyCount = 1 },
        new StoryMilestone { id = "truth-2", kind = MilestoneKind.TruthReplyCount, replyCount = 2 },
        new StoryMilestone { id = "truth-3", kind = MilestoneKind.TruthReplyCount, replyCount = 3 }
    };
    [SerializeField] StoryTimeOfDay currentStage;
    [SerializeField, Range(0, 1)] float narrativeProgress;
    [SerializeField] int observedDay;
    public StoryTimeOfDay CurrentStage => currentStage;
    public float NarrativeProgress => narrativeProgress;
    public int MilestoneCount => earned.Count;
    public bool IsNight { get; private set; }
    public bool IsBlending => blendTime < transitionSeconds;
    readonly HashSet<string> earned = new HashSet<string>();
    bool initialized, subscribed;
    float blendTime;
    LightingProfile start, destination;
    MaterialPropertyBlock windowBlock;
    Color appliedWindowColor, appliedWindowEmission;
    public event Action<StoryTimeOfDay> StageChanged;
    void OnEnable()
    {
        if (!subscribed && tasks != null) { tasks.Changed += ReevaluateMilestones; subscribed = true; }
        if (initialized) ReevaluateMilestones();
    }
    void OnDisable() { if (subscribed && tasks != null) tasks.Changed -= ReevaluateMilestones; subscribed = false; }
    void Start()
    {
        windowBlock = new MaterialPropertyBlock();
        if (windowPane != null)
        {
            windowPane.GetPropertyBlock(windowBlock);
            appliedWindowColor = windowPane.sharedMaterial.GetColor("_BaseColor");
            appliedWindowEmission = windowPane.sharedMaterial.GetColor("_EmissionColor");
        }
        initialized = true; ResetDay(true); ReevaluateMilestones();
    }
    bool Satisfied(StoryMilestone milestone)
    {
        switch (milestone.kind)
        {
            case MilestoneKind.LaptopReply: return tasks.Current.repliedMessages.Contains(milestone.messageOrObjectiveId);
            case MilestoneKind.TruthReplyCount: return tasks.Current.conversationStep >= milestone.replyCount;
            default: return tasks.IsComplete(milestone.messageOrObjectiveId);
        }
    }
    void ResetDay(bool immediate)
    {
        observedDay = tasks.CurrentDay; earned.Clear(); narrativeProgress = 0; IsNight = false;
        currentStage = StoryTimeOfDay.Morning;
        ceilingSwitch.SetLight(ceilingSwitch.initiallyOn);
        BlendTo(morning, immediate); StageChanged?.Invoke(currentStage);
    }
    public void ReevaluateMilestones()
    {
        if (!initialized) return;
        if (observedDay != tasks.CurrentDay) ResetDay(false);
        if (IsNight) return;
        var applicable = milestones.Where(m => m != null && !string.IsNullOrEmpty(m.id) && (m.day == 0 || m.day == tasks.CurrentDay)).GroupBy(m => m.id).Select(g => g.First()).ToArray();
        foreach (var milestone in applicable) if (Satisfied(milestone)) earned.Add(milestone.id);
        float total = applicable.Sum(m => Mathf.Max(0, m.weight));
        narrativeProgress = total <= 0 ? 0 : applicable.Where(m => earned.Contains(m.id)).Sum(m => Mathf.Max(0, m.weight)) / total;
        var next = tasks.BedUnlocked ? StoryTimeOfDay.LateEvening : narrativeProgress + .0001f >= eveningThreshold ? StoryTimeOfDay.Evening : narrativeProgress + .0001f >= afternoonThreshold ? StoryTimeOfDay.Afternoon : StoryTimeOfDay.Morning;
        if (next <= currentStage) return;
        currentStage = next;
        // Automatic lamp changes happen only on these stage entries, never on repeated notifications.
        if (next == StoryTimeOfDay.Evening || next == StoryTimeOfDay.LateEvening) ceilingSwitch.SetLight(true);
        BlendTo(next == StoryTimeOfDay.Afternoon ? afternoon : next == StoryTimeOfDay.Evening ? evening : lateEvening, false);
        StageChanged?.Invoke(next);
    }
    public void EnterNight()
    {
        IsNight = true; BlendTo(night, false);
    }
    LightingProfile Capture()
    {
        return new LightingProfile { daylightColor = windowDaylight.color, daylightIntensity = windowDaylight.intensity, lightRotation = windowDaylight.transform.eulerAngles, shadowStrength = windowDaylight.shadowStrength, ambient = RenderSettings.ambientLight, windowColor = appliedWindowColor, windowEmission = appliedWindowEmission };
    }
    void BlendTo(LightingProfile profile, bool immediate)
    {
        start = Capture(); destination = profile; blendTime = immediate ? transitionSeconds : 0;
        if (immediate) Apply(1);
    }
    void Update()
    {
        if (!initialized || destination == null || blendTime >= transitionSeconds) return;
        blendTime = Mathf.Min(transitionSeconds, blendTime + Time.deltaTime);
        Apply(Mathf.SmoothStep(0, 1, Mathf.Clamp01(blendTime / Mathf.Max(.01f, transitionSeconds))));
    }
    void Apply(float t)
    {
        windowDaylight.enabled = true;
        windowDaylight.color = Color.Lerp(start.daylightColor, destination.daylightColor, t);
        windowDaylight.intensity = Mathf.Lerp(start.daylightIntensity, destination.daylightIntensity, t);
        windowDaylight.transform.rotation = Quaternion.Slerp(Quaternion.Euler(start.lightRotation), Quaternion.Euler(destination.lightRotation), t);
        windowDaylight.shadowStrength = Mathf.Lerp(start.shadowStrength, destination.shadowStrength, t);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(start.ambient, destination.ambient, t);
        appliedWindowColor = Color.Lerp(start.windowColor, destination.windowColor, t);
        appliedWindowEmission = Color.Lerp(start.windowEmission, destination.windowEmission, t);
        if (windowPane != null)
        {
            // MaterialPropertyBlock is not restored by an Editor script-domain reload.
            if (windowBlock == null) { windowBlock = new MaterialPropertyBlock(); windowPane.GetPropertyBlock(windowBlock); }
            windowBlock.SetColor("_BaseColor", appliedWindowColor); windowBlock.SetColor("_EmissionColor", appliedWindowEmission); windowPane.SetPropertyBlock(windowBlock);
        }
    }
}
