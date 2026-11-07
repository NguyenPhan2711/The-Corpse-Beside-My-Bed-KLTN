using System;
using UnityEngine;
using UnityEngine.Localization;

// Player-facing text lives in the Morning string table (edit it per language in the localized fields).
// The hidden original strings stay as fallbacks, and replies keep them as language-independent choice records.
[CreateAssetMenu(menuName = "Bedroom/Morning content")]
public sealed class MorningContent : ScriptableObject
{
    [Serializable] public class Objective
    {
        public string id; [HideInInspector] public string label; public LocalizedString localizedLabel = new LocalizedString(); public bool required = true;
        public string Label => GameText.Resolve(localizedLabel, label);
    }
    [Serializable] public class Message
    {
        public string id; [HideInInspector] public string sender; public LocalizedString localizedSender = new LocalizedString();
        [HideInInspector, TextArea] public string body; public LocalizedString localizedBody = new LocalizedString();
        [HideInInspector] public string[] replies; public LocalizedString[] localizedReplies = Array.Empty<LocalizedString>();
        public string Sender => GameText.Resolve(localizedSender, sender);
        public string Body => GameText.Resolve(localizedBody, body);
        public int ReplyCount => Count(replies, localizedReplies);
        public string Reply(int index) => ReplyText(replies, localizedReplies, index);
        public string ReplyId(int index) => ReplyRecord(replies, localizedReplies, index);
    }
    [Serializable] public class Conversation
    {
        [HideInInspector, TextArea] public string line; public LocalizedString localizedLine = new LocalizedString();
        [HideInInspector] public string[] replies; public LocalizedString[] localizedReplies = Array.Empty<LocalizedString>();
        public string Line => GameText.Resolve(localizedLine, line);
        public int ReplyCount => Count(replies, localizedReplies);
        public string Reply(int index) => ReplyText(replies, localizedReplies, index);
        public string ReplyId(int index) => ReplyRecord(replies, localizedReplies, index);
    }
    static int Count(string[] ids, LocalizedString[] texts) => Mathf.Max(ids?.Length ?? 0, texts?.Length ?? 0);
    static string ReplyText(string[] ids, LocalizedString[] texts, int i) => GameText.Resolve(texts != null && i < texts.Length ? texts[i] : null, ids != null && i < ids.Length ? ids[i] : "");
    // Recorded choices must not depend on the language: use the original English reply, or the table key for replies added later.
    static string ReplyRecord(string[] ids, LocalizedString[] texts, int i) => ids != null && i < ids.Length ? ids[i] : texts[i].TableEntryReference.ToString();
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
