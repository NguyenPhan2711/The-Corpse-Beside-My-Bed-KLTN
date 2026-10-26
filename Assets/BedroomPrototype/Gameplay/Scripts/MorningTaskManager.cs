using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class MorningTaskManager : MonoBehaviour
{
    [Serializable] public class Choice { public int day; public string source, entry, reply; }
    [Serializable] public class DayProgress
    {
        public int day;
        public List<string> completedObjectives = new List<string>();
        public List<string> repliedMessages = new List<string>();
        public int conversationStep;
    }
    public MorningContent content;
    [SerializeField] int currentDay = 1;
    [SerializeField] List<DayProgress> progress = new List<DayProgress>();
    [SerializeField] List<Choice> choices = new List<Choice>();
    [Serializable] public sealed class StoryFlag { public string id; public bool value; }
    [SerializeField] List<StoryFlag> storyFlags = new List<StoryFlag>();
    public bool GetStoryFlag(string id) => storyFlags.Any(f => f.id == id && f.value);
    public void SetStoryFlag(string id, bool value) { var flag=storyFlags.Find(f=>f.id==id); if(flag==null){flag=new StoryFlag{id=id};storyFlags.Add(flag);} flag.value=value; Changed?.Invoke(); }
    public void SetStoryFlagTrue(string id) => SetStoryFlag(id,true);
    public void SetStoryFlagFalse(string id) => SetStoryFlag(id,false);
    public void DebugSetDay(int day) { if(!(Application.isEditor||Debug.isDebugBuild))return;currentDay=Mathf.Clamp(day,1,Mathf.Min(3,content.days.Length));var unused=Current;Changed?.Invoke(); }
    public event Action Changed;
    public int CurrentDay => currentDay;
    public IReadOnlyList<Choice> Choices => choices;
    public MorningContent.Day Definition => content.days[Mathf.Clamp(currentDay - 1, 0, content.days.Length - 1)];
    public DayProgress Current
    {
        get { var p = progress.Find(x => x.day == currentDay); if (p == null) { p = new DayProgress { day = currentDay }; progress.Add(p); } return p; }
    }
    public bool BedUnlocked => Definition.objectives.Where(o => o.required).All(o => IsComplete(o.id));
    public bool IsComplete(string id) => Current.completedObjectives.Contains(id);
    public void CompleteObjective(string id)
    {
        if (!Definition.objectives.Any(o => o.id == id) || IsComplete(id)) return;
        Current.completedObjectives.Add(id); Changed?.Invoke();
    }
    public bool ReplyToMessage(string id, int reply)
    {
        var message = Definition.messages.FirstOrDefault(m => m.id == id);
        if (message == null || Current.repliedMessages.Contains(id) || reply < 0 || reply >= message.replies.Length) return false;
        choices.Add(new Choice { day = currentDay, source = "laptop", entry = id, reply = message.replies[reply] });
        Current.repliedMessages.Add(id);
        if (Definition.messages.All(m => Current.repliedMessages.Contains(m.id))) CompleteObjective(Definition.laptopObjectiveId);
        Changed?.Invoke(); return true;
    }
    public bool ReplyToTruth(int reply)
    {
        if (Current.conversationStep >= Definition.conversation.Length) return false;
        var line = Definition.conversation[Current.conversationStep];
        if (reply < 0 || reply >= line.replies.Length) return false;
        choices.Add(new Choice { day = currentDay, source = "truth", entry = Current.conversationStep.ToString(), reply = line.replies[reply] });
        Current.conversationStep++;
        if (Current.conversationStep == Definition.conversation.Length) CompleteObjective(Definition.truthObjectiveId);
        Changed?.Invoke(); return true;
    }
    public void BeginNextDay()
    {
        if (currentDay >= Mathf.Min(3, content.days.Length)) return;
        currentDay++; var unused = Current; Changed?.Invoke();
    }
}


