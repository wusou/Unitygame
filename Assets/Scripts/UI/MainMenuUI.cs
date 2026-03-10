using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class MainMenuUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference submitAction;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button readButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private bool autoBindButtonsByName = true;

    [Header("存档")]
    [SerializeField, Min(1)] private int saveSlotCount = 12;

    private bool clickListenersBound;

    private void Awake()
    {
        EnsureEventSystem();
        EnsureRuntimeManagers();

        TryAutoResolveButtons();
        TryAutoCreateReadButton();
        BindButtonClicks();
    }

    private void OnEnable()
    {
        submitAction?.action?.Enable();
    }

    private void OnDisable()
    {
        submitAction?.action?.Disable();
    }

    private void Update()
    {
        if (submitAction != null && submitAction.action != null && submitAction.action.WasPressedThisFrame())
        {
            PlayGame();
            return;
        }

        if (submitAction == null && IsFallbackSubmitPressed())
        {
            PlayGame();
        }
    }

    private static bool IsFallbackSubmitPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    public void PlayGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewRun();
            return;
        }

        LoadFallbackGameplayScene();
    }

    public void OpenReadSaves()
    {
        var saveManager = SaveGameManager.EnsureInstance();
        saveManager.ConfigureSlotCount(saveSlotCount);
        SaveSlotBrowserPanel.ShowFor(this, SavePanelMode.Load);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BindButtonClicks()
    {
        if (clickListenersBound)
        {
            return;
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(PlayGame);
        }

        if (readButton != null)
        {
            readButton.onClick.AddListener(OpenReadSaves);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        clickListenersBound = true;
    }

    private void TryAutoResolveButtons()
    {
        if (!autoBindButtonsByName)
        {
            return;
        }

        var buttons = FindObjectsOfType<Button>(true);
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            var searchText = BuildButtonSearchText(button);

            if (playButton == null && ContainsAny(searchText, "play", "start", "begin", "开始"))
            {
                playButton = button;
                continue;
            }

            if (readButton == null && ContainsAny(searchText, "read", "load", "continue", "读档", "读取", "继续"))
            {
                readButton = button;
                continue;
            }

            if (quitButton == null && ContainsAny(searchText, "quit", "exit", "close", "退出"))
            {
                quitButton = button;
            }
        }
    }

    private void TryAutoCreateReadButton()
    {
        if (readButton != null || playButton == null)
        {
            return;
        }

        var parent = playButton.transform.parent;
        if (parent == null)
        {
            return;
        }

        var clone = Instantiate(playButton, parent);
        clone.name = "Btn_Read";
        clone.onClick.RemoveAllListeners();

        var label = clone.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "读档";
        }

        readButton = clone;
    }

    private static string BuildButtonSearchText(Button button)
    {
        if (button == null)
        {
            return string.Empty;
        }

        var searchText = button.name;

        var tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            searchText += " " + tmp.text;
        }

        var text = button.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            searchText += " " + text.text;
        }

        return searchText.ToLowerInvariant();
    }

    private static bool ContainsAny(string source, params string[] words)
    {
        if (string.IsNullOrWhiteSpace(source) || words == null)
        {
            return false;
        }

        for (var i = 0; i < words.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(words[i]) && source.Contains(words[i]))
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

    private static void EnsureRuntimeManagers()
    {
        if (GameManager.Instance == null)
        {
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        if (InputBindingsManager.Instance == null)
        {
            var go = new GameObject("InputBindingsManager");
            go.AddComponent<InputBindingsManager>();
        }

        SaveGameManager.EnsureInstance();
    }

    private static void LoadFallbackGameplayScene()
    {
        var sceneCount = SceneManager.sceneCountInBuildSettings;
        if (sceneCount <= 0)
        {
            Debug.LogError("PlayGame 失败：Build Settings 中没有任何场景。");
            return;
        }

        var activeName = SceneManager.GetActiveScene().name;
        for (var i = 0; i < sceneCount; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!string.Equals(name, activeName, System.StringComparison.OrdinalIgnoreCase))
            {
                SceneManager.LoadScene(i);
                return;
            }
        }

        SceneManager.LoadScene(0);
    }
}


