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
    void Awake() { buttonTemplate.gameObject.SetActive(false); modalPanel.SetActive(false); closeButton.onClick.AddListener(CloseModal); SetFade(0); InitializeDialogue(); }
    void Update()
    {
        UpdateDialogue();
        bool desk = phases.State == BedroomPhase.MorningDesk && !phases.IsTransitioning;
        prompt.text = !ModalOpen && !phases.IsTransitioning ? interactor.CurrentPrompt : "";
        checklistPanel.SetActive(desk);
        if (desk) checklist.text = "MORNING " + tasks.CurrentDay + "\n" + string.Join("\n", tasks.Definition.objectives.Select(o => (tasks.IsComplete(o.id) ? "[x] " : "[ ] ") + o.label)) + "\n\nEscape: stand up (close dialogue first)";
        status.text = (phases.State == BedroomPhase.NightIntroduction || phases.State == BedroomPhase.NightGameplay || phases.State == BedroomPhase.NightWon || phases.State == BedroomPhase.NightLost) ? "NIGHT " + tasks.CurrentDay + "   " + phases.night.Clock : "DAY " + tasks.CurrentDay + " / 3";
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
        if (message == null) { Show("PROTOTYPE LAPTOP", "All required messages have been read and replied to."); return; }
        Show("PROTOTYPE LAPTOP - " + message.sender, message.body);
        for (int i = 0; i < message.replies.Length; i++) { int reply = i; AddButton(message.replies[i], () => { if (tasks.ReplyToMessage(message.id, reply)) OpenLaptop(); }); }
    }
    public void OpenTruth()
    {
        if(phases.State!=BedroomPhase.MorningDesk||phases.IsTransitioning||DialogueActive||modalPanel.activeSelf)return;
        int step=tasks.Current.conversationStep;var sequence=ScriptableObject.CreateInstance<DialogueSequence>();sequence.hideFlags=HideFlags.DontSave;sequence.sequenceId="morning-truth-"+tasks.CurrentDay;sequence.blockMovement=true;
        sequence.lines=step>=tasks.Definition.conversation.Length?new[]{new DialogueLine{speaker="TheTruth",text="The required conversation is finished. Your choices have been recorded."}}:tasks.Definition.conversation.Skip(step).Select(c=>new DialogueLine{speaker="TheTruth",text=c.line,choices=c.replies.Select(r=>new DialogueChoice{text=r}).ToArray()}).ToArray();
        if(!TryShowDialogue(sequence,null,null,(line,reply)=>tasks.ReplyToTruth(reply)))Destroy(sequence);
    }
    public void ShowNightResult(bool won)
    {
        CancelDialogue();
        bool last = tasks.CurrentDay >= Mathf.Min(3, tasks.content.days.Length);
        Show(won ? "6 AM - NIGHT SURVIVED" : "NIGHT LOST", won ? (last ? "Three-night prototype complete.\nYour dialogue choices remain in this session's progression data." : "Morning " + (tasks.CurrentDay + 1) + " is ready.") : "Something reached you before dawn.", false);
        if (!won) AddButton("Retry night", () => { DismissResult(); phases.RetryNight(); });
        else if (!last) AddButton("Begin next morning", phases.NextMorning);
    }
}





