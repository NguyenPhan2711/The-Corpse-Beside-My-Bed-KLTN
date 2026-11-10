using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif
// Morning opening: wake in bed beside the glass corpse, look closer, get up, go to the desk.
// BedroomPhaseController owns the body, camera and fades; this owns timing, audio, prompts and the desk hook.
// E only reaches this through PlayerInteractor (it is the interactor's target while the phase is MorningOpening).
[DefaultExecutionOrder(100)]
public sealed class MorningOpeningController:MonoBehaviour,IInteractable
{
 [Serializable]public sealed class DaySettings
 {
  public bool enabled;
  [Tooltip("Black screen with sound, then eyelid blinks. Off: a short fade in.")]public bool blinkIntro=true;
  [Tooltip("Off: [E] Get up is offered from the start and looking closer is optional.")]public bool requireLookCloser=true;
  public DialogueSequence thoughts;
  [Tooltip("Show the Go to the desk objective and run the desk hook when the player sits.")]public bool deskObjective=true;
 }
 public enum Step{Inactive,Intro,Waking,PushingIn,Thoughts,EasingBack,ReadyToGetUp,GettingUp,GoToDesk,DeskHook,Finished}
 public BedroomPhaseController phases;
 [Tooltip("Point on the corpse the player must look toward for [E] Look closer.")]public Transform lookCloserFocus;
 [Tooltip("Standing position after getting up.")]public Transform standPoint;
 public DaySettings[] days={new DaySettings{enabled=true},new DaySettings{blinkIntro=false,requireLookCloser=false,deskObjective=false},new DaySettings{blinkIntro=false,requireLookCloser=false,deskObjective=false}};
 [Header("Black screen and blinks")]
 public RectTransform eyelidTop,eyelidBottom;
 [Min(0)]public float blackSeconds=3;
 [Tooltip("How far the eyes open on each blink (1 = fully open). The last one stays open.")]public float[] blinkOpenAmounts={.3f,.65f,1};
 [Min(.01f)]public float blinkOpenSeconds=.9f,blinkCloseSeconds=.3f;
 [Min(0)]public float blinkHoldOpenSeconds=.5f,blinkHoldClosedSeconds=.2f;
 [Tooltip("Used when the blink intro is off.")][Min(.01f)]public float shortFadeInSeconds=1;
 [Header("Turning to the side")]
 public TMP_Text hintText;
 [Min(0)]public float lookHintDelay=4;
 [Tooltip("Total mouse movement (pixels) that counts as having looked around.")][Min(0)]public float lookHintMouseThreshold=25;
 [Tooltip("Degrees between the view and the corpse for [E] Look closer.")][Range(1,90)]public float lookCloserAngle=25;
 [Header("Looking closer")]
 [Tooltip("Pause after [E] Look closer before the first thought. The camera stays under the player's control.")][Min(0)]public float pauseBeforeThoughts=.4f;
 [Header("Desk")]
 public TMP_Text objectiveText;
 [Tooltip("Placeholder for The Truth's first-morning introduction, invoked when the player sits at the desk.")]public UnityEvent OpeningTruthIntroduction=new UnityEvent();
 [Tooltip("Optional dialogue played after the event. Empty: the morning continues straight away.")]public DialogueSequence openingTruthDialogue;
 [Header("Audio (empty clips are skipped)")]
 public AudioSource fliesSource,breathingSource,ambienceSource;
 public AudioClip fliesClip,breathingClip,morningAmbienceClip;
 [Range(0,1)]public float fliesVolume=.25f,fliesCloseVolume=.45f,breathingVolume=.35f,ambienceVolume=.3f;
 [Min(.01f)]public float audioFadeSeconds=1.5f;
 [Header("Stained-glass light (pillows, sheets and wall beside the bed)")]
 public Light ceilingPatches;
 [Min(0)]public float patchIntensityOpening=2.6f,patchIntensityRestOfMorning=1.4f;
 [Min(.01f)]public float patchFadeSeconds=2;
 [Tooltip("Day 1 keeps the patches after the opening; they follow the daylight and go out at night.")]public bool keepPatchesOnDay1=true;
 [Header("Development (Editor and development builds only)")]
 [Tooltip("Start the game past the opening. F7 skips it while it plays.")]public bool skipOpeningOnStart;
 public Step CurrentStep{get;private set;}
 public bool IsRunning=>CurrentStep!=Step.Inactive&&CurrentStep!=Step.Finished;
 public bool HidesChecklist=>IsRunning;
 public bool HidesHud=>CurrentStep==Step.Intro;
 public bool LookEnabled=>CurrentStep>=Step.Waking&&CurrentStep<=Step.ReadyToGetUp;
 public bool FacingCorpse=>lookCloserFocus&&phases.view&&Vector3.Angle(phases.view.transform.forward,lookCloserFocus.position-phases.view.transform.position)<=lookCloserAngle;
 DaySettings settings;float wakingSince,mouseTravel;bool lookedCloser,fliesOn,breathingOn,ambienceOn,patchesOn;int playingDay;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 InputAction skipAction;
#endif
 void Awake()
 {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  skipAction=new InputAction("Skip opening",InputActionType.Button,"<Keyboard>/f7");
#endif
  SetEyelids(1);if(hintText)hintText.gameObject.SetActive(false);if(objectiveText)objectiveText.gameObject.SetActive(false);if(ceilingPatches){ceilingPatches.intensity=0;ceilingPatches.enabled=false;}
 }
 void OnEnable()
 {
  if(phases)phases.PhaseChanged+=OnPhaseChanged;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  skipAction?.Enable();
#endif
 }
 void OnDisable()
 {
  if(phases)phases.PhaseChanged-=OnPhaseChanged;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  skipAction?.Disable();
#endif
 }
 void OnDestroy()
 {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  skipAction?.Dispose();
#endif
 }
 void Start()
 {
  TryBegin(phases.tasks.CurrentDay);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  if(skipOpeningOnStart)DebugSkipOpening();
#endif
 }
 public DaySettings SettingsFor(int day)=>days!=null&&day>=1&&day<=days.Length?days[day-1]:null;
 public bool PlaysOn(int day){var s=SettingsFor(day);return s!=null&&s.enabled&&phases&&phases.openingCameraAnchor&&standPoint;}
 public bool TryBegin(int day)
 {
  if(!PlaysOn(day)||IsRunning)return false;
  settings=SettingsFor(day);playingDay=day;lookedCloser=false;mouseTravel=0;
  phases.BeginOpening();fliesOn=breathingOn=ambienceOn=patchesOn=true;
  Loop(fliesSource,fliesClip);Loop(breathingSource,breathingClip);Loop(ambienceSource,morningAmbienceClip);
  StartCoroutine(Intro());return true;
 }
 static void Loop(AudioSource source,AudioClip clip){if(!source||!clip)return;source.clip=clip;source.loop=true;source.volume=0;source.Play();}
 IEnumerator Intro()
 {
  CurrentStep=Step.Intro;
  if(settings.blinkIntro)
  {
   SetEyelids(0);phases.ui.SetFade(0);yield return Wait(blackSeconds);float open=0;
   for(int i=0;i<blinkOpenAmounts.Length;i++)
   {
    yield return Eyelids(open,blinkOpenAmounts[i],blinkOpenSeconds);open=blinkOpenAmounts[i];
    if(i==blinkOpenAmounts.Length-1)break;
    yield return Wait(blinkHoldOpenSeconds);yield return Eyelids(open,0,blinkCloseSeconds);open=0;yield return Wait(blinkHoldClosedSeconds);
   }
   SetEyelids(1);
  }
  else{SetEyelids(1);float t=0;while(t<shortFadeInSeconds){t+=Time.unscaledDeltaTime;phases.ui.SetFade(1-Mathf.Clamp01(t/shortFadeInSeconds));yield return null;}phases.ui.SetFade(0);}
  CurrentStep=Step.Waking;wakingSince=Time.unscaledTime;mouseTravel=0;
 }
 static IEnumerator Wait(float seconds){float t=0;while(t<seconds){t+=Time.unscaledDeltaTime;yield return null;}}
 IEnumerator Eyelids(float from,float to,float seconds){float t=0;while(t<seconds){t+=Time.unscaledDeltaTime;SetEyelids(Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,Mathf.Clamp01(t/seconds))));yield return null;}SetEyelids(to);}
 // 0 = closed (black), 1 = fully open (lids off screen).
 void SetEyelids(float open)
 {
  open=Mathf.Clamp01(open);bool visible=open<.999f;
  if(eyelidTop){eyelidTop.gameObject.SetActive(visible);eyelidTop.anchoredPosition=new Vector2(0,open*eyelidTop.rect.height);}
  if(eyelidBottom){eyelidBottom.gameObject.SetActive(visible);eyelidBottom.anchoredPosition=new Vector2(0,-open*eyelidBottom.rect.height);}
 }
 void Update()
 {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  if(IsRunning&&skipAction!=null&&skipAction.WasPressedThisFrame())DebugSkipOpening();
#endif
  UpdateAudio();UpdatePatches();
  if(CurrentStep==Step.Waking&&Cursor.lockState==CursorLockMode.Locked)mouseTravel+=phases.interactor.Look.magnitude;
  bool hint=CurrentStep==Step.Waking&&!lookedCloser&&mouseTravel<lookHintMouseThreshold&&Time.unscaledTime-wakingSince>=lookHintDelay&&!FacingCorpse;
  if(hintText){hintText.gameObject.SetActive(hint);if(hint)hintText.text=GameText.UI("opening.lookHint");}
  bool objective=CurrentStep==Step.GoToDesk&&phases.State==BedroomPhase.MorningExploration;
  if(objectiveText){objectiveText.gameObject.SetActive(objective);if(objective)objectiveText.text=GameText.UI("objective.goToDesk");}
 }
 void UpdateAudio()
 {
  float rate=Time.unscaledDeltaTime/audioFadeSeconds;
  bool close=CurrentStep>=Step.PushingIn&&CurrentStep<=Step.EasingBack;
  Fade(fliesSource,fliesOn?(close?fliesCloseVolume:fliesVolume):0,rate);Fade(breathingSource,breathingOn?breathingVolume:0,rate);Fade(ambienceSource,ambienceOn?ambienceVolume:0,rate);
 }
 static void Fade(AudioSource source,float target,float rate){if(!source||!source.isPlaying)return;source.volume=Mathf.MoveTowards(source.volume,target,rate);if(target<=0&&source.volume<=0)source.Stop();}
 void UpdatePatches()
 {
  if(!ceilingPatches)return;float target=0;
  if(patchesOn&&!phases.IsNightPhase)
  {
   if(IsRunning&&CurrentStep<Step.GoToDesk)target=patchIntensityOpening;
   else if(keepPatchesOnDay1&&playingDay==1&&phases.tasks.CurrentDay==1)target=patchIntensityRestOfMorning*DaylightFactor;
  }
  ceilingPatches.intensity=Mathf.MoveTowards(ceilingPatches.intensity,target,Mathf.Max(patchIntensityOpening,patchIntensityRestOfMorning)*Time.unscaledDeltaTime/patchFadeSeconds);
  ceilingPatches.enabled=ceilingPatches.intensity>.001f;
 }
 // The patches are sunlight through the glass: they dim with the time-of-day daylight.
 float DaylightFactor{get{var lighting=phases.timeOfDayLighting;return lighting&&lighting.windowDaylight&&lighting.morning.daylightIntensity>0?Mathf.Clamp01(lighting.windowDaylight.intensity/lighting.morning.daylightIntensity):1;}}
 void OnPhaseChanged(BedroomPhase state)
 {
  if(phases.IsNightPhase){fliesOn=breathingOn=ambienceOn=patchesOn=false;if(IsRunning)Cancel();}
  else if(state==BedroomPhase.MorningDesk&&CurrentStep==Step.GoToDesk)StartCoroutine(DeskHook());
 }
 bool OffersLookCloser=>CurrentStep==Step.Waking&&!lookedCloser&&FacingCorpse;
 bool OffersGetUp=>CurrentStep==Step.ReadyToGetUp||(CurrentStep==Step.Waking&&!settings.requireLookCloser);
 public string GetPrompt(PlayerInteractor actor)=>OffersLookCloser?GameText.UI("prompt.lookCloser"):OffersGetUp?GameText.UI("prompt.getUp"):"";
 public bool CanInteract(PlayerInteractor actor)=>phases.State==BedroomPhase.MorningOpening&&!phases.IsTransitioning&&!phases.ui.ModalOpen&&(OffersLookCloser||OffersGetUp);
 public void Interact(PlayerInteractor actor)
 {
  if(!CanInteract(actor))return;
  if(OffersLookCloser)StartCoroutine(LookCloser());else GetUp();
 }
 IEnumerator LookCloser()
 {
  CurrentStep=Step.PushingIn;lookedCloser=true;yield return Wait(pauseBeforeThoughts);
  CurrentStep=Step.Thoughts;bool done=false;
  if(settings.thoughts&&phases.ui.TryShowDialogue(settings.thoughts,()=>done=true))while(!done&&phases.ui.DialogueActive)yield return null;
  CurrentStep=Step.EasingBack;yield return null;
  CurrentStep=Step.ReadyToGetUp;
 }
 void GetUp()
 {
  if(!phases.FinishOpeningGetUp(standPoint))return;
  CurrentStep=Step.GettingUp;breathingOn=false;StartCoroutine(AfterGetUp());
 }
 IEnumerator AfterGetUp(){while(phases.IsTransitioning)yield return null;CurrentStep=settings.deskObjective?Step.GoToDesk:Step.Finished;}
 IEnumerator DeskHook()
 {
  CurrentStep=Step.DeskHook;while(phases.IsTransitioning)yield return null;
  OpeningTruthIntroduction.Invoke();
  if(openingTruthDialogue){bool done=false;if(phases.ui.TryShowDialogue(openingTruthDialogue,()=>done=true))while(!done&&phases.ui.DialogueActive)yield return null;}
  CurrentStep=Step.Finished;
 }
 // Stops the opening where it is (used when a night is forced from the debug panel).
 public void Cancel()
 {
  if(!IsRunning)return;StopAllCoroutines();if(phases.ui.DialogueActive)phases.ui.CancelDialogue();
  SetEyelids(1);breathingOn=false;CurrentStep=Step.Finished;
 }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 public void DebugSkipOpening()
 {
  if(!IsRunning)return;Cancel();
  phases.DebugEndOpening(standPoint);Debug.Log("[Opening] Skipped (development only).");
 }
#endif
}
