using UnityEngine;
using UnityEngine.InputSystem;

public enum WeaponType
{
    Melee = 0,
    Bow = 1
}

[RequireComponent(typeof(PlayerController), typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("武器背包")]
    [SerializeField] private PlayerWeaponInventory inventory;

    [Header("兼容旧模式：武器切换")]
    [SerializeField] private WeaponType currentWeapon = WeaponType.Melee;
    [SerializeField] private InputActionReference switchWeaponAction;

    [Header("旧模式近战参数")]
    [SerializeField] private int meleeDamage = 25;
    [SerializeField] private float meleeRange = 1.5f;
    [SerializeField] private float meleeCooldown = 0.4f;
    [SerializeField] private Transform attackPoint;

    [Header("旧模式远程参数")]
    [SerializeField] private int bowDamage = 15;
    [SerializeField] private float bowCooldown = 0.6f;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawn;

    [Header("Input System")]
    [SerializeField] private InputActionReference meleeAction;
    [SerializeField] private InputActionReference bowAction;

    [HideInInspector] public int bonusDamage;

    private PlayerController controller;
    private Animator animator;
    private float attackCooldownTimer;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        if (inventory == null)
        {
            inventory = GetComponent<PlayerWeaponInventory>();
        }
    }

    private void OnEnable()
    {
        meleeAction?.action?.Enable();
        bowAction?.action?.Enable();
        switchWeaponAction?.action?.Enable();
    }

    private void OnDisable()
    {
        meleeAction?.action?.Disable();
        bowAction?.action?.Disable();
        switchWeaponAction?.action?.Disable();
    }

    private void Update()
    {
        attackCooldownTimer -= Time.deltaTime;

        if (ReadSwitchWeaponPressed())
        {
            if (inventory != null && inventory.CurrentWeapon != null)
            {
                inventory.SwitchNext();
            }
            else
            {
                ToggleLegacyWeapon();
            }
        }

        if (attackCooldownTimer > 0f)
        {
            return;
        }

        var wantMelee = ReadMeleePressed();
        var wantBow = ReadBowPressed();
        if (!wantMelee && !wantBow)
        {
            return;
        }

        var weapon = inventory != null ? inventory.CurrentWeapon : null;
        if (weapon != null)
        {
            AttackByDefinition(weapon);
            return;
        }

        if (currentWeapon == WeaponType.Melee)
        {
            LegacyMeleeAttack();
        }
        else
        {
            LegacyBowAttack();
        }
    }

    private bool ReadSwitchWeaponPressed()
    {
        if (switchWeaponAction != null && switchWeaponAction.action != null)
        {
            return switchWeaponAction.action.WasPressedThisFrame();
        }

        return Input.GetKeyDown(KeyCode.Q);
    }

    private bool ReadMeleePressed()
    {
        if (meleeAction != null && meleeAction.action != null)
        {
            return meleeAction.action.WasPressedThisFrame();
        }

        return Input.GetKeyDown(KeyCode.J);
    }

    private bool ReadBowPressed()
    {
        if (bowAction != null && bowAction.action != null)
        {
            return bowAction.action.WasPressedThisFrame();
        }

        return Input.GetKeyDown(KeyCode.K);
    }

    private void ToggleLegacyWeapon()
    {
        currentWeapon = currentWeapon == WeaponType.Melee ? WeaponType.Bow : WeaponType.Melee;
    }

    private void AttackByDefinition(WeaponDefinition weapon)
    {
        if (weapon == null)
        {
            return;
        }

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

        switch (weapon.AttackMode)
        {
            case WeaponAttackMode.Projectile:
                DoProjectileAttack(weapon, damage, cooldown);
                break;
            default:
                DoMeleeAttack(weapon, damage, cooldown);
                break;
        }
    }

    private void DoMeleeAttack(WeaponDefinition weapon, int damage, float cooldown)
    {
        if (attackPoint == null)
        {
            return;
        }

        attackCooldownTimer = cooldown;
        animator.SetTrigger("Attack");

        var range = weapon.Range > 0f ? weapon.Range : meleeRange;
        var hits = Physics2D.OverlapCircleAll(attackPoint.position, range);
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
        if (arrowSpawn == null)
        {
            return;
        }

        attackCooldownTimer = cooldown;
        animator.SetTrigger("Bow");

        var prefab = weapon.ProjectilePrefab != null ? weapon.ProjectilePrefab : arrowPrefab;
        var arrowObject = prefab != null
            ? Instantiate(prefab, arrowSpawn.position, Quaternion.identity)
            : CreateFallbackArrow(arrowSpawn.position);

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

    private void LegacyMeleeAttack()
    {
        attackCooldownTimer = meleeCooldown;
        animator.SetTrigger("Attack");

        if (attackPoint == null)
        {
            return;
        }

        var hits = Physics2D.OverlapCircleAll(attackPoint.position, meleeRange);
        for (var i = 0; i < hits.Length; i++)
        {
            var enemy = hits[i].GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(meleeDamage + bonusDamage);
            }
        }
    }

    private void LegacyBowAttack()
    {
        attackCooldownTimer = bowCooldown;
        animator.SetTrigger("Bow");

        if (arrowSpawn == null)
        {
            return;
        }

        var arrowObject = arrowPrefab != null
            ? Instantiate(arrowPrefab, arrowSpawn.position, Quaternion.identity)
            : CreateFallbackArrow(arrowSpawn.position);

        var arrow = arrowObject.GetComponent<Arrow>();
        arrow?.Initialize(controller.Facing, bowDamage + bonusDamage, ProjectileOwner.Player);
    }

    private static GameObject CreateFallbackArrow(Vector3 spawnPosition)
    {
        var fallback = new GameObject("RuntimePlayerArrow");
        fallback.transform.position = spawnPosition;

        var rb = fallback.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var collider = fallback.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        fallback.AddComponent<Arrow>();
        return fallback;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, meleeRange);
    }
}
