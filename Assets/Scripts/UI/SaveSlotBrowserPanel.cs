using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum SavePanelMode
{
    Load = 0,
    Save = 1
}

public class SaveSlotBrowserPanel : MonoBehaviour, IPointerClickHandler
{
    private sealed class SlotRowView
    {
        public GameObject Root;
        public Image Background;
        public TMP_Text TitleText;
        public TMP_Text DetailText;
        public SaveSlotRowItem RowItem;
    }

    [Header("分页")]
    [SerializeField, Min(1)] private int slotsPerPage = 6;
    [SerializeField, Min(0.15f)] private float doubleClickInterval = 0.35f;

    [Header("外观")]
    [SerializeField] private Color panelColor = new Color(0.12f, 0.14f, 0.18f, 0.96f);
    [SerializeField] private Color rowColor = new Color(0.2f, 0.24f, 0.3f, 0.92f);
    [SerializeField] private Color rowSelectedColor = new Color(0.3f, 0.42f, 0.58f, 0.96f);
    [SerializeField] private Color rowEmptyColor = new Color(0.17f, 0.17f, 0.17f, 0.78f);

    private Canvas rootCanvas;
    private RectTransform rootRect;
    private RectTransform windowRect;
    private RectTransform rowsRect;

    private TMP_Text titleText;
    private TMP_Text pageText;
    private TMP_Text statusText;

    private Button prevPageButton;
    private Button nextPageButton;
    private Button closeButton;

    private RectTransform contextMenuRect;
    private Button contextRenameButton;
    private Button contextDeleteButton;
    private Button contextCancelButton;

    private RectTransform confirmRect;
    private TMP_Text confirmText;
    private Button confirmYesButton;
    private Button confirmNoButton;

    private RectTransform renameRect;
    private TMP_InputField renameInput;
    private Button renameOkButton;
    private Button renameCancelButton;

    private readonly List<SlotRowView> rowViews = new();

    private SavePanelMode mode;
    private int currentPage;
    private int selectedSlot = -1;
    private int contextSlot = -1;
    private Action confirmAction;

    private bool layoutBuilt;

    public static SaveSlotBrowserPanel ShowFor(Component owner, SavePanelMode mode)
    {
        var panel = FindObjectOfType<SaveSlotBrowserPanel>(true);
        if (panel == null)
        {
            panel = CreatePanel(owner);
        }

        panel.Open(mode);
        return panel;
    }

    private static SaveSlotBrowserPanel CreatePanel(Component owner)
    {
        var canvas = FindTargetCanvas(owner);
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }

        var go = new GameObject("SaveSlotBrowserPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        var panel = go.AddComponent<SaveSlotBrowserPanel>();
        panel.rootCanvas = canvas;
        return panel;
    }

    private static Canvas FindTargetCanvas(Component owner)
    {
        if (owner != null)
        {
            var parentCanvas = owner.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null)
            {
                return parentCanvas;
            }
        }

        return FindObjectOfType<Canvas>();
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("RuntimeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private void Awake()
    {
        SaveGameManager.EnsureInstance();
        EnsureLayout();
        HideAllPopups();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        SaveGameManager.EnsureInstance().SlotsChanged += OnSlotsChanged;
    }

    private void OnDisable()
    {
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.SlotsChanged -= OnSlotsChanged;
        }
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || !WasCancelPressedThisFrame())
        {
            return;
        }

        if (renameRect != null && renameRect.gameObject.activeSelf)
        {
            HideRenameDialog();
            return;
        }

        if (confirmRect != null && confirmRect.gameObject.activeSelf)
        {
            HideConfirmDialog();
            return;
        }

        if (contextMenuRect != null && contextMenuRect.gameObject.activeSelf)
        {
            HideContextMenu();
            return;
        }

        ClosePanel();
    }

    private static bool WasCancelPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        return keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame);
#else
        return false;
