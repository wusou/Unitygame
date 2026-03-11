using UnityEngine;

/// <summary>
/// 统一武器敌人：同一类支持近战/远程，攻击逻辑完全由 WeaponDefinition 驱动。
/// </summary>
public class EnemyController : EnemyBase
{
    [Header("巡逻")]
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;
    [SerializeField] private float patrolSpeedMultiplier = 0.8f;

    [Header("武器")]
    [SerializeField] private WeaponDefinition weaponDefinition;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform arrowSpawn;
    [SerializeField] private GameObject fallbackProjectilePrefab;
    [SerializeField] private float defaultMeleeRange = 1.5f;

    [Header("远程追击")]
    [SerializeField] private bool useRangedKeepDistance = true;
    [SerializeField] private float keepDistance = 3f;
    [SerializeField] private float keepDistanceTolerance = 1f;

    [Header("动画（可选）")]
    [SerializeField] private Animator animator;
    [SerializeField] private string meleeTrigger = "Attack";
    [SerializeField] private string projectileTrigger = "Bow";

    private float attackTimer;
    private bool warnedMissingWeapon;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    protected override void Update()
    {
        attackTimer -= Time.deltaTime;
        base.Update();
    }

    protected override void Patrol()
    {
        var patrolSpeed = moveSpeed * Mathf.Max(0.1f, patrolSpeedMultiplier);

        if (leftPoint == null || rightPoint == null)
        {
            MoveHorizontal(facing * patrolSpeed);
            return;
        }

        if (transform.position.x <= leftPoint.position.x)
        {
            facing = 1;
        }
        else if (transform.position.x >= rightPoint.position.x)
        {
            facing = -1;
        }

        MoveHorizontal(facing * patrolSpeed);
    }

    protected override void Chase()
    {
        if (!ShouldUseRangedKiting() || player == null)
        {
            base.Chase();
            return;
        }

        var dirToPlayer = Mathf.Sign(player.position.x - transform.position.x);
        var dist = Mathf.Abs(player.position.x - transform.position.x);

        var stopBand = Mathf.Max(0.1f, keepDistanceTolerance);
        if (dist < keepDistance - stopBand)
        {
            MoveHorizontal(-dirToPlayer * moveSpeed);
        }
        else if (dist > keepDistance + stopBand)
        {
            MoveHorizontal(dirToPlayer * moveSpeed);
        }
        else
        {
            MoveHorizontal(0f);
            facing = dirToPlayer >= 0f ? 1 : -1;
            spriteRenderer.flipX = facing < 0;
        }
    }

    protected override void Attack()
    {
        MoveHorizontal(0f);

        if (player == null || attackTimer > 0f)
        {
            return;
        }

        if (weaponDefinition == null)
        {
            if (!warnedMissingWeapon)
            {
                warnedMissingWeapon = true;
                Debug.LogWarning($"{name} 未配置 WeaponDefinition，无法攻击。", this);
            }

            return;
        }

        facing = player.position.x >= transform.position.x ? 1 : -1;
        spriteRenderer.flipX = facing < 0;

        var damage = weaponDefinition.BaseDamage;
        var cooldown = weaponDefinition.Cooldown;

        for (var i = 0; i < weaponDefinition.Modifiers.Count; i++)
        {
            var modifier = weaponDefinition.Modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            damage = modifier.ModifyDamage(weaponDefinition, damage);
            cooldown = modifier.ModifyCooldown(weaponDefinition, cooldown);
        }

        damage = Mathf.Max(1, damage);
        cooldown = Mathf.Max(0.05f, cooldown);

        if (weaponDefinition.AttackMode == WeaponAttackMode.Projectile)
        {
            DoProjectileAttack(damage);
            TriggerAttackAnimation(projectileTrigger);
        }
        else
        {
            DoMeleeAttack(damage);
            TriggerAttackAnimation(meleeTrigger);
        }

        attackTimer = cooldown;
    }

    private bool ShouldUseRangedKiting()
    {
        return useRangedKeepDistance &&
               weaponDefinition != null &&
               weaponDefinition.AttackMode == WeaponAttackMode.Projectile;
    }

    private void DoMeleeAttack(int damage)
    {
        var point = attackPoint != null ? attackPoint : transform;
        var range = weaponDefinition != null && weaponDefinition.Range > 0f
            ? weaponDefinition.Range
            : defaultMeleeRange;

        var hits = Physics2D.OverlapCircleAll(point.position, Mathf.Max(0.1f, range));
        for (var i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit == null || hit.gameObject == gameObject || !hit.CompareTag("Player"))
            {
                continue;
            }

            var playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                continue;
            }

            playerHealth.TakeDamage(damage);
            ApplyModifiersOnHit(hit.gameObject);
            return;
        }
    }

    private void DoProjectileAttack(int damage)
    {
        var spawn = arrowSpawn != null ? arrowSpawn : transform;

        var projectilePrefab = weaponDefinition != null && weaponDefinition.ProjectilePrefab != null
            ? weaponDefinition.ProjectilePrefab
            : fallbackProjectilePrefab;

        var projectileObject = projectilePrefab != null
            ? Instantiate(projectilePrefab, spawn.position, Quaternion.identity)
            : CreateFallbackArrow(spawn.position);

        var arrow = projectileObject.GetComponent<Arrow>();
        if (arrow != null)
        {
            arrow.Initialize(facing, damage, ProjectileOwner.Enemy);
        }

        var payload = projectileObject.GetComponent<ProjectileEffectPayload>();
        if (payload == null)
        {
            payload = projectileObject.AddComponent<ProjectileEffectPayload>();
        }

        payload.Initialize(gameObject, weaponDefinition, weaponDefinition.Modifiers);

        for (var i = 0; i < weaponDefinition.Modifiers.Count; i++)
        {
            weaponDefinition.Modifiers[i]?.OnProjectileSpawn(gameObject, projectileObject, weaponDefinition);
        }
    }

    private void ApplyModifiersOnHit(GameObject target)
    {
        if (weaponDefinition == null)
        {
            return;
        }

        for (var i = 0; i < weaponDefinition.Modifiers.Count; i++)
        {
            weaponDefinition.Modifiers[i]?.OnHit(gameObject, target, weaponDefinition);
        }
    }

    private static GameObject CreateFallbackArrow(Vector3 spawnPosition)
    {
        var fallback = new GameObject("RuntimeEnemyArrow");
        fallback.transform.position = spawnPosition;

        var rb = fallback.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var collider = fallback.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.6f, 0.18f);

        fallback.AddComponent<Arrow>();
        return fallback;
    }

    private void TriggerAttackAnimation(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
        {
            return;
        }

        animator.SetTrigger(triggerName);
    }

    private void OnDrawGizmosSelected()
    {
        var point = attackPoint != null ? attackPoint : transform;
        var range = weaponDefinition != null && weaponDefinition.Range > 0f
            ? weaponDefinition.Range
            : defaultMeleeRange;

        Gizmos.color = weaponDefinition != null && weaponDefinition.AttackMode == WeaponAttackMode.Projectile
            ? Color.cyan
            : Color.red;

        Gizmos.DrawWireSphere(point.position, Mathf.Max(0.1f, range));
    }
}
