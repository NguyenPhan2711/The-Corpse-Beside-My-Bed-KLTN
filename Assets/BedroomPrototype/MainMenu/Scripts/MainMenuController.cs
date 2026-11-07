using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    public Button playButton, settingsButton, quitButton;
    public SettingsPanel settings;
    public Image fade;
    [Tooltip("Scene name as listed in Build Settings.")] public string gameScene = "BedroomPrototype_PlayerFigure";
    [Min(0)] public float fadeSeconds = .6f;
    bool loading;
    void Awake()
    {
        playButton.onClick.AddListener(Play); settingsButton.onClick.AddListener(OpenSettings); quitButton.onClick.AddListener(Quit);
        SetFade(0);
    }
    void OnEnable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    void Start() { Select(playButton); }
    void Update()
    {
        // Clicking empty space clears the selection; restore it so keyboard navigation always has a starting point.
        if (!loading && EventSystem.current && !EventSystem.current.currentSelectedGameObject) Select(settings.IsOpen ? settings.firstSelected : playButton);
    }
    public static void Select(Selectable target) { if (target && EventSystem.current) EventSystem.current.SetSelectedGameObject(target.gameObject); }
    void OpenSettings() { if (!loading) settings.Open(() => Select(settingsButton)); }
    void Play() { if (!loading) StartCoroutine(LoadGame()); }
    IEnumerator LoadGame()
    {
        loading = true; playButton.interactable = settingsButton.interactable = quitButton.interactable = false;
        for (float t = 0; t < fadeSeconds; t += Time.unscaledDeltaTime) { SetFade(t / fadeSeconds); yield return null; }
        SetFade(1); SceneManager.LoadScene(gameScene);
    }
    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    void SetFade(float alpha) { var c = fade.color; c.a = Mathf.Clamp01(alpha); fade.color = c; fade.raycastTarget = alpha > .01f; }
}
