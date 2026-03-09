using UnityEngine;

/// <summary>
/// 武器拾取物：支持触发自动拾取，也支持F交互拾取。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private WeaponDefinition weaponDefinition;
    [SerializeField] private bool autoPickupOnTrigger = true;
    [SerializeField] private string interactionTitle = "拾取武器";

    [Header("显示")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private bool autoCreateIconRenderer = true;
    [SerializeField] private Sprite fallbackIcon;
    [SerializeField] private Color fallbackColor = new Color(0.95f, 0.88f, 0.24f, 1f);

    private Collider2D pickupCollider;
    private static Sprite runtimeFallbackIcon;

    public string InteractionTitle => interactionTitle;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider != null)
        {
            // 拾取物不应阻挡角色。
            pickupCollider.isTrigger = true;
        }

        EnsureIconRenderer();
        RefreshVisual();
    }

    public void SetWeapon(WeaponDefinition definition)
    {
        weaponDefinition = definition;
        RefreshVisual();
    }

    public void SetAutoPickup(bool enabled)
    {
        autoPickupOnTrigger = enabled;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        var inventory = interactor.GetPlayerComponent<PlayerWeaponInventory>();
        return inventory != null && weaponDefinition != null;
    }

    public void Interact(PlayerInteractor interactor)
    {
        TryPickup(interactor.GetPlayerComponent<PlayerWeaponInventory>());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoPickupOnTrigger || !other.CompareTag("Player"))
        {
            return;
        }

        var inventory = ResolveInventory(other.gameObject);
        TryPickup(inventory);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!autoPickupOnTrigger || collision == null)
        {
            return;
        }

        var other = collision.collider;
        if (other == null || !other.CompareTag("Player"))
        {
            return;
        }

        var inventory = ResolveInventory(other.gameObject);
        TryPickup(inventory);
    }

    private static PlayerWeaponInventory ResolveInventory(GameObject source)
    {
        if (source == null)
        {
            return null;
        }

        var inventory = source.GetComponent<PlayerWeaponInventory>();
        if (inventory != null)
        {
            return inventory;
        }

        return source.GetComponentInParent<PlayerWeaponInventory>();
    }

    private void TryPickup(PlayerWeaponInventory inventory)
    {
        if (inventory == null)
        {
            Debug.LogWarning($"无法拾取 {name}: 未找到玩家背包组件 PlayerWeaponInventory。");
            return;
        }

        if (weaponDefinition == null)
        {
            Debug.LogWarning($"无法拾取 {name}: weaponDefinition 为空。");
            return;
        }

        if (!inventory.TryAddWeapon(weaponDefinition, out var failReason))
        {
            Debug.Log($"无法拾取 {weaponDefinition.DisplayName}: {failReason}");
            return;
        }

        Destroy(gameObject);
    }

    private void EnsureIconRenderer()
    {
        if (iconRenderer != null)
        {
            return;
        }

        iconRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (iconRenderer != null || !autoCreateIconRenderer)
        {
            return;
        }

        var go = new GameObject("Icon");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        go.transform.localScale = Vector3.one * 0.6f;

        iconRenderer = go.AddComponent<SpriteRenderer>();
        iconRenderer.sortingOrder = 12;
    }

    private void RefreshVisual()
    {
        EnsureIconRenderer();
        if (iconRenderer == null)
        {
            return;
        }

        var icon = weaponDefinition != null ? weaponDefinition.Icon : null;
        if (icon != null)
        {
            iconRenderer.sprite = icon;
            iconRenderer.color = Color.white;
            return;
        }

        iconRenderer.sprite = fallbackIcon != null ? fallbackIcon : GetFallbackIcon();
        iconRenderer.color = fallbackColor;
    }

    private static Sprite GetFallbackIcon()
    {
        if (runtimeFallbackIcon != null)
        {
            return runtimeFallbackIcon;
        }

        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        var pixels = new Color[16 * 16];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0.95f, 0.88f, 0.24f, 1f);
        }

        tex.SetPixels(pixels);
        tex.Apply();

        runtimeFallbackIcon = Sprite.Create(tex, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
        return runtimeFallbackIcon;
    }
}
