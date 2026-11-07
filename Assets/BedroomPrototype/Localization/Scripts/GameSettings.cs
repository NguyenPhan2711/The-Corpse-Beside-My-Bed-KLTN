using UnityEngine;
using UnityEngine.Localization.Settings;

// Single owner of saved player settings. Add future settings (volume, mouse sensitivity) here as new keys.
public static class GameSettings
{
    // Shared with the PlayerPrefLocaleSelector in LocalizationSettings, which restores it on startup in any scene.
    public const string LanguageKey="settings.language";
    public const string DefaultLanguage="en";
    public static string Language=>PlayerPrefs.GetString(LanguageKey,DefaultLanguage);
    public static string CurrentLanguage=>LocalizationSettings.SelectedLocale!=null?LocalizationSettings.SelectedLocale.Identifier.Code:Language;
    public static bool SetLanguage(string code)
    {
        var locale=LocalizationSettings.AvailableLocales.GetLocale(code);
        if(locale==null)return false;
        LocalizationSettings.SelectedLocale=locale;PlayerPrefs.SetString(LanguageKey,code);PlayerPrefs.Save();return true;
    }
}
