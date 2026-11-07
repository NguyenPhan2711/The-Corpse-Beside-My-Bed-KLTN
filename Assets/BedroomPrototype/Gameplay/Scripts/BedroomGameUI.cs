using System.Linq;
using TMPro;
using UnityEngine;

public sealed partial class BedroomGameUI : MonoBehaviour
{
    public BedroomPhaseController phases;
    public MorningTaskManager tasks;
    public PlayerInteractor interactor;
    public TMP_Text prompt, checklist, status, modalTitle, modalBody, crosshair;
    public GameObject checklistPanel, modalPanel;
    public Transform buttonRoot;
    public UnityEngine.UI.Button buttonTemplate, closeButton;
    public UnityEngine.UI.Image fade;
    public bool ModalOpen => modalPanel.activeSelf || DialogueActive;
    static readonly string[] ClockKeys = { "clock.0", "clock.1", "clock.2", "clock.3", "clock.4", "clock.5", "clock.6" };
    void Awake() { buttonTemplate.gameObject.SetActive(false); modalPanel.SetActive(false); closeButton.onClick.AddListener(CloseModal); SetFade(0); InitializeDialogue(); }
    void Update()
    {
        UpdateDialogue();
        bool desk = phases.State == BedroomPhase.MorningDesk && !phases.IsTransitioning;
        prompt.text = !ModalOpen && !phases.IsTransitioning ? interactor.CurrentPrompt : "";
        checklistPanel.SetActive(desk);
        if (desk) checklist.text = GameText.Format(GameText.GameUI, "checklist.header", tasks.CurrentDay) + "\n" + string.Join("\n", tasks.Definition.objectives.Select(o => (tasks.IsComplete(o.id) ? "[x] " : "[ ] ") + o.Label)) + "\n\n" + GameText.UI("checklist.footer");
        status.text = (phases.State == BedroomPhase.NightIntroduction || phases.State == BedroomPhase.NightGameplay || phases.State == BedroomPhase.NightWon || phases.State == BedroomPhase.NightLost) ? GameText.Format(GameText.GameUI, "status.night", tasks.CurrentDay, GameText.UI(ClockKeys[phases.night.Hour])) : GameText.Format(GameText.GameUI, "status.day", tasks.CurrentDay);
        crosshair.gameObject.SetActive(!ModalOpen && !phases.IsTransitioning && (phases.State == BedroomPhase.MorningExploration || desk || phases.State == BedroomPhase.NightGameplay));
    }
    public void SetFade(float alpha) { var c = fade.color; c.a = Mathf.Clamp01(alpha); fade.color = c; fade.raycastTarget = alpha > .01f; }
    void ClearButtons() { foreach (Transform t in buttonRoot) if (t.gameObject != buttonTemplate.gameObject) { t.gameObject.SetActive(false); Destroy(t.gameObject); } }
    void Show(string title, string body, bool canClose = true)
    {
        modalPanel.SetActive(true); modalTitle.text = title; modalBody.text = body; closeButton.gameObject.SetActive(canClose); ClearButtons(); phases.ReleaseCursor();
    }
    void AddButton(string label, UnityEngine.Events.UnityAction action)
    {
        var b = Instantiate(buttonTemplate, buttonRoot); b.name = label; b.GetComponentInChildren<TMP_Text>(true).text = label; b.onClick.RemoveAllListeners(); b.onClick.AddListener(action); b.gameObject.SetActive(true);
    }
    public void CloseModal()
    {
        if (DialogueActive) { CancelDialogue(); return; }
        if (phases.State == BedroomPhase.NightWon || phases.State == BedroomPhase.NightLost) return;
        modalPanel.SetActive(false); ClearButtons(); if (!phases.IsTransitioning) phases.CaptureCursor();
    }
    public void DismissResult() { modalPanel.SetActive(false); ClearButtons(); }
    public void OpenLaptop()
    {
        if (phases.State != BedroomPhase.MorningDesk || phases.IsTransitioning) return;
        var message = tasks.Definition.messages.FirstOrDefault(m => !tasks.Current.repliedMessages.Contains(m.id));
        if (message == null) { Show(GameText.UI("laptop.title"), GameText.UI("laptop.allDone")); return; }
        Show(GameText.Format(GameText.GameUI, "laptop.titleSender", message.Sender), message.Body);
        for (int i = 0; i < message.ReplyCount; i++) { int reply = i; AddButton(message.Reply(i), () => { if (tasks.ReplyToMessage(message.id, reply)) OpenLaptop(); }); }
    }
    public void OpenTruth()
    {
        if(phases.State!=BedroomPhase.MorningDesk||phases.IsTransitioning||DialogueActive||modalPanel.activeSelf)return;
        int step=tasks.Current.conversationStep;var sequence=ScriptableObject.CreateInstance<DialogueSequence>();sequence.hideFlags=HideFlags.DontSave;sequence.sequenceId="morning-truth-"+tasks.CurrentDay;sequence.blockMovement=true;
        sequence.lines=step>=tasks.Definition.conversation.Length?new[]{new DialogueLine{speaker="TheTruth",text=GameText.UI("truth.finished")}}:tasks.Definition.conversation.Skip(step).Select(c=>new DialogueLine{speaker="TheTruth",text=c.Line,choices=Enumerable.Range(0,c.ReplyCount).Select(r=>new DialogueChoice{text=c.Reply(r)}).ToArray()}).ToArray();
        if(!TryShowDialogue(sequence,null,null,(line,reply)=>tasks.ReplyToTruth(reply)))Destroy(sequence);
    }
    public void ShowNightResult(bool won)
    {
        CancelDialogue();
        bool last = tasks.CurrentDay >= Mathf.Min(3, tasks.content.days.Length);
        Show(won ? GameText.UI("result.wonTitle") : GameText.UI("result.lostTitle"), won ? (last ? GameText.UI("result.wonLast") : GameText.Format(GameText.GameUI, "result.wonNext", tasks.CurrentDay + 1)) : GameText.UI("result.lostBody"), false);
        if (!won) AddButton(GameText.UI("result.retry"), () => { DismissResult(); phases.RetryNight(); });
        else if (!last) AddButton(GameText.UI("result.nextMorning"), phases.NextMorning);
    }
}





