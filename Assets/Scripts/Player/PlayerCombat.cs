using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController), typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("武器背包")]
    [SerializeField] private PlayerWeaponInventory inventory;

    [Header("新模式初始武器（可选）")]
    [SerializeField] private bool addInitialWeaponsWhenInventoryEmpty = true;
    [SerializeField] private WeaponDefinition initialWeapon1;
    [SerializeField] private WeaponDefinition initialWeapon2;

    [Header("自动回退初始武器")]
    [SerializeField] private bool autoCreateFallbackStarterWeapons = true;
    [SerializeField] private int fallbackMeleeDamage = 16;
    [SerializeField] private float fallbackMeleeCooldown = 0.35f;
    [SerializeField] private int fallbackProjectileDamage = 11;
    [SerializeField] private float fallbackProjectileCooldown = 0.55f;

    [Header("武器切换")]
    [SerializeField] private InputActionReference switchWeaponAction;

    [Header("快捷槽位切换（1~4）")]
    [SerializeField] private InputActionReference weaponSlot1Action;
    [SerializeField] private InputActionReference weaponSlot2Action;
    [SerializeField] private InputActionReference weaponSlot3Action;
    [SerializeField] private InputActionReference weaponSlot4Action;

    [Header("丢弃武器")]
    [SerializeField] private InputActionReference dropWeaponAction;
    [SerializeField] private WeaponPickup droppedWeaponPickupPrefab;
    [SerializeField] private Transform dropSpawnPoint;
    [SerializeField] private float dropForwardDistance = 0.8f;

    [Header("攻击参数")]
    [SerializeField] private float defaultMeleeRange = 1.5f;
    [SerializeField] private GameObject fallbackProjectilePrefab;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform arrowSpawn;
    [SerializeField] private bool autoFindSpawnPointsByName = true;

    [Header("Input System")]
    [SerializeField] private InputActionReference meleeAction;
    [SerializeField] private InputActionReference bowAction;

    [HideInInspector] public int bonusDamage;

    private PlayerController controller;
    private Animator animator;
    private float attackCooldownTimer;

    private WeaponDefinition runtimeFallbackMelee;
    private WeaponDefinition runtimeFallbackProjectile;
    private Sprite runtimeMeleeIcon;
    private Sprite runtimeProjectileIcon;

    private static Sprite runtimeArrowSprite;

    public PlayerWeaponInventory Inventory => inventory;
    public WeaponDefinition CurrentWeapon => inventory != null ? inventory.CurrentWeapon : null;
    public Transform AttackPoint => attackPoint;
    public Transform ArrowSpawn => arrowSpawn;
    public float CooldownRemaining => Mathf.Max(0f, attackCooldownTimer);
    public bool IsCooldownReady => attackCooldownTimer <= 0f;
    public string LastBlockReason { get; private set; } = "未触发输入";
    public int LastAttackFrame { get; private set; } = -1;
    public string LastAttackDescription { get; private set; } = "无";

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();

        if (inventory == null)
        {
            inventory = GetComponent<PlayerWeaponInventory>();
        }

        if (inventory == null)
        {
            inventory = gameObject.AddComponent<PlayerWeaponInventory>();
        }

        ResolveSpawnPoints();
        EnsureInitialWeapons();
    }

    private void OnEnable()
    {
        meleeAction?.action?.Enable();
        bowAction?.action?.Enable();
        switchWeaponAction?.action?.Enable();
        dropWeaponAction?.action?.Enable();
        weaponSlot1Action?.action?.Enable();
        weaponSlot2Action?.action?.Enable();
        weaponSlot3Action?.action?.Enable();
        weaponSlot4Action?.action?.Enable();
    }

    private void OnDisable()
    {
        meleeAction?.action?.Disable();
        bowAction?.action?.Disable();
        switchWeaponAction?.action?.Disable();
        dropWeaponAction?.action?.Disable();
        weaponSlot1Action?.action?.Disable();
        weaponSlot2Action?.action?.Disable();
        weaponSlot3Action?.action?.Disable();
        weaponSlot4Action?.action?.Disable();
    }

    private void Update()
    {
        attackCooldownTimer -= Time.deltaTime;

        HandleQuickSlotSwitch();

        if (ReadSwitchWeaponPressed())
        {
            inventory?.SwitchNext();
        }

        if (ReadDropWeaponPressed())
        {
            TryDropCurrentWeapon();
        }

        var wantMelee = ReadMeleePressed();
        var wantBow = ReadBowPressed();
        if (!wantMelee && !wantBow)
        {
            LastBlockReason = attackCooldownTimer > 0f
                ? $"攻击冷却中: {attackCooldownTimer:0.00}s"
                : "未检测到攻击输入";
            return;
        }

        if (attackCooldownTimer > 0f)
        {
            LastBlockReason = $"攻击冷却中: {attackCooldownTimer:0.00}s";
            return;
        }

        var weapon = inventory != null ? inventory.CurrentWeapon : null;
        if (weapon == null)
        {
            EnsureInitialWeapons();
            weapon = inventory != null ? inventory.CurrentWeapon : null;
            if (weapon == null)
            {
                LastBlockReason = "背包中没有可用武器";
                return;
            }
        }

        LastAttackFrame = Time.frameCount;
        LastAttackDescription = $"{weapon.DisplayName} ({weapon.AttackMode})";
        LastBlockReason = "攻击已触发";

        AttackByDefinition(weapon);
    }

    private void ResolveSpawnPoints()
    {
        if (!autoFindSpawnPointsByName)
        {
            return;
        }

        if (attackPoint == null)
        {
            attackPoint = transform.Find("AttackPoint");
            if (attackPoint == null)
            {
                attackPoint = transform.Find("WeaponPoint");
            }
        }

        if (arrowSpawn == null)
        {
            arrowSpawn = transform.Find("ArrowSpawn");
            if (arrowSpawn == null)
            {
                arrowSpawn = transform.Find("ShootPoint");
            }
        }
    }

    private void EnsureInitialWeapons()
    {
        if (inventory == null || inventory.WeaponCount > 0)
        {
            return;
        }

        // 无论配置如何，都保证至少有可用武器，避免“角色无法攻击”。
        var first = initialWeapon1;
        var second = initialWeapon2;

        if (first == null)
        {
            runtimeFallbackMelee ??= WeaponDefinition.CreateRuntime(
                "weapon.runtime.melee",
                "训练短剑",
                WeaponAttackMode.Melee,
                fallbackMeleeDamage,
                defaultMeleeRange,
                fallbackMeleeCooldown,
                null,
                GetRuntimeMeleeIcon(),
                true);
            first = runtimeFallbackMelee;
        }

        if (second == null)
        {
            runtimeFallbackProjectile ??= WeaponDefinition.CreateRuntime(
                "weapon.runtime.bow",
                "训练木弓",
                WeaponAttackMode.Projectile,
                fallbackProjectileDamage,
                defaultMeleeRange,
                fallbackProjectileCooldown,
                fallbackProjectilePrefab,
                GetRuntimeProjectileIcon(),
                true);
            second = runtimeFallbackProjectile;
        }

        if (addInitialWeaponsWhenInventoryEmpty || autoCreateFallbackStarterWeapons || initialWeapon1 != null || initialWeapon2 != null)
        {
            inventory.TryAddWeapon(first);
            inventory.TryAddWeapon(second);
        }

        if (inventory.WeaponCount == 0)
        {
            inventory.TryAddWeapon(first);
        }
    }

    private Sprite GetRuntimeMeleeIcon()
    {
        runtimeMeleeIcon ??= CreateSolidIcon(new Color(0.9f, 0.78f, 0.22f, 1f));
        return runtimeMeleeIcon;
    }

    private Sprite GetRuntimeProjectileIcon()
    {
        runtimeProjectileIcon ??= CreateSolidIcon(new Color(0.33f, 0.82f, 0.97f, 1f));
        return runtimeProjectileIcon;
    }

    private static Sprite CreateSolidIcon(Color color)
    {
        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        var pixels = new Color[16 * 16];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
    }

    private void HandleQuickSlotSwitch()
    {
        if (inventory == null)
        {
            return;
        }

        if (TryReadSlotPressed(out var slot))
        {
            inventory.TrySelectSlot(slot);
        }
    }

    private bool TryReadSlotPressed(out int slot)
    {
        slot = 0;

        if (weaponSlot1Action != null && weaponSlot1Action.action != null && weaponSlot1Action.action.WasPressedThisFrame())
        {
            slot = 1;
            return true;
        }

        if (weaponSlot2Action != null && weaponSlot2Action.action != null && weaponSlot2Action.action.WasPressedThisFrame())
        {
            slot = 2;
            return true;
        }

        if (weaponSlot3Action != null && weaponSlot3Action.action != null && weaponSlot3Action.action.WasPressedThisFrame())
        {
            slot = 3;
            return true;
        }

        if (weaponSlot4Action != null && weaponSlot4Action.action != null && weaponSlot4Action.action.WasPressedThisFrame())
        {
            slot = 4;
            return true;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
        {
            slot = 1;
            return true;
        }

        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
        {
            slot = 2;
            return true;
        }

        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
        {
            slot = 3;
            return true;
        }

        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
        {
            slot = 4;
            return true;
        }

        return false;
    }

    private bool ReadDropWeaponPressed()
    {
        if (dropWeaponAction != null && dropWeaponAction.action != null)
        {
            return dropWeaponAction.action.WasPressedThisFrame();
        }

        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.gKey.wasPressedThisFrame;
    }

    private void TryDropCurrentWeapon()
    {
        if (inventory == null)
        {
            return;
        }

        if (!inventory.TryDropCurrent(out var dropped, out var reason))
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                Debug.Log(reason);
            }

            return;
        }

        if (dropped == null)
        {
            return;
        }

        SpawnDroppedPickup(dropped, ResolveDropPosition());
    }

    private Vector3 ResolveDropPosition()
    {
        if (dropSpawnPoint != null)
        {
            return dropSpawnPoint.position;
        }

        var dir = controller != null ? controller.Facing : 1;
        var origin = transform.position + new Vector3(0f, 0.2f, 0f);
        return origin + new Vector3(dir * dropForwardDistance, 0f, 0f);
    }

    private void SpawnDroppedPickup(WeaponDefinition droppedWeapon, Vector3 position)
    {
        WeaponPickup pickup;
        if (droppedWeaponPickupPrefab != null)
        {
            pickup = Instantiate(droppedWeaponPickupPrefab, position, Quaternion.identity);
        }
        else
        {
            var go = new GameObject($"Dropped_{droppedWeapon.name}");
            go.transform.position = position;

            var interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                go.layer = interactableLayer;
            }

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            pickup = go.AddComponent<WeaponPickup>();
        }

        pickup.SetWeapon(droppedWeapon);
        pickup.SetAutoPickup(false);
    }

    private bool ReadSwitchWeaponPressed()
    {
        if (switchWeaponAction != null && switchWeaponAction.action != null)
        {
            return switchWeaponAction.action.WasPressedThisFrame();
        }

        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.qKey.wasPressedThisFrame;
    }

    private bool ReadMeleePressed()
    {
        if (meleeAction != null && meleeAction.action != null)
        {
            return meleeAction.action.WasPressedThisFrame();
        }

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        var keyboardPressed = keyboard != null && keyboard.jKey.wasPressedThisFrame;
        var mousePressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        return keyboardPressed || mousePressed;
    }

    private bool ReadBowPressed()
    {
        if (bowAction != null && bowAction.action != null)
        {
            return bowAction.action.WasPressedThisFrame();
        }

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        var keyboardPressed = keyboard != null && keyboard.kKey.wasPressedThisFrame;
        var mousePressed = mouse != null && mouse.rightButton.wasPressedThisFrame;
        return keyboardPressed || mousePressed;
    }

    private void AttackByDefinition(WeaponDefinition weapon)
    {
        var damage = weapon.BaseDamage + bonusDamage;
        var cooldown = weapon.Cooldown;

        for (var i = 0; i < weapon.Modifiers.Count; i++)
        {
            var modifier = weapon.Modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            damage = modifier.ModifyDamage(weapon, damage);
            cooldown = modifier.ModifyCooldown(weapon, cooldown);
        }

        damage = Mathf.Max(1, damage);
        cooldown = Mathf.Max(0.05f, cooldown);

        if (weapon.AttackMode == WeaponAttackMode.Projectile)
        {
            DoProjectileAttack(weapon, damage, cooldown);
            return;
        }

        DoMeleeAttack(weapon, damage, cooldown);
    }

    private void DoMeleeAttack(WeaponDefinition weapon, int damage, float cooldown)
    {
        var point = attackPoint != null ? attackPoint : transform;

        attackCooldownTimer = cooldown;
        animator.SetTrigger("Attack");

        var range = weapon.Range > 0f ? weapon.Range : defaultMeleeRange;
        var hits = Physics2D.OverlapCircleAll(point.position, range);
        for (var i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit == null || hit.gameObject == gameObject)
            {
                continue;
            }

            var enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null)
            {
                continue;
            }

            enemy.TakeDamage(damage);
            ApplyModifiersOnHit(weapon, hit.gameObject);
        }
    }

    private void DoProjectileAttack(WeaponDefinition weapon, int damage, float cooldown)
    {
        var spawn = arrowSpawn != null ? arrowSpawn : transform;

        attackCooldownTimer = cooldown;
        animator.SetTrigger("Bow");

        var prefab = weapon.ProjectilePrefab != null ? weapon.ProjectilePrefab : fallbackProjectilePrefab;
        var arrowObject = prefab != null
            ? Instantiate(prefab, spawn.position, Quaternion.identity)
            : CreateFallbackArrow(spawn.position);

        var arrow = arrowObject.GetComponent<Arrow>();
        if (arrow != null)
        {
            arrow.Initialize(controller.Facing, damage, ProjectileOwner.Player);
        }

        var payload = arrowObject.GetComponent<ProjectileEffectPayload>();
        if (payload == null)
        {
            payload = arrowObject.AddComponent<ProjectileEffectPayload>();
        }

        payload.Initialize(gameObject, weapon, weapon.Modifiers);

        for (var i = 0; i < weapon.Modifiers.Count; i++)
        {
            weapon.Modifiers[i]?.OnProjectileSpawn(gameObject, arrowObject, weapon);
        }
    }

    private void ApplyModifiersOnHit(WeaponDefinition weapon, GameObject target)
    {
        for (var i = 0; i < weapon.Modifiers.Count; i++)
        {
            weapon.Modifiers[i]?.OnHit(gameObject, target, weapon);
        }
    }

    private static GameObject CreateFallbackArrow(Vector3 spawnPosition)
    {
        var fallback = new GameObject("RuntimePlayerArrow");
        fallback.transform.position = spawnPosition;

        var rb = fallback.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var collider = fallback.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.6f, 0.18f);

        var renderer = fallback.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFallbackArrowSprite();
        renderer.sortingOrder = 10;

        fallback.AddComponent<Arrow>();
        return fallback;
    }

    private static Sprite GetFallbackArrowSprite()
    {
        if (runtimeArrowSprite != null)
        {
            return runtimeArrowSprite;
        }

        var tex = new Texture2D(8, 2, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(1f, 0.9f, 0.35f, 1f);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        runtimeArrowSprite = Sprite.Create(tex, new Rect(0f, 0f, 8f, 2f), new Vector2(0.1f, 0.5f), 16f);
        return runtimeArrowSprite;
    }

    private void OnDrawGizmosSelected()
    {
        var point = attackPoint != null ? attackPoint : transform;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(point.position, defaultMeleeRange);
    }
}



