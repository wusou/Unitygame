using UnityEngine;

/// <summary>
/// 新敌人示例：突进敌人（短时冲刺攻击）。
/// </summary>
public class DasherEnemy : EnemyBase
{
    [SerializeField] private int dashDamage = 16;
    [SerializeField] private float dashSpeedMultiplier = 2.4f;
    [SerializeField] private float dashCooldown = 2f;

    private float dashTimer;

    protected override void Update()
    {
        dashTimer -= Time.deltaTime;
        base.Update();
    }

    protected override void Patrol()
    {
        MoveHorizontal(facing * moveSpeed * 0.7f);
    }

    protected override void Attack()
    {
        if (player == null)
        {
            return;
        }

        if (dashTimer > 0f)
        {
            MoveHorizontal(0f);
            return;
        }

        var dir = player.position.x >= transform.position.x ? 1 : -1;
        MoveHorizontal(dir * moveSpeed * dashSpeedMultiplier);

        var playerHealth = player.GetComponent<PlayerHealth>();
        playerHealth?.TakeDamage(dashDamage);
        dashTimer = dashCooldown;
    }
}
