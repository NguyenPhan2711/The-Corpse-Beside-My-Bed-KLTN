using System;
using UnityEngine;
[Serializable] public sealed class DialogueCondition
{
 public int day,night; public string requiredFlag; public bool flagValue=true; public string choiceSource,choiceEntry,choiceReply;
 public bool Matches(MorningTaskManager tasks,BedroomPhaseController phases){if(day>0&&tasks.CurrentDay!=day)return false;if(night>0&&(!phases.IsNightPhase||tasks.CurrentDay!=night))return false;if(!string.IsNullOrEmpty(requiredFlag)&&tasks.GetStoryFlag(requiredFlag)!=flagValue)return false;if(!string.IsNullOrEmpty(choiceSource)){bool found=false;foreach(var c in tasks.Choices)if(c.source==choiceSource&&(string.IsNullOrEmpty(choiceEntry)||c.entry==choiceEntry)&&(string.IsNullOrEmpty(choiceReply)||c.reply==choiceReply)){found=true;break;}if(!found)return false;}return true;}
}
[Serializable] public sealed class DialogueChoice {public string text,flagId;public bool flagValue=true;}
[Serializable] public sealed class DialogueLine
{
 public string speaker="TheTruth";[TextArea(2,6)]public string text;[Tooltip("-1 uses the UI default; 0 reveals immediately.")]public float charactersPerSecond=-1;
 public string eventId;public bool eventAfterLine=true,pauseForAnimation,hideDuringEvent;public DialogueCondition condition=new DialogueCondition();public DialogueChoice[] choices=Array.Empty<DialogueChoice>();
}
[CreateAssetMenu(menuName="Bedroom/Dialogue sequence")]
public sealed class DialogueSequence:ScriptableObject
{
 public string sequenceId;public bool blockMovement=true,allowLook;public DialogueLine[] lines=Array.Empty<DialogueLine>();
}
