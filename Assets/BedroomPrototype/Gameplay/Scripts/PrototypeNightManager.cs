using System;
using UnityEngine;

public sealed class PrototypeNightManager : MonoBehaviour
{
    [Min(1)] public float nightDurationSeconds = 420f;
    public float Elapsed { get; private set; }
    public bool Running { get; private set; }
    public event Action Won;
    public event Action Began;
    // 0 = 12 AM ... 6 = 6 AM. Display text is localized by the UI; Clock stays as the English value for checks.
    public int Hour => Elapsed >= nightDurationSeconds ? 6 : Mathf.FloorToInt(Mathf.Clamp01(Elapsed / nightDurationSeconds) * 6);
    public string Clock => Hour == 0 ? "12 AM" : Hour + " AM";
    public void PrepareNight() { Elapsed=0; Running=false; }
    public void BeginNight() { Elapsed = 0; Running = true; Began?.Invoke(); }
    public void StopNight() { Running = false; }
    void Update()
    {
        if (!Running) return;
        Elapsed += Time.deltaTime;
        if (Elapsed >= nightDurationSeconds) { Elapsed = nightDurationSeconds; Running = false; Won?.Invoke(); }
    }
}


