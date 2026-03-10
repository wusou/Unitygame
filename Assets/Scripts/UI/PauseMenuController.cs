using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class PauseMenuController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("UI")]
    [SerializeField] private GameObject pauseRoot;
    [SerializeField] private TMP_Text pauseHintText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private bool autoBindButtons = true;
    [SerializeField] private bool ensureEventSystem = true;
    [SerializeField] private bool showCursorWhenPaused = true;

    [Header("存档")]
    [SerializeField, Min(1)] private int saveSlotCount = 12;

    private bool isPaused;
    private bool cachedCursorVisible;
    private CursorLockMode cachedCursorLockMode;

    private void Awake()
    {
        if (ensureEventSystem)
        {
            EnsureEventSystem();
        }

        SaveGameManager.EnsureInstance().ConfigureSlotCount(saveSlotCount);
        RefreshBindings();
    }

    private void OnEnable()
    {
        pauseAction?.action?.Enable();
    }

    private void OnDisable()
    {
        pauseAction?.action?.Disable();
        if (isPaused)
        {
            SetPaused(false);
        }
    }

    private void Start()
    {
        RefreshPauseHint();
        SetPaused(false);
    }

    private void Update()
    {
        if (pauseAction != null && pauseAction.action != null && pauseAction.action.WasPressedThisFrame())
        {
            SetPaused(!isPaused);
            return;
        }

        if (pauseAction == null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetPaused(!isPaused);
        }
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void OpenSavePanel()
    {
        SaveGameManager.EnsureInstance().ConfigureSlotCount(saveSlotCount);
        SaveSlotBrowserPanel.ShowFor(this, SavePanelMode.Save);
    }

    public void BackToMainMenu()
    {
        SetPaused(false);
        GameManager.Instance?.LoadMainMenu();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPaused(bool pause)
    {
        isPaused = pause;
        if (pauseRoot != null)
        {
            pauseRoot.SetActive(isPaused);
        }

        Time.timeScale = isPaused ? 0f : 1f;
        UpdateCursorState();

        if (isPaused)
        {
            RefreshBindings();
            FocusResumeButton();
        }
    }

    private void UpdateCursorState()
    {
        if (!showCursorWhenPaused)
        {
            return;
        }

        if (isPaused)
        {
            cachedCursorVisible = Cursor.visible;
            cachedCursorLockMode = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        Cursor.visible = cachedCursorVisible;
        Cursor.lockState = cachedCursorLockMode;
    }

    private void FocusResumeButton()
    {
        if (resumeButton == null || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    private void RefreshPauseHint()
    {
        if (pauseHintText == null)
        {
            return;
        }

        if (pauseAction != null && pauseAction.action != null)
        {
            var binding = pauseAction.action.GetBindingDisplayString();
            pauseHintText.text = $"暂停: {binding}";
            return;
        }

        pauseHintText.text = "暂停: Esc";
    }

    private void RefreshBindings()
    {
        TryAutoResolvePauseRoot();

        if (autoBindButtons)
        {
            TryAutoResolveButtons();
            TryAutoCreateSaveButton();
        }

        RebindButtonClicks();
    }

    private void TryAutoResolvePauseRoot()
    {
        if (pauseRoot != null)
        {
            return;
        }

        var child = transform.Find("PauseRoot");
        if (child != null)
        {
            pauseRoot = child.gameObject;
            return;
        }

        var scenePauseRoot = GameObject.Find("PauseRoot");
        if (scenePauseRoot != null)
        {
            pauseRoot = scenePauseRoot;
        }
    }

    private void TryAutoResolveButtons()
    {
        var root = pauseRoot != null ? pauseRoot.transform : transform;
        var buttons = root.GetComponentsInChildren<Button>(true);

        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            var text = GetButtonSearchText(button);

            if (resumeButton == null && ContainsAny(text, "resume", "continue", "继续"))
            {
                resumeButton = button;
                continue;
            }

            if (saveButton == null && ContainsAny(text, "save", "保存", "存档"))
            {
                saveButton = button;
                continue;
            }

            if (mainMenuButton == null && ContainsAny(text, "mainmenu", "main_menu", "menu", "主菜单", "返回菜单"))
            {
                mainMenuButton = button;
                continue;
            }

            if (quitButton == null && ContainsAny(text, "quit", "exit", "close", "退出"))
            {
                quitButton = button;
            }
        }
    }

    private void TryAutoCreateSaveButton()
    {
        if (saveButton != null)
        {
            return;
        }

        var source = resumeButton != null ? resumeButton : mainMenuButton;
        if (source == null || source.transform.parent == null)
        {
            return;
        }

        var clone = Instantiate(source, source.transform.parent);
        clone.name = "Btn_Save";
        clone.onClick.RemoveAllListeners();

        var label = clone.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "存档";
        }

        saveButton = clone;
    }

    private void RebindButtonClicks()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Resume);
            resumeButton.onClick.AddListener(Resume);
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(OpenSavePanel);
            saveButton.onClick.AddListener(OpenSavePanel);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(BackToMainMenu);
            mainMenuButton.onClick.AddListener(BackToMainMenu);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
        else if (isPaused)
        {
            Debug.LogWarning("PauseMenuController: 未找到 Quit 按钮，请手动绑定 quitButton 字段。");
        }
    }

    private static string GetButtonSearchText(Button button)
    {
        if (button == null)
        {
            return string.Empty;
        }

        var text = button.name;

        var tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            text += " " + tmp.text;
        }

        var legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            text += " " + legacyText.text;
        }

        return text.ToLowerInvariant();
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        if (string.IsNullOrEmpty(source) || keywords == null)
        {
            return false;
        }

        for (var i = 0; i < keywords.Length; i++)
        {
            var keyword = keywords[i];
            if (!string.IsNullOrEmpty(keyword) && source.Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }
}
