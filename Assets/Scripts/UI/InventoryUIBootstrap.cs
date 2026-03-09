using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 运行时背包UI兜底创建器：当场景里没有背包面板时自动创建，保证新手场景也能直接看到背包与武器图标。
/// </summary>
public static class InventoryUIBootstrap
{
    private const string RuntimeCanvasName = "RuntimeUICanvas";
    private const string RuntimePanelName = "RuntimeInventoryPanel";

    public static void EnsureUI(PlayerWeaponInventory inventory)
    {
        if (inventory == null)
        {
            return;
        }

        var existing = Object.FindObjectOfType<InventoryUIController>(true);
        if (existing != null)
        {
            existing.BindInventory(inventory);
            return;
        }

        var canvas = FindOrCreateCanvas();
        if (canvas == null)
        {
            return;
        }

        var panel = CreatePanel(canvas.transform as RectTransform);
        if (panel == null)
        {
            return;
        }

        var controller = panel.GetComponent<InventoryUIController>();
        controller.BindInventory(inventory);
    }

    private static Canvas FindOrCreateCanvas()
    {
        var canvases = Object.FindObjectsOfType<Canvas>(true);
        for (var i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].isRootCanvas)
            {
                return canvases[i];
            }
        }

        var go = new GameObject(RuntimeCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static GameObject CreatePanel(RectTransform canvasRect)
    {
        if (canvasRect == null)
        {
            return null;
        }

        var panelGo = new GameObject(
            RuntimePanelName,
            typeof(RectTransform),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(BackpackPanelLayout),
            typeof(InventoryUIController));

        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.SetParent(canvasRect, false);

        var image = panelGo.GetComponent<Image>();
        image.color = new Color(0.08f, 0.11f, 0.14f, 0.7f);

        var canvasGroup = panelGo.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 1f;

        CreateText(panelRect, "CapacityText", new Vector2(0.02f, 0.72f), new Vector2(0.34f, 0.94f), "背包容量: 0/0", TextAlignmentOptions.Left);
        CreateText(panelRect, "CurrentText", new Vector2(0.02f, 0.5f), new Vector2(0.34f, 0.72f), "当前武器: 无", TextAlignmentOptions.Left);
        CreateText(panelRect, "WeaponListText", new Vector2(0.02f, 0.08f), new Vector2(0.34f, 0.5f), "1. (空)", TextAlignmentOptions.TopLeft);

        var slotsGo = new GameObject("SlotContainer", typeof(RectTransform));
        var slotsRect = slotsGo.GetComponent<RectTransform>();
        slotsRect.SetParent(panelRect, false);
        slotsRect.anchorMin = new Vector2(0.38f, 0.1f);
        slotsRect.anchorMax = new Vector2(0.98f, 0.9f);
        slotsRect.offsetMin = Vector2.zero;
        slotsRect.offsetMax = Vector2.zero;

        return panelGo;
    }

    private static TMP_Text CreateText(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        string defaultText,
        TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = defaultText;
        text.fontSize = 28f;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        return text;
    }
}
