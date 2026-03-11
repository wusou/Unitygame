using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 小地图单元：被探索后关闭遮罩。
/// </summary>
public class MiniMapCell : MonoBehaviour
{
    [SerializeField] private GameObject fogMask;
    [SerializeField] private bool discoveredOnStart;

    [Header("遮罩显示")]
    [SerializeField] private bool autoCreateUiMaskImage = true;
    [SerializeField] private Color maskColor = new(0f, 0f, 0f, 0.78f);

    public bool IsDiscovered { get; private set; }

    private void Awake()
    {
        ResolveFogMaskIfNeeded();
        EnsureFogMaskVisualIfNeeded();
    }

    private void Start()
    {
        SetDiscovered(discoveredOnStart);
    }

    public void SetDiscovered(bool discovered)
    {
        IsDiscovered = discovered;
        if (fogMask != null)
        {
            fogMask.SetActive(!discovered);
        }
    }

    private void ResolveFogMaskIfNeeded()
    {
        if (fogMask != null)
        {
            return;
        }

        var child = transform.Find("FogMask");
        if (child != null)
        {
            fogMask = child.gameObject;
            return;
        }

        if (name.ToLowerInvariant().Contains("fogmask"))
        {
            fogMask = gameObject;
        }
    }

    private void EnsureFogMaskVisualIfNeeded()
    {
        if (!autoCreateUiMaskImage || fogMask == null)
        {
            return;
        }

        var fogRect = fogMask.GetComponent<RectTransform>();
        if (fogRect == null)
        {
            return;
        }

        var image = fogMask.GetComponent<Image>();
        if (image == null)
        {
            image = fogMask.AddComponent<Image>();
        }

        image.color = maskColor;
        image.raycastTarget = false;

        // 自动拉伸到父级，避免遮罩尺寸不匹配导致“看起来失效”。
        fogRect.anchorMin = Vector2.zero;
        fogRect.anchorMax = Vector2.one;
        fogRect.anchoredPosition = Vector2.zero;
        fogRect.sizeDelta = Vector2.zero;
    }
}