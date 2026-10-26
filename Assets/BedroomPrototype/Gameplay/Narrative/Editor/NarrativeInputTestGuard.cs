#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
// Explicitly armed only by temporary automated-test launchers. Normal gameplay is untouched.
[InitializeOnLoad]
public static class NarrativeInputTestGuard
{
 static bool armed;static InputSettings original;static InputDevice[] devices;
 static NarrativeInputTestGuard(){AssemblyReloadEvents.beforeAssemblyReload+=Restore;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingPlayMode)Restore();};}
 public static void Arm(){Restore();original=InputSystem.settings;devices=InputSystem.devices.ToArray();armed=true;}
 public static void Restore(){if(!armed)return;armed=false;if(original)InputSystem.settings=original;if(devices!=null)foreach(var device in InputSystem.devices.ToArray())if(!device.native&&!devices.Contains(device))InputSystem.RemoveDevice(device);original=null;devices=null;}
}
#endif
