using UnityEngine;

/// <summary>
/// 新敌人示例：重装近战（体型更大、伤害更高、速度更慢）。
/// </summary>
public class BruteEnemy : EnemyBase
{
    [SerializeField] private int attackDamage = 35;
    [SerializeField] private float attackCooldown = 1.6f;

    private float attackTimer;

    protected override void Awake()
    {
        base.Awake();
        transform.localScale = new Vector3(1.35f, 1.35f, 1f);
    }

    protected override void Update()
    {
        attackTimer -= Time.deltaTime;
        base.Update();
    }

    protected override void Patrol()
    {
        MoveHorizontal(facing * moveSpeed * 0.45f);
    }

    protected override void Attack()
    {
        MoveHorizontal(0f);
        if (player == null || attackTimer > 0f)
        {
            return;
        }

        var playerHealth = player.GetComponent<PlayerHealth>();
        playerHealth?.TakeDamage(attackDamage);
        attackTimer = attackCooldown;
    }
}
