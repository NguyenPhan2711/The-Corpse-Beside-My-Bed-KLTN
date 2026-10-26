using System;
using UnityEngine;

public sealed class PrototypeNightManager : MonoBehaviour
{
    [Min(1)] public float nightDurationSeconds = 180f;
    public float Elapsed { get; private set; }
    public bool Running { get; private set; }
    public event Action Won;
    public event Action Began;
    public string Clock => Elapsed >= nightDurationSeconds ? "6 AM" : (Mathf.FloorToInt(Mathf.Clamp01(Elapsed / nightDurationSeconds) * 6) == 0 ? "12 AM" : Mathf.FloorToInt(Mathf.Clamp01(Elapsed / nightDurationSeconds) * 6) + " AM");
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


