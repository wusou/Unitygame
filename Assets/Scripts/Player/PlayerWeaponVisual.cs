using UnityEngine;

/// <summary>
/// 玩家武器可视化：把当前武器图标显示在角色身侧。
/// </summary>
public class PlayerWeaponVisual : MonoBehaviour
{
    [SerializeField] private PlayerWeaponInventory inventory;
    [SerializeField] private PlayerController controller;

    [Header("显示")]
    [SerializeField] private Vector2 localOffset = new Vector2(0.45f, 0.08f);
    [SerializeField] private Vector3 visualScale = new Vector3(0.45f, 0.45f, 1f);
    [SerializeField] private int sortingOrderOffset = 2;
    [SerializeField] private Color fallbackColor = new Color(0.95f, 0.92f, 0.42f, 0.95f);

    private SpriteRenderer weaponRenderer;
    private Transform visualRoot;
    private Sprite fallbackSprite;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponent<PlayerWeaponInventory>();
        }

        if (controller == null)
        {
            controller = GetComponent<PlayerController>();
        }

        EnsureVisualNode();
        RefreshVisual();
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.CurrentWeaponChanged += OnCurrentWeaponChanged;
            inventory.InventoryChanged += OnInventoryChanged;
        }

        RefreshVisual();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.CurrentWeaponChanged -= OnCurrentWeaponChanged;
            inventory.InventoryChanged -= OnInventoryChanged;
        }
    }

    private void LateUpdate()
    {
        UpdatePlacement();
    }

    private void OnCurrentWeaponChanged(WeaponDefinition weapon)
    {
        RefreshVisual();
    }

    private void OnInventoryChanged()
    {
        RefreshVisual();
    }

    private void EnsureVisualNode()
    {
        var existing = transform.Find("WeaponVisual");
        if (existing == null)
        {
            var go = new GameObject("WeaponVisual");
            go.transform.SetParent(transform, false);
            existing = go.transform;
        }

        visualRoot = existing;
        weaponRenderer = existing.GetComponent<SpriteRenderer>();
        if (weaponRenderer == null)
        {
            weaponRenderer = existing.gameObject.AddComponent<SpriteRenderer>();
        }

        var playerRenderer = GetComponent<SpriteRenderer>();
        if (playerRenderer != null)
        {
            weaponRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            weaponRenderer.sortingOrder = playerRenderer.sortingOrder + sortingOrderOffset;
        }

        visualRoot.localScale = visualScale;
    }

    private void UpdatePlacement()
    {
        if (visualRoot == null || controller == null)
        {
            return;
        }

        var facing = controller.Facing >= 0 ? 1 : -1;
        visualRoot.localPosition = new Vector3(localOffset.x * facing, localOffset.y, 0f);

        var scale = visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * facing;
        visualRoot.localScale = scale;
    }

    private void RefreshVisual()
    {
        if (weaponRenderer == null || inventory == null)
        {
            return;
        }

        var weapon = inventory.CurrentWeapon;
        if (weapon == null)
        {
            weaponRenderer.enabled = false;
            return;
        }

        weaponRenderer.enabled = true;
        if (weapon.Icon != null)
        {
            weaponRenderer.sprite = weapon.Icon;
            weaponRenderer.color = Color.white;
        }
        else
        {
            weaponRenderer.sprite = GetFallbackSprite();
            weaponRenderer.color = fallbackColor;
        }

        UpdatePlacement();
    }

    private Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[]
        {
            Color.white, Color.white,
            Color.white, Color.white
        });
        texture.Apply();

        fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
        return fallbackSprite;
    }
}
