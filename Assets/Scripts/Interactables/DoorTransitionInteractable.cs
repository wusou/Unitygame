using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 触发式门交互：站在门前按F切换场景。
/// </summary>
public class DoorTransitionInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionTitle = "进入";
    [SerializeField] private string targetSceneName = "SampleScene";
    [SerializeField] private string targetSpawnPointId;

    public string InteractionTitle => interactionTitle;

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !string.IsNullOrWhiteSpace(targetSceneName);
    }

    public void Interact(PlayerInteractor interactor)
    {
        SceneTransitionContext.PendingSpawnId = targetSpawnPointId;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadSceneByName(targetSceneName);
            return;
        }

        if (GameManager.SceneExistsInBuild(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
