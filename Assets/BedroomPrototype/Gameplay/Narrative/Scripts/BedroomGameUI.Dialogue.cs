using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
[Serializable]public sealed class DialogueEventBinding { public string id; public UnityEngine.Events.UnityEvent action=new UnityEngine.Events.UnityEvent(); }
[Serializable]public sealed class DialogueSpeakerStyle {public string id,displayName;public Color nameColor=new Color(.78f,.75f,.68f),textColor=new Color(.92f,.91f,.86f);}
public sealed partial class BedroomGameUI
{
 public GameObject dialoguePanel;public TMP_Text dialogueSpeaker,dialogueText,dialogueContinue;public Transform dialogueChoiceRoot;
 public DialogueSpeakerStyle[] speakers={new DialogueSpeakerStyle{id="TheTruth",displayName="THE TRUTH"},new DialogueSpeakerStyle{id="Protagonist",displayName="PROTAGONIST"},new DialogueSpeakerStyle{id="Narration",displayName="NARRATION"}};
 [Min(0)]public float defaultCharactersPerSecond=35;[Min(.01f)]public float minimumAdvanceInterval=.1f;
 public DialogueEventBinding[] eventBindings=Array.Empty<DialogueEventBinding>();
 public InputAction dialogueAdvance=CreateAdvance();public UnityEngine.Events.UnityEvent<string> dialogueEvent=new UnityEngine.Events.UnityEvent<string>();
 public bool DialogueActive{get;private set;}public bool DialogueBusy{get;private set;}public bool DialogueTyping{get;private set;}public int DialogueLineIndex{get;private set;}=-1;
 public DialogueSequence CurrentDialogue{get;private set;}public bool DialogueAllowsLook=>DialogueActive&&CurrentDialogue&&CurrentDialogue.allowLook&&!DialogueBusy;
 public Transform ActiveButtonRoot=>DialogueActive?dialogueChoiceRoot:buttonRoot;
 public bool GameplayInputSuppressed=>DialogueActive||Time.frameCount<=releaseInputFrame;
 Func<string,IEnumerator> eventHandler;Action completed;Action<int,int> choiceHandler;bool restoreMovement;CursorLockMode previousCursor;bool previousCursorVisible;BedroomPhase previousPhase;int openedFrame,advancedFrame=-1,releaseInputFrame=-1,characterCount;float revealCount,revealSpeed,nextAdvance;Coroutine blockingEvent;
 static InputAction CreateAdvance(){var action=new InputAction("Dialogue advance",InputActionType.Button,"<Mouse>/leftButton");action.AddBinding("<Keyboard>/enter");return action;}
 void InitializeDialogue(){if(dialoguePanel)dialoguePanel.SetActive(false);}
 void OnEnable(){dialogueAdvance.Enable();}
 void OnDisable(){dialogueAdvance.Disable();CancelDialogue();}
 void OnDestroy(){dialogueAdvance.Dispose();}
 void UpdateDialogue()
 {
  if(!DialogueActive)return;
  if(DialogueTyping){revealCount+=Time.unscaledDeltaTime*revealSpeed;dialogueText.maxVisibleCharacters=Mathf.Min(characterCount,Mathf.FloorToInt(revealCount));if(dialogueText.maxVisibleCharacters>=characterCount){DialogueTyping=false;UpdateContinue();}}
  if(!DialogueBusy&&!phases.IsObservationLocked&&Time.frameCount>openedFrame&&dialogueAdvance.WasPressedThisFrame())AdvanceDialogue();
 }
 void UpdateContinue(){if(dialogueContinue)dialogueContinue.text=DialogueBusy?"":HasChoices?"Choose a reply":DialogueTyping?"Click / Enter - reveal text":"Click / Enter - continue";}
 bool HasChoices=>CurrentDialogue&&DialogueLineIndex>=0&&DialogueLineIndex<CurrentDialogue.lines.Length&&CurrentDialogue.lines[DialogueLineIndex].choices!=null&&CurrentDialogue.lines[DialogueLineIndex].choices.Length>0;
 public bool TryShowDialogue(DialogueSequence sequence,Action onComplete=null,Func<string,IEnumerator> handler=null,Action<int,int> onChoice=null)
 {
  if(!sequence||DialogueActive||modalPanel.activeSelf||!dialoguePanel)return false;
  CurrentDialogue=sequence;completed=onComplete;eventHandler=handler;choiceHandler=onChoice;DialogueActive=true;DialogueBusy=false;openedFrame=Time.frameCount;nextAdvance=Time.unscaledTime+minimumAdvanceInterval;
  previousPhase=phases.State;restoreMovement=sequence.blockMovement&&phases.movement.enabled;previousCursor=Cursor.lockState;previousCursorVisible=Cursor.visible;
  if(restoreMovement)phases.movement.enabled=false;
  if(sequence.allowLook)phases.CaptureCursor();else phases.ReleaseCursor();
  DialogueLineIndex=-1;NextDialogueLine();return true;
 }
 public bool AdvanceDialogue()
 {
  if(!DialogueActive||DialogueBusy||Time.frameCount<=openedFrame||advancedFrame==Time.frameCount||Time.unscaledTime<nextAdvance)return false;
  advancedFrame=Time.frameCount;nextAdvance=Time.unscaledTime+minimumAdvanceInterval;
  if(DialogueTyping){dialogueText.maxVisibleCharacters=int.MaxValue;DialogueTyping=false;UpdateContinue();return true;}
  if(HasChoices)return false;
  FinishDialogueLine();return true;
 }
 void NextDialogueLine()
 {
  ClearDialogueChoices();DialogueLineIndex++;
  while(DialogueLineIndex<CurrentDialogue.lines.Length&&CurrentDialogue.lines[DialogueLineIndex].condition!=null&&!CurrentDialogue.lines[DialogueLineIndex].condition.Matches(tasks,phases))DialogueLineIndex++;
  if(DialogueLineIndex>=CurrentDialogue.lines.Length){EndDialogue(true);return;}
  var line=CurrentDialogue.lines[DialogueLineIndex];
  if(!line.eventAfterLine&&!string.IsNullOrEmpty(line.eventId)){RunDialogueEvent(line,PresentDialogueLine);return;}
  PresentDialogueLine();
 }
 void PresentDialogueLine()
 {
  if(!DialogueActive)return;var line=CurrentDialogue.lines[DialogueLineIndex];dialoguePanel.SetActive(true);
  var style=speakers.FirstOrDefault(s=>s.id==line.speaker);dialogueSpeaker.text=style!=null?style.displayName:line.speaker;dialogueSpeaker.color=style!=null?style.nameColor:Color.white;dialogueText.color=style!=null?style.textColor:Color.white;
  dialogueText.text=line.text;dialogueText.ForceMeshUpdate();characterCount=dialogueText.textInfo.characterCount;revealCount=0;revealSpeed=line.charactersPerSecond<0?defaultCharactersPerSecond:line.charactersPerSecond;DialogueTyping=revealSpeed>0;dialogueText.maxVisibleCharacters=DialogueTyping?0:int.MaxValue;
  if(line.choices!=null)for(int i=0;i<line.choices.Length;i++){int index=i;var b=Instantiate(buttonTemplate,dialogueChoiceRoot);b.name="DialogueReply"+i;b.GetComponentInChildren<TMP_Text>(true).text=line.choices[i].text;b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>SelectDialogueChoice(index));var image=b.GetComponent<UnityEngine.UI.Image>();if(image)image.color=new Color(.16f,.16f,.17f,.98f);b.gameObject.SetActive(true);}
  var rect=dialoguePanel.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(rect.sizeDelta.x,HasChoices?240+line.choices.Length*42:210);UpdateContinue();
 }
 public bool SelectDialogueChoice(int index)
 {
  if(!DialogueActive||DialogueBusy||!HasChoices)return false;var line=CurrentDialogue.lines[DialogueLineIndex];if(index<0||index>=line.choices.Length)return false;
  // Choices may be selected without waiting for typewriter; selecting is an explicit reply, never an accidental advance.
  advancedFrame=Time.frameCount;nextAdvance=Time.unscaledTime+minimumAdvanceInterval;var choice=line.choices[index];if(!string.IsNullOrEmpty(choice.flagId))tasks.SetStoryFlag(choice.flagId,choice.flagValue);choiceHandler?.Invoke(DialogueLineIndex,index);FinishDialogueLine();return true;
 }
 void FinishDialogueLine(){var line=CurrentDialogue.lines[DialogueLineIndex];if(line.eventAfterLine&&!string.IsNullOrEmpty(line.eventId))RunDialogueEvent(line,NextDialogueLine);else NextDialogueLine();}
 void RunDialogueEvent(DialogueLine line,Action continuation)
 {
  foreach(var binding in eventBindings)if(binding.id==line.eventId)binding.action.Invoke();dialogueEvent.Invoke(line.eventId);var routine=eventHandler?.Invoke(line.eventId);
  if(routine==null){continuation();return;}
  if(!line.pauseForAnimation){StartCoroutine(routine);continuation();return;}
  DialogueBusy=true;DialogueTyping=false;if(line.hideDuringEvent)dialoguePanel.SetActive(false);UpdateContinue();blockingEvent=StartCoroutine(EventThen(routine,continuation));
 }
 IEnumerator EventThen(IEnumerator routine,Action continuation){yield return routine;blockingEvent=null;if(!DialogueActive)yield break;DialogueBusy=false;continuation();}
 void ClearDialogueChoices(){if(!dialogueChoiceRoot)return;foreach(Transform t in dialogueChoiceRoot){t.gameObject.SetActive(false);Destroy(t.gameObject);}}
 public void CancelDialogue(){if(DialogueActive)EndDialogue(false);}
 void EndDialogue(bool invokeCompletion)
 {
  if(!DialogueActive)return;StopAllCoroutines();blockingEvent=null;var callback=completed;completed=null;eventHandler=null;choiceHandler=null;DialogueActive=DialogueBusy=DialogueTyping=false;if(dialoguePanel)dialoguePanel.SetActive(false);ClearDialogueChoices();releaseInputFrame=Time.frameCount+1;
  if(restoreMovement&&phases.State==previousPhase&&!phases.IsTransitioning)phases.movement.enabled=true;restoreMovement=false;
  Cursor.lockState=previousCursor;Cursor.visible=previousCursorVisible;var previousDialogue=CurrentDialogue;CurrentDialogue=null;DialogueLineIndex=-1;if(previousDialogue&&(previousDialogue.hideFlags&HideFlags.DontSave)!=0)Destroy(previousDialogue);if(invokeCompletion)callback?.Invoke();
 }
 public bool OpenNarrative(DialogueSequence sequence)=>TryShowDialogue(sequence);
}



