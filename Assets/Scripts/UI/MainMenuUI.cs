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
    [SerializeField] private Button quitButton;
    [SerializeField] private bool autoBindButtonsByName = true;

    private bool clickListenersBound;

    private void Awake()
    {
        EnsureEventSystem();
        EnsureRuntimeManagers();
        TryAutoResolveButtons();
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

        if (submitAction == null && Input.GetKeyDown(KeyCode.Return))
        {
            PlayGame();
        }
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

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        clickListenersBound = true;
    }

    private void TryAutoResolveButtons()
    {
        if (!autoBindButtonsByName || (playButton != null && quitButton != null))
        {
            return;
        }

        var buttons = FindObjectsOfType<Button>(true);
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            var lower = button.name.ToLowerInvariant();

            if (playButton == null && (lower.Contains("play") || lower.Contains("start") || lower.Contains("begin") || lower.Contains("开始")))
            {
                playButton = button;
                continue;
            }

            if (quitButton == null && (lower.Contains("quit") || lower.Contains("exit") || lower.Contains("close") || lower.Contains("退出")))
            {
                quitButton = button;
            }
        }
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
