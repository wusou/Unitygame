using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private InputActionReference submitAction;

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
        GameManager.Instance?.StartNewRun();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
