using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("UI")]
    [SerializeField] private GameObject pauseRoot;
    [SerializeField] private TMP_Text pauseHintText;

    private bool isPaused;

    private void OnEnable()
    {
        pauseAction?.action?.Enable();
    }

    private void OnDisable()
    {
        pauseAction?.action?.Disable();
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

        if (pauseAction == null && Input.GetKeyDown(KeyCode.Escape))
        {
            SetPaused(!isPaused);
        }
    }

    public void Resume()
    {
        SetPaused(false);
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
}
