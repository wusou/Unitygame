using UnityEngine;

public class MeleeEnemy : EnemyBase
{
    [Header("巡逻")]
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;

    [Header("近战")]
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float attackCooldown = 1.2f;

    private float attackTimer;

    protected override void Update()
    {
        attackTimer -= Time.deltaTime;
        base.Update();
    }

    protected override void Patrol()
    {
        if (leftPoint == null || rightPoint == null)
        {
            MoveHorizontal(facing * moveSpeed * 0.5f);
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

        MoveHorizontal(facing * moveSpeed);
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
