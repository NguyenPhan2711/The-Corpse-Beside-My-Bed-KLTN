using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Bedroom/Morning content")]
public sealed class MorningContent : ScriptableObject
{
    [Serializable] public class Objective { public string id; public string label; public bool required = true; }
    [Serializable] public class Message { public string id; public string sender; [TextArea] public string body; public string[] replies; }
    [Serializable] public class Conversation { [TextArea] public string line; public string[] replies; }
    [Serializable] public class Day
    {
        public string title;
        public Objective[] objectives;
        public Message[] messages;
        public Conversation[] conversation;
        public string laptopObjectiveId = "laptop";
        public string truthObjectiveId = "truth";
    }
    public Day[] days;
}
