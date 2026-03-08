using TMPro;
using UnityEngine;

public class DeathScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text runCountText;

    private void OnEnable()
    {
        if (runCountText != null && GameManager.Instance != null)
        {
            runCountText.text = $"第 {GameManager.Instance.RunCount} 次尝试";
        }
    }

    public void Retry()
    {
        GameManager.Instance?.Respawn();
        gameObject.SetActive(false);
    }

    public void ToMain()
    {
        GameManager.Instance?.LoadMainMenu();
    }
}
