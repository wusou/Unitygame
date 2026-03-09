using TMPro;
using UnityEngine;

/// <summary>
/// 可选UI：接收NPC文本并自动隐藏。
/// </summary>
public class NpcDialoguePanel : MonoBehaviour
{
    private static NpcDialoguePanel instance;

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;

    private float hideTimer;

    private void Awake()
    {
        instance = this;
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void Update()
    {
        if (hideTimer <= 0f)
        {
            return;
        }

        hideTimer -= Time.unscaledDeltaTime;
        if (hideTimer <= 0f && root != null)
        {
            root.SetActive(false);
        }
    }

    public static void Show(string message, float duration)
    {
        if (instance == null)
        {
            Debug.Log(message);
            return;
        }

        if (instance.messageText != null)
        {
            instance.messageText.text = message;
        }

        if (instance.root != null)
        {
            instance.root.SetActive(true);
        }

        instance.hideTimer = Mathf.Max(0.5f, duration);
    }
}
