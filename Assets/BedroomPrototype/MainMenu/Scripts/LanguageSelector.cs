using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// One button per locale. Selecting one switches the language immediately and saves it.
public sealed class LanguageSelector : MonoBehaviour
{
    [Serializable] public class Option { public string code; public Button button; }
    public Option[] options;
    void Awake() { foreach (var option in options) { string code = option.code; option.button.onClick.AddListener(() => GameSettings.SetLanguage(code)); } }
    void OnEnable() { LocalizationSettings.SelectedLocaleChanged += Refresh; Refresh(null); }
    void OnDisable() { LocalizationSettings.SelectedLocaleChanged -= Refresh; }
    void Refresh(Locale _)
    {
        string current = GameSettings.CurrentLanguage;
        foreach (var option in options) { var style = option.button.GetComponent<MenuSelectable>(); if (style) style.SetActiveChoice(option.code == current); }
    }
}
