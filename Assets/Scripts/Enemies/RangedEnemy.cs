using UnityEngine;

public class RangedEnemy : EnemyBase
{
    [Header("巡逻")]
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;

    [Header("射击")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawn;
    [SerializeField] private int shootDamage = 12;
    [SerializeField] private float shootCooldown = 1.8f;
    [SerializeField] private float keepDistance = 3f;

    private float shootTimer;

    protected override void Awake()
    {
        base.Awake();
        attackRange = Mathf.Clamp(attackRange, 4f, 12f);
    }

    protected override void Update()
    {
        shootTimer -= Time.deltaTime;
        base.Update();
    }

    protected override void Patrol()
    {
        if (leftPoint == null || rightPoint == null)
        {
            MoveHorizontal(facing * moveSpeed * 0.4f);
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

        MoveHorizontal(facing * moveSpeed * 0.8f);
    }

    protected override void Chase()
    {
        if (player == null)
        {
            return;
        }

        var dirToPlayer = Mathf.Sign(player.position.x - transform.position.x);
        var dist = Mathf.Abs(player.position.x - transform.position.x);

        if (dist < keepDistance)
        {
            MoveHorizontal(-dirToPlayer * moveSpeed);
        }
        else if (dist > keepDistance + 1f)
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
        if (player == null || arrowSpawn == null)
        {
            return;
        }

        facing = player.position.x >= transform.position.x ? 1 : -1;
        spriteRenderer.flipX = facing < 0;

        if (shootTimer > 0f)
        {
            return;
        }

        var arrowObject = arrowPrefab != null
            ? Instantiate(arrowPrefab, arrowSpawn.position, Quaternion.identity)
            : CreateFallbackArrow(arrowSpawn.position);

        var arrow = arrowObject.GetComponent<Arrow>();
        if (arrow != null)
        {
            arrow.Initialize(facing, shootDamage, ProjectileOwner.Enemy);
        }

        shootTimer = shootCooldown;
    }

    private GameObject CreateFallbackArrow(Vector3 spawnPosition)
    {
        var fallback = new GameObject("RuntimeEnemyArrow");
        fallback.transform.position = spawnPosition;

        var rb = fallback.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var collider = fallback.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        fallback.AddComponent<Arrow>();
        return fallback;
    }
}
