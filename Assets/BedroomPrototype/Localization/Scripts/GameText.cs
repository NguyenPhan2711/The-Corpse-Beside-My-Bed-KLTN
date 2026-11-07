using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

// Synchronous lookups for text built in code. Tables are preloaded and results are cached per locale,
// so per-frame UI can call this without hitting the table every time.
public static class GameText
{
    public const string Menu="Menu", Settings="Settings", GameUI="GameUI", Cues="ThreatCues", Dialogue="Dialogue", Morning="Morning";
    static readonly Dictionary<(string,string),string> cache=new Dictionary<(string,string),string>();
    static Locale cachedLocale;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics(){cache.Clear();cachedLocale=null;}
    // SelectedLocaleChanged arrives a frame late, so the cache is tied to the locale itself.
    static void Hook(){var locale=LocalizationSettings.SelectedLocale;if(locale==cachedLocale)return;cache.Clear();cachedLocale=locale;}
    // Falls back to the given text (or the key) when the entry is missing, so a gap never shows an error string.
    public static string Get(string table,string key,string fallback=null)
    {
        Hook();
        if(cache.TryGetValue((table,key),out var value))return value;
        StringTable stringTable=LocalizationSettings.StringDatabase.GetTable(table);
        var entry=stringTable?stringTable.GetEntry(key):null;
        value=entry!=null&&!string.IsNullOrEmpty(entry.Value)?entry.Value:fallback??key;
        cache[(table,key)]=value;return value;
    }
    // Inspector-assigned text (dialogue, morning content). Empty references use the asset's original string.
    public static string Resolve(LocalizedString text,string fallback)
    {
        if(text==null||text.IsEmpty)return fallback;
        Hook();var id=(text.TableReference.ToString(),text.TableEntryReference.ToString());
        if(cache.TryGetValue(id,out var value))return value;
        StringTable stringTable=LocalizationSettings.StringDatabase.GetTable(text.TableReference);
        var entry=stringTable?stringTable.GetEntryFromReference(text.TableEntryReference):null;
        value=entry!=null&&!string.IsNullOrEmpty(entry.Value)?entry.Value:fallback;
        cache[id]=value;return value;
    }
    public static string Format(string table,string key,params object[] args)=>string.Format(Get(table,key),args);
    public static string UI(string key)=>Get(GameUI,key);
    public static string Cue(string key)=>Get(Cues,key);
}