#endif
    }

    public void Open(SavePanelMode newMode)
    {
        mode = newMode;
        SaveGameManager.EnsureInstance();
        EnsureLayout();

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        selectedSlot = -1;
        contextSlot = -1;
        confirmAction = null;

        titleText.text = mode == SavePanelMode.Load ? "读档列表" : "存档列表";
        SetStatus(string.Empty, false);

        currentPage = 0;
        RefreshPage();
        HideAllPopups();
    }

    private void EnsureLayout()
    {
        if (layoutBuilt)
        {
            return;
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        rootRect = GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = gameObject.AddComponent<RectTransform>();
        }

        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        var overlay = GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.62f);
        overlay.raycastTarget = true;

        var window = CreateUIObject("Window", rootRect, typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(980f, 760f);

        var windowImage = window.GetComponent<Image>();
        windowImage.color = panelColor;

        var layout = window.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 20, 20);
        layout.spacing = 14f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = window.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        BuildHeader(windowRect);
        BuildRows(windowRect);
        BuildFooter(windowRect);
        BuildContextMenu(rootRect);
        BuildConfirmDialog(rootRect);
        BuildRenameDialog(rootRect);

        layoutBuilt = true;
    }

    private void BuildHeader(Transform parent)
    {
        var header = CreateUIObject("Header", parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        var headerLayout = header.GetComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 10f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlHeight = false;
        headerLayout.childControlWidth = false;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childForceExpandWidth = true;

        var headerElement = header.GetComponent<LayoutElement>();
        headerElement.preferredHeight = 56f;

        titleText = CreateText("Title", header.transform, "存档", 34f, FontStyles.Bold, TextAlignmentOptions.Left);
        var titleRect = titleText.rectTransform;
        titleRect.sizeDelta = new Vector2(680f, 50f);

        closeButton = CreateButton("CloseButton", header.transform, "关闭", new Vector2(130f, 46f));
        closeButton.onClick.AddListener(ClosePanel);
    }

    private void BuildRows(Transform parent)
    {
        var paging = CreateUIObject("Paging", parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        var pagingLayout = paging.GetComponent<HorizontalLayoutGroup>();
        pagingLayout.spacing = 10f;
        pagingLayout.childAlignment = TextAnchor.MiddleCenter;
        pagingLayout.childControlHeight = false;
        pagingLayout.childControlWidth = false;
        pagingLayout.childForceExpandHeight = false;
        pagingLayout.childForceExpandWidth = false;

        var pagingElement = paging.GetComponent<LayoutElement>();
        pagingElement.preferredHeight = 54f;

        prevPageButton = CreateButton("PrevPage", paging.transform, "上一页", new Vector2(140f, 44f));
        prevPageButton.onClick.AddListener(PrevPage);

        pageText = CreateText("PageText", paging.transform, "第 1/1 页", 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        pageText.rectTransform.sizeDelta = new Vector2(220f, 44f);

        nextPageButton = CreateButton("NextPage", paging.transform, "下一页", new Vector2(140f, 44f));
        nextPageButton.onClick.AddListener(NextPage);

        var rows = CreateUIObject("Rows", parent, typeof(VerticalLayoutGroup), typeof(LayoutElement));
        rowsRect = rows.GetComponent<RectTransform>();

        var rowsLayout = rows.GetComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 8f;
        rowsLayout.childAlignment = TextAnchor.UpperCenter;
        rowsLayout.childControlHeight = false;
        rowsLayout.childControlWidth = true;
        rowsLayout.childForceExpandHeight = false;
        rowsLayout.childForceExpandWidth = true;

        var rowsElement = rows.GetComponent<LayoutElement>();
        rowsElement.preferredHeight = 500f;

        for (var i = 0; i < slotsPerPage; i++)
        {
            rowViews.Add(CreateRowView(rows.transform, i));
        }
    }

    private void BuildFooter(Transform parent)
    {
        var footer = CreateUIObject("Footer", parent, typeof(Image), typeof(LayoutElement));
        var footerLayout = footer.GetComponent<LayoutElement>();
        footerLayout.preferredHeight = 38f;

        var footerImage = footer.GetComponent<Image>();
        footerImage.color = new Color(0f, 0f, 0f, 0.18f);
        footerImage.raycastTarget = false;

        statusText = CreateText("Status", footer.transform, string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.Left);
        statusText.color = new Color(0.9f, 0.93f, 1f, 0.92f);
        statusText.raycastTarget = false;
        statusText.enableWordWrapping = false;
        statusText.overflowMode = TextOverflowModes.Ellipsis;

        var statusRect = statusText.rectTransform;
        statusRect.anchorMin = Vector2.zero;
        statusRect.anchorMax = Vector2.one;
        statusRect.offsetMin = new Vector2(12f, 4f);
        statusRect.offsetMax = new Vector2(-12f, -4f);
    }

    private void BuildContextMenu(Transform parent)
    {
        var context = CreateUIObject("ContextMenu", parent, typeof(Image), typeof(VerticalLayoutGroup));
        contextMenuRect = context.GetComponent<RectTransform>();
        contextMenuRect.anchorMin = new Vector2(0.5f, 0.5f);
        contextMenuRect.anchorMax = new Vector2(0.5f, 0.5f);
        contextMenuRect.pivot = new Vector2(0f, 1f);
        contextMenuRect.sizeDelta = new Vector2(220f, 176f);

        var bg = context.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);

        var layout = context.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        contextRenameButton = CreateButton("Rename", context.transform, "重命名", new Vector2(0f, 42f));
        contextRenameButton.onClick.AddListener(OnContextRenameClicked);

        contextDeleteButton = CreateButton("Delete", context.transform, "删除", new Vector2(0f, 42f));
        contextDeleteButton.onClick.AddListener(OnContextDeleteClicked);

        contextCancelButton = CreateButton("Cancel", context.transform, "取消", new Vector2(0f, 42f));
        contextCancelButton.onClick.AddListener(HideContextMenu);

        context.gameObject.SetActive(false);
    }

    private void BuildConfirmDialog(Transform parent)
    {
        var dialog = CreateUIObject("ConfirmDialog", parent, typeof(Image), typeof(VerticalLayoutGroup));
        confirmRect = dialog.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0.5f);
        confirmRect.anchorMax = new Vector2(0.5f, 0.5f);
        confirmRect.pivot = new Vector2(0.5f, 0.5f);
        confirmRect.sizeDelta = new Vector2(560f, 260f);

        var bg = dialog.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);

        var layout = dialog.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 16f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        confirmText = CreateText("ConfirmText", dialog.transform, "确认操作", 26f, FontStyles.Normal, TextAlignmentOptions.Center);
        confirmText.rectTransform.sizeDelta = new Vector2(500f, 110f);

        var buttonRow = CreateUIObject("Buttons", dialog.transform, typeof(HorizontalLayoutGroup));
        var rowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childControlHeight = false;
        rowLayout.childControlWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;

        confirmYesButton = CreateButton("Yes", buttonRow.transform, "确认", new Vector2(160f, 48f));
        confirmYesButton.onClick.AddListener(OnConfirmYesClicked);

        confirmNoButton = CreateButton("No", buttonRow.transform, "取消", new Vector2(160f, 48f));
        confirmNoButton.onClick.AddListener(HideConfirmDialog);

        dialog.gameObject.SetActive(false);
    }

    private void BuildRenameDialog(Transform parent)
    {
        var dialog = CreateUIObject("RenameDialog", parent, typeof(Image), typeof(VerticalLayoutGroup));
        renameRect = dialog.GetComponent<RectTransform>();
        renameRect.anchorMin = new Vector2(0.5f, 0.5f);
        renameRect.anchorMax = new Vector2(0.5f, 0.5f);
        renameRect.pivot = new Vector2(0.5f, 0.5f);
        renameRect.sizeDelta = new Vector2(620f, 310f);

        var bg = dialog.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);

        var layout = dialog.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 16f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var title = CreateText("RenameTitle", dialog.transform, "重命名存档", 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        title.rectTransform.sizeDelta = new Vector2(560f, 52f);

        renameInput = CreateInputField(dialog.transform);

        var buttonRow = CreateUIObject("Buttons", dialog.transform, typeof(HorizontalLayoutGroup));
        var rowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlHeight = false;
        rowLayout.childControlWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = false;

        renameOkButton = CreateButton("RenameOk", buttonRow.transform, "确认", new Vector2(160f, 48f));
        renameOkButton.onClick.AddListener(OnRenameConfirmClicked);

        renameCancelButton = CreateButton("RenameCancel", buttonRow.transform, "取消", new Vector2(160f, 48f));
        renameCancelButton.onClick.AddListener(HideRenameDialog);

        dialog.gameObject.SetActive(false);
    }

    private SlotRowView CreateRowView(Transform parent, int rowIndex)
    {
        var rowObject = CreateUIObject($"Row_{rowIndex + 1:00}", parent, typeof(Image), typeof(LayoutElement));
        var rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, 74f);

        var rowImage = rowObject.GetComponent<Image>();
        rowImage.color = rowColor;

        var rowElement = rowObject.GetComponent<LayoutElement>();
        rowElement.preferredHeight = 74f;

        var rowItem = rowObject.AddComponent<SaveSlotRowItem>();

        var textRoot = CreateUIObject("Texts", rowObject.transform, typeof(VerticalLayoutGroup));
        var textRect = textRoot.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(16f, 8f);
        textRect.offsetMax = new Vector2(-16f, -8f);

        var textLayout = textRoot.GetComponent<VerticalLayoutGroup>();
        textLayout.spacing = 2f;
        textLayout.childAlignment = TextAnchor.MiddleLeft;
        textLayout.childControlHeight = false;
        textLayout.childControlWidth = true;
        textLayout.childForceExpandHeight = false;
        textLayout.childForceExpandWidth = true;

        var title = CreateText("Title", textRoot.transform, "", 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        title.rectTransform.sizeDelta = new Vector2(0f, 34f);

        var detail = CreateText("Detail", textRoot.transform, "", 19f, FontStyles.Normal, TextAlignmentOptions.Left);
        detail.color = new Color(0.82f, 0.86f, 0.95f, 0.95f);
        detail.rectTransform.sizeDelta = new Vector2(0f, 30f);

        return new SlotRowView
        {
            Root = rowObject,
            Background = rowImage,
            TitleText = title,
            DetailText = detail,
            RowItem = rowItem
        };
    }

    private void RefreshPage()
    {
        var save = SaveGameManager.EnsureInstance();
        var allSlots = save.GetSlotSummaries();
        var maxSlots = Mathf.Max(1, save.MaxSlots);
        var totalPages = Mathf.Max(1, Mathf.CeilToInt(maxSlots / (float)Mathf.Max(1, slotsPerPage)));

        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
        pageText.text = $"第 {currentPage + 1}/{totalPages} 页";

        prevPageButton.interactable = currentPage > 0;
        nextPageButton.interactable = currentPage < totalPages - 1;

        for (var i = 0; i < rowViews.Count; i++)
        {
            var view = rowViews[i];
            var slotIndex = currentPage * slotsPerPage + i;

            if (slotIndex >= maxSlots)
            {
                view.Root.SetActive(false);
                continue;
            }

            view.Root.SetActive(true);
            var summary = allSlots[slotIndex];

            if (summary.Exists)
            {
                view.TitleText.text = $"{slotIndex + 1}. {summary.DisplayName}";
                view.DetailText.text = $"场景: {summary.SceneName}    时间: {summary.SavedAtText}";
            }
            else
            {
                view.TitleText.text = $"{slotIndex + 1}. <空存档>";
                view.DetailText.text = mode == SavePanelMode.Save
                    ? "点击即可保存到此槽位"
                    : "双击可读档（当前为空）";
            }

            var targetColor = summary.Exists ? rowColor : rowEmptyColor;
            if (slotIndex == selectedSlot)
            {
                targetColor = rowSelectedColor;
            }

            view.Background.color = targetColor;
            view.RowItem.Configure(slotIndex, doubleClickInterval, OnSlotLeftClick, OnSlotDoubleClick, OnSlotRightClick);
        }
    }

    private void OnSlotsChanged()
    {
        RefreshPage();
    }

    private void PrevPage()
    {
        currentPage--;
        RefreshPage();
    }

    private void NextPage()
    {
        currentPage++;
        RefreshPage();
    }

    private void OnSlotLeftClick(int slotIndex)
    {
        selectedSlot = slotIndex;
        RefreshPage();

        var summary = SaveGameManager.EnsureInstance().GetSlotSummary(slotIndex);

        if (mode == SavePanelMode.Save)
        {
            if (summary.Exists)
            {
                ShowConfirm(
                    $"存档 {slotIndex + 1} 已有内容。\n是否覆盖【{summary.DisplayName}】？",
                    () => ExecuteSave(slotIndex, true));
            }
            else
            {
                ExecuteSave(slotIndex, false);
            }

            return;
        }

        if (!summary.Exists)
        {
            SetStatus($"存档 {slotIndex + 1} 为空，无法读档。", true);
        }
        else
        {
            SetStatus(string.Empty, false);
        }
    }

    private void OnSlotDoubleClick(int slotIndex)
    {
        if (mode != SavePanelMode.Load)
        {
            return;
        }

        selectedSlot = slotIndex;
        RefreshPage();

        var summary = SaveGameManager.EnsureInstance().GetSlotSummary(slotIndex);
        if (!summary.Exists)
        {
            SetStatus($"存档 {slotIndex + 1} 为空，无法读档。", true);
            return;
        }

        if (SaveGameManager.EnsureInstance().TryLoadSlot(slotIndex, out var message))
        {
            SetStatus(message, false);
            ClosePanel();
        }
        else
        {
            SetStatus(message, true);
        }
    }

    private void OnSlotRightClick(int slotIndex, Vector2 screenPosition)
    {
        var summary = SaveGameManager.EnsureInstance().GetSlotSummary(slotIndex);
        if (!summary.Exists)
        {
            SetStatus("空存档槽无法重命名或删除。", true);
            return;
        }

        contextSlot = slotIndex;
        ShowContextMenu(screenPosition);
    }

    private void ExecuteSave(int slotIndex, bool overwrite)
    {
        if (SaveGameManager.EnsureInstance().TrySaveSlot(slotIndex, overwrite, out var message))
        {
            SetStatus(message, false);
            ClosePanel();
            return;
        }

        SetStatus(message, true);
        RefreshPage();
    }

    private void OnContextRenameClicked()
    {
        HideContextMenu();

        if (contextSlot < 0)
        {
            return;
        }

        var summary = SaveGameManager.EnsureInstance().GetSlotSummary(contextSlot);
        if (!summary.Exists)
        {
            SetStatus("空存档槽无法重命名。", true);
            return;
        }

        renameInput.text = summary.DisplayName;
        renameRect.gameObject.SetActive(true);
        renameRect.SetAsLastSibling();
        renameInput.ActivateInputField();
    }

    private void OnContextDeleteClicked()
    {
        HideContextMenu();

        if (contextSlot < 0)
        {
            return;
        }

        var summary = SaveGameManager.EnsureInstance().GetSlotSummary(contextSlot);
        if (!summary.Exists)
        {
            SetStatus("空存档槽无法删除。", true);
            return;
        }

        ShowConfirm(
            $"确定删除存档 {contextSlot + 1}【{summary.DisplayName}】吗？",
            () =>
            {
                if (SaveGameManager.EnsureInstance().TryDeleteSlot(contextSlot, out var message))
                {
                    SetStatus(message, false);
                }
                else
                {
                    SetStatus(message, true);
                }

                RefreshPage();
            });
    }

    private void OnRenameConfirmClicked()
    {
        if (contextSlot < 0)
        {
            HideRenameDialog();
            return;
        }

        var newName = renameInput.text;
        if (SaveGameManager.EnsureInstance().TryRenameSlot(contextSlot, newName, out var message))
        {
            SetStatus(message, false);
            HideRenameDialog();
            RefreshPage();
            return;
        }

        SetStatus(message, true);
    }

    private void ShowContextMenu(Vector2 screenPosition)
    {
        HideAllPopups();

        contextMenuRect.gameObject.SetActive(true);
        contextMenuRect.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contextMenuRect);

        var camera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screenPosition, camera, out var localPoint))
        {
            localPoint = Vector2.zero;
        }

        const float edgePadding = 10f;
        var menuSize = contextMenuRect.rect.size;
        var rootBounds = rootRect.rect;

        var x = Mathf.Clamp(localPoint.x + 6f, rootBounds.xMin + edgePadding, rootBounds.xMax - menuSize.x - edgePadding);
        var y = Mathf.Clamp(localPoint.y - 6f, rootBounds.yMin + menuSize.y + edgePadding, rootBounds.yMax - edgePadding);

        contextMenuRect.anchoredPosition = new Vector2(x, y);
    }

    private void HideContextMenu()
    {
        if (contextMenuRect != null)
        {
            contextMenuRect.gameObject.SetActive(false);
        }
    }

    private void ShowConfirm(string content, Action onConfirm)
    {
        HideAllPopups();

        confirmAction = onConfirm;
        confirmText.text = content;
        confirmRect.gameObject.SetActive(true);
        confirmRect.SetAsLastSibling();
    }

    private void OnConfirmYesClicked()
    {
        var callback = confirmAction;
        HideConfirmDialog();
        callback?.Invoke();
    }

    private void HideConfirmDialog()
    {
        confirmAction = null;
        if (confirmRect != null)
        {
            confirmRect.gameObject.SetActive(false);
        }
    }

    private void HideRenameDialog()
    {
        if (renameRect != null)
        {
            renameRect.gameObject.SetActive(false);
        }
    }

    private void HideAllPopups()
    {
        HideContextMenu();
        HideConfirmDialog();
        HideRenameDialog();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        var hitObject = eventData.pointerPressRaycast.gameObject ?? eventData.pointerCurrentRaycast.gameObject;
        if (hitObject != gameObject)
        {
            return;
        }

        if (renameRect != null && renameRect.gameObject.activeSelf)
        {
            HideRenameDialog();
            return;
        }

        if (confirmRect != null && confirmRect.gameObject.activeSelf)
        {
            HideConfirmDialog();
            return;
        }

        if (contextMenuRect != null && contextMenuRect.gameObject.activeSelf)
        {
            HideContextMenu();
            return;
        }

        ClosePanel();
    }

    private void SetStatus(string message, bool isError)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
        statusText.color = isError
            ? new Color(1f, 0.47f, 0.47f, 1f)
            : new Color(0.87f, 0.95f, 1f, 1f);
    }

    private void ClosePanel()
    {
        HideAllPopups();
        gameObject.SetActive(false);
    }

    private static GameObject CreateUIObject(string name, Transform parent, params Type[] components)
    {
        var go = new GameObject(name, components);
        var rect = go.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;
        }
        else
        {
            go.transform.SetParent(parent, false);
        }

        return go;
    }

    private static TMP_Text CreateText(string name, Transform parent, string content, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        var go = CreateUIObject(name, parent, typeof(TextMeshProUGUI));
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.96f, 0.97f, 1f, 1f);
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string caption, Vector2 size)
    {
        var go = CreateUIObject(name, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
        var rect = go.GetComponent<RectTransform>();
        if (size.x > 0f)
        {
            rect.sizeDelta = size;
        }

        var image = go.GetComponent<Image>();
        image.color = new Color(0.22f, 0.38f, 0.56f, 0.96f);

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.28f, 0.46f, 0.67f, 1f);
        colors.pressedColor = new Color(0.18f, 0.3f, 0.45f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);
        button.colors = colors;

        var layout = go.GetComponent<LayoutElement>();
        if (size.y > 0f)
        {
            layout.preferredHeight = size.y;
        }

        if (size.x > 0f)
        {
            layout.preferredWidth = size.x;
        }

        var label = CreateText("Label", go.transform, caption, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private static TMP_InputField CreateInputField(Transform parent)
    {
        var root = CreateUIObject("InputField", parent, typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(560f, 60f);

        var layout = root.GetComponent<LayoutElement>();
        layout.preferredHeight = 60f;

        var image = root.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.24f, 0.96f);

        var input = root.GetComponent<TMP_InputField>();
        input.lineType = TMP_InputField.LineType.SingleLine;

        var textArea = CreateUIObject("Text Area", root.transform, typeof(RectTransform), typeof(RectMask2D));
        var areaRect = textArea.GetComponent<RectTransform>();
        areaRect.anchorMin = Vector2.zero;
        areaRect.anchorMax = Vector2.one;
        areaRect.offsetMin = new Vector2(12f, 8f);
        areaRect.offsetMax = new Vector2(-12f, -8f);

        var text = CreateText("Text", textArea.transform, string.Empty, 24f, FontStyles.Normal, TextAlignmentOptions.Left);
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        var textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var placeholder = CreateText("Placeholder", textArea.transform, "输入新存档名", 22f, FontStyles.Italic, TextAlignmentOptions.Left);
        placeholder.color = new Color(0.7f, 0.73f, 0.8f, 0.68f);
        var placeRect = placeholder.rectTransform;
        placeRect.anchorMin = Vector2.zero;
        placeRect.anchorMax = Vector2.one;
        placeRect.offsetMin = Vector2.zero;
        placeRect.offsetMax = Vector2.zero;

        input.textViewport = areaRect;
        input.textComponent = (TextMeshProUGUI)text;
        input.placeholder = placeholder;

        return input;
    }
}






