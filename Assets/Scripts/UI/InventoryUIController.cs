using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包UI控制器：显示容量、当前武器、武器列表与槽位图标。
/// 自动处理槽位布局，减少手工配置成本。
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    [Header("数据源")]
    [SerializeField] private PlayerWeaponInventory inventory;

    [Header("文本")]
    [SerializeField] private TMP_Text capacityText;
    [SerializeField] private TMP_Text currentWeaponText;
    [SerializeField] private TMP_Text weaponListText;

    [Header("槽位容器")]
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private Image slotTemplate;

    [Header("槽位外观")]
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Color normalSlotColor = Color.white;
    [SerializeField] private Color selectedSlotColor = new Color(1f, 0.86f, 0.28f, 1f);
    [SerializeField] private Color emptySlotColor = new Color(1f, 1f, 1f, 0.35f);

    [Header("槽位布局")]
    [SerializeField] private bool autoCollectSlotIcons = true;
    [SerializeField] private bool autoGenerateSlots = true;
    [SerializeField] private bool autoApplyGridLayout = true;
    [SerializeField] private Vector2 slotSize = new Vector2(64f, 64f);
    [SerializeField] private Vector2 slotSpacing = new Vector2(10f, 10f);
    [SerializeField, Min(1)] private int fixedColumnCount = 4;

    private Sprite runtimeFallbackSprite;
    private bool isSubscribed;

    private void Awake()
    {
        ResolveReferences();
        BuildSlotsIfNeeded();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BuildSlotsIfNeeded();
        SubscribeInventoryEvents();
        RefreshUI();
    }

    private void OnDisable()
    {
        UnsubscribeInventoryEvents();
    }

    /// <summary>
    /// 允许运行时补绑背包引用，避免场景引用丢失导致UI不刷新。
    /// </summary>
    public void BindInventory(PlayerWeaponInventory source)
    {
        if (source == null || inventory == source)
        {
            return;
        }

        UnsubscribeInventoryEvents();
        inventory = source;
        BuildSlotsIfNeeded();
        SubscribeInventoryEvents();
        RefreshUI();
    }

    private void ResolveReferences()
    {
        if (inventory == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                inventory = player.GetComponent<PlayerWeaponInventory>();
            }
        }

        if (capacityText == null)
        {
            capacityText = FindChildText("Capacity");
        }

        if (currentWeaponText == null)
        {
            currentWeaponText = FindChildText("Current");
        }

        if (weaponListText == null)
        {
            weaponListText = FindChildText("WeaponList");
        }

        if (slotContainer == null)
        {
            var found = transform.Find("SlotContainer");
            if (found != null)
            {
                slotContainer = found as RectTransform;
            }
        }

        if (slotContainer == null)
        {
            CreateDefaultSlotContainer();
        }
    }

    private void BuildSlotsIfNeeded()
    {
        if (slotContainer == null)
        {
            return;
        }

        if (autoApplyGridLayout)
        {
            EnsureGridLayout();
        }

        if (autoGenerateSlots)
        {
            EnsureSlotCount();
        }

        if (autoCollectSlotIcons)
        {
            CollectSlotIconsFromContainer();
        }

        if (emptySlotSprite == null)
        {
            emptySlotSprite = GetFallbackSprite();
        }
    }

    private void SubscribeInventoryEvents()
    {
        if (inventory == null || isSubscribed)
        {
            return;
        }

        inventory.InventoryChanged += OnInventoryChanged;
        inventory.CurrentWeaponChanged += OnCurrentWeaponChanged;
        isSubscribed = true;
    }

    private void UnsubscribeInventoryEvents()
    {
        if (inventory == null || !isSubscribed)
        {
            return;
        }

        inventory.InventoryChanged -= OnInventoryChanged;
        inventory.CurrentWeaponChanged -= OnCurrentWeaponChanged;
        isSubscribed = false;
    }

    private void OnInventoryChanged()
    {
        BuildSlotsIfNeeded();
        RefreshUI();
    }

    private void OnCurrentWeaponChanged(WeaponDefinition currentWeapon)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (inventory == null)
        {
            if (capacityText != null) capacityText.text = "背包: 未连接";
            if (currentWeaponText != null) currentWeaponText.text = "当前武器: 无";
            if (weaponListText != null) weaponListText.text = "";
            return;
        }

        if (autoGenerateSlots)
        {
            EnsureSlotCount();
        }

        if (autoCollectSlotIcons)
        {
            CollectSlotIconsFromContainer();
        }

        if (capacityText != null)
        {
            capacityText.text = $"背包容量: {inventory.WeaponCount}/{inventory.MaxWeaponSlots}";
        }

        if (currentWeaponText != null)
        {
            var current = inventory.CurrentWeapon;
            currentWeaponText.text = current != null
                ? $"当前武器: {current.DisplayName}"
                : "当前武器: 无";
        }

        if (weaponListText != null)
        {
            var sb = new StringBuilder();
            var max = Mathf.Max(inventory.MaxWeaponSlots, inventory.Weapons.Count);
            for (var i = 0; i < max; i++)
            {
                if (i < inventory.Weapons.Count && inventory.Weapons[i] != null)
                {
                    var weapon = inventory.Weapons[i];
                    var selectedMark = i == inventory.CurrentIndex ? "*" : " ";
                    var dropMark = !weapon.CanDrop ? " [不可丢弃]" : "";
                    sb.AppendLine($"{selectedMark}{i + 1}. {weapon.DisplayName}{dropMark}");
                }
                else
                {
                    sb.AppendLine($" {i + 1}. (空)");
                }
            }

            weaponListText.text = sb.ToString();
        }

        RefreshSlotIcons();
    }

    private void RefreshSlotIcons()
    {
        if (inventory == null)
        {
            return;
        }

        if (slotIcons == null || slotIcons.Length == 0)
        {
            CollectSlotIconsFromContainer();
            if (slotIcons == null || slotIcons.Length == 0)
            {
                return;
            }
        }

        for (var i = 0; i < slotIcons.Length; i++)
        {
            var image = slotIcons[i];
            if (image == null)
            {
                continue;
            }

            if (i < inventory.Weapons.Count && inventory.Weapons[i] != null)
            {
                var weapon = inventory.Weapons[i];
                image.sprite = weapon.Icon != null ? weapon.Icon : GetFallbackSprite();
                image.color = i == inventory.CurrentIndex ? selectedSlotColor : normalSlotColor;
            }
            else
            {
                image.sprite = emptySlotSprite != null ? emptySlotSprite : GetFallbackSprite();
                image.color = emptySlotColor;
            }

            image.preserveAspect = true;
        }
    }

    private void EnsureGridLayout()
    {
        var grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = slotContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        grid.cellSize = slotSize;
        grid.spacing = slotSpacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, fixedColumnCount);
    }

    private void EnsureSlotCount()
    {
        if (slotContainer == null)
        {
            return;
        }

        var targetCount = inventory != null ? inventory.MaxWeaponSlots : 8;
        var existing = GetDirectChildImages(slotContainer);

        for (var i = existing.Count - 1; i >= targetCount; i--)
        {
            var node = existing[i] != null ? existing[i].gameObject : null;
            if (node == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(node);
            }
            else
            {
                DestroyImmediate(node);
            }
        }

        existing = GetDirectChildImages(slotContainer);

        for (var i = existing.Count; i < targetCount; i++)
        {
            var slot = CreateSlotNode(i + 1);
            existing.Add(slot);
        }
    }

    private void CollectSlotIconsFromContainer()
    {
        if (slotContainer == null)
        {
            slotIcons = new Image[0];
            return;
        }

        var existing = GetDirectChildImages(slotContainer);
        if (existing.Count == 0)
        {
            var nested = slotContainer.GetComponentsInChildren<Image>(true);
            for (var i = 0; i < nested.Length; i++)
            {
                if (nested[i] == null || nested[i].transform == slotContainer)
                {
                    continue;
                }

                existing.Add(nested[i]);
            }
        }

        slotIcons = existing.ToArray();
    }

    private static List<Image> GetDirectChildImages(RectTransform container)
    {
        var list = new List<Image>();
        for (var i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            var image = child.GetComponent<Image>();
            if (image != null)
            {
                list.Add(image);
            }
        }

        return list;
    }

    private Image CreateSlotNode(int index)
    {
        Image img;
        if (slotTemplate != null)
        {
            img = Instantiate(slotTemplate, slotContainer);
            img.gameObject.name = $"Slot_{index:00}";
        }
        else
        {
            var go = new GameObject($"Slot_{index:00}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(slotContainer, false);
            img = go.GetComponent<Image>();
        }

        img.sprite = emptySlotSprite != null ? emptySlotSprite : GetFallbackSprite();
        img.color = emptySlotColor;
        img.preserveAspect = true;
        return img;
    }

    private TMP_Text FindChildText(string keyword)
    {
        var texts = GetComponentsInChildren<TMP_Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i].name.ToLowerInvariant().Contains(keyword.ToLowerInvariant()))
            {
                return texts[i];
            }
        }

        return null;
    }

    private void CreateDefaultSlotContainer()
    {
        var parentRect = transform as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        var go = new GameObject("SlotContainer", typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parentRect, false);

        rect.anchorMin = new Vector2(0.03f, 0.08f);
        rect.anchorMax = new Vector2(0.97f, 0.58f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        slotContainer = rect;
    }

    private Sprite GetFallbackSprite()
    {
        if (runtimeFallbackSprite != null)
        {
            return runtimeFallbackSprite;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[]
        {
            new Color(0.78f, 0.78f, 0.78f, 1f), Color.white,
            Color.white, new Color(0.78f, 0.78f, 0.78f, 1f)
        });
        texture.Apply();

        runtimeFallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
        return runtimeFallbackSprite;
    }
}
