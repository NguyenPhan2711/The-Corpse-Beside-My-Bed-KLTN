using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Text-only menu item: dim when idle, bright when focused or chosen, underline while focused.
// Hovering focuses the item, so mouse and keyboard share one highlight.
[RequireComponent(typeof(Selectable))]
public sealed class MenuSelectable : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    public TMP_Text label;
    public Graphic underline;
    public Color idleColor = new Color(.52f, .5f, .47f), focusColor = new Color(.9f, .88f, .85f), disabledColor = new Color(.3f, .29f, .28f);
    Selectable selectable; bool focused, chosen;
    void Awake() { selectable = GetComponent<Selectable>(); }
    void OnEnable() { focused = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject; Apply(); }
    void Update() { if (!selectable.interactable && label.color != disabledColor) Apply(); }
    public void OnSelect(BaseEventData eventData) { focused = true; Apply(); }
    public void OnDeselect(BaseEventData eventData) { focused = false; Apply(); }
    public void OnPointerEnter(PointerEventData eventData) { if (selectable.interactable && EventSystem.current) EventSystem.current.SetSelectedGameObject(gameObject); }
    public void SetActiveChoice(bool value) { chosen = value; Apply(); }
    void Apply()
    {
        if (!selectable) selectable = GetComponent<Selectable>();
        label.color = !selectable.interactable ? disabledColor : focused || chosen ? focusColor : idleColor;
        if (underline) underline.enabled = focused && selectable.interactable;
    }
}
