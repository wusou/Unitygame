using UnityEngine;

/// <summary>
/// 背包面板布局助手：把 UI 固定到屏幕中下部，适合 2D 横版视角。
/// </summary>
[ExecuteAlways]
public class BackpackPanelLayout : MonoBehaviour
{
    [SerializeField] private RectTransform target;

    [Header("位置")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, 36f);
    [SerializeField] private Vector2 panelSize = new Vector2(760f, 220f);

    [Header("行为")]
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool applyOnValidate = true;

    private void Reset()
    {
        target = GetComponent<RectTransform>();
        ApplyLayout();
    }

    private void OnEnable()
    {
        if (applyOnEnable)
        {
            ApplyLayout();
        }
    }

    private void OnValidate()
    {
        if (!applyOnValidate)
        {
            return;
        }

        ApplyLayout();
    }

    [ContextMenu("Apply Backpack Layout")]
    public void ApplyLayout()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
            if (target == null)
            {
                return;
            }
        }

        // 锚点与枢轴固定在底部中心。
        target.anchorMin = new Vector2(0.5f, 0f);
        target.anchorMax = new Vector2(0.5f, 0f);
        target.pivot = new Vector2(0.5f, 0f);

        // 让面板位于屏幕中下部并保持适中尺寸。
        target.anchoredPosition = anchoredPosition;
        target.sizeDelta = panelSize;
    }
}
