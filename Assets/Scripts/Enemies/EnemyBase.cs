using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("通用属性")]
    [SerializeField] protected int maxHealth = 60;
    [SerializeField] protected float moveSpeed = 2.2f;
    [SerializeField] protected float detectRange = 6f;
    [SerializeField] protected float attackRange = 1.2f;

    [Header("游走与追击限制")]
    [SerializeField] private float patrolHalfWidth = 4f;
    [SerializeField] private bool useChaseLeash = true;
    [SerializeField] private float chaseLeashDistance = 12f;

    [Header("血量显示")]
    [SerializeField] private bool showHealthText = true;
    [SerializeField] private Vector3 healthTextOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float healthTextScale = 0.18f;

    protected int currentHealth;
    protected EnemyState state = EnemyState.Patrol;
    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected int facing = -1;

    private TextMeshPro hpText;
    private MeshRenderer hpTextRenderer;
    private Vector3 homePosition;
    private EnemyStatusController statusController;

    public event Action<EnemyBase> EnemyDied;

    public Vector3 HomePosition => homePosition;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        statusController = GetComponent<EnemyStatusController>();
        if (statusController == null)
        {
            statusController = gameObject.AddComponent<EnemyStatusController>();
        }

        homePosition = transform.position;

        if (showHealthText)
        {
            CreateHealthTextIfNeeded();
            RefreshHealthText();
        }
    }

    protected virtual void Start()
    {
        TryFindPlayer();
    }

    protected virtual void Update()
    {
        if (state == EnemyState.Dead)
        {
            return;
        }

        UpdateHealthLabel();

        if (player == null)
        {
            TryFindPlayer();
            Patrol();
            return;
        }

        var distToPlayer = Vector2.Distance(transform.position, player.position);
        var distFromHome = Mathf.Abs(transform.position.x - homePosition.x);
        var canChase = !useChaseLeash || distFromHome <= chaseLeashDistance;

        if (distToPlayer <= attackRange && canChase)
        {
            state = EnemyState.Attack;
            Attack();
        }
        else if (distToPlayer <= detectRange && canChase)
        {
            state = EnemyState.Chase;
            Chase();
        }
        else
        {
            state = EnemyState.Patrol;
            Patrol();
        }
    }

    public void TakeDamage(int amount)
    {
        if (state == EnemyState.Dead || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        RefreshHealthText();

        if (currentHealth == 0)
        {
            Die();
        }
    }

    protected virtual void Chase()
    {
        if (player == null)
        {
            return;
        }

        var dir = player.position.x >= transform.position.x ? 1 : -1;
        MoveHorizontal(dir * moveSpeed);
    }

    protected abstract void Patrol();
    protected abstract void Attack();

    protected virtual void Die()
    {
        if (state == EnemyState.Dead)
        {
            return;
        }

        state = EnemyState.Dead;
        rb.velocity = Vector2.zero;
        rb.simulated = false;

        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        EnemyDied?.Invoke(this);

        if (hpText != null)
        {
            hpText.text = "HP 0/0";
            Destroy(hpText.gameObject, 0.5f);
        }

        Destroy(gameObject, 0.75f);
    }

    protected void MoveHorizontal(float speedX)
    {
        speedX = ApplyPatrolLimit(speedX);

        if (statusController != null)
        {
            speedX *= statusController.MovementMultiplier;
        }

        rb.velocity = new Vector2(speedX, rb.velocity.y);

        if (Mathf.Abs(speedX) > Mathf.Epsilon)
        {
            facing = speedX > 0f ? 1 : -1;
            spriteRenderer.flipX = facing < 0;
        }
    }

    private float ApplyPatrolLimit(float speedX)
    {
        // 追击时可越过巡逻范围，但不能超出追击绳距。
        if (state == EnemyState.Chase && useChaseLeash)
        {
            var fromHome = transform.position.x - homePosition.x;
            if ((fromHome >= chaseLeashDistance && speedX > 0f) ||
                (fromHome <= -chaseLeashDistance && speedX < 0f))
            {
                return 0f;
            }

            return speedX;
        }

        var dist = transform.position.x - homePosition.x;
        if ((dist >= patrolHalfWidth && speedX > 0f) ||
            (dist <= -patrolHalfWidth && speedX < 0f))
        {
            return -speedX;
        }

        return speedX;
    }

    private void TryFindPlayer()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void UpdateHealthLabel()
    {
        if (!showHealthText || hpText == null)
        {
            return;
        }

        hpText.transform.position = transform.position + healthTextOffset;
        hpText.text = $"HP {currentHealth}/{maxHealth}";
        SyncHealthTextSorting();
    }

    private void CreateHealthTextIfNeeded()
    {
        hpText = GetComponentInChildren<TextMeshPro>();
        if (hpText != null)
        {
            hpTextRenderer = hpText.GetComponent<MeshRenderer>();
            SyncHealthTextSorting();
            return;
        }

        var go = new GameObject("EnemyHpText");
        go.transform.SetParent(transform);
        go.transform.localPosition = healthTextOffset;
        go.transform.localScale = Vector3.one * healthTextScale;

        hpText = go.AddComponent<TextMeshPro>();
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.fontSize = 24;
        hpText.color = Color.white;
        hpText.outlineWidth = 0.2f;
        hpText.enableWordWrapping = false;
        hpTextRenderer = hpText.GetComponent<MeshRenderer>();
        SyncHealthTextSorting();
    }

    private void RefreshHealthText()
    {
        if (!showHealthText || hpText == null)
        {
            return;
        }

        hpText.text = $"HP {currentHealth}/{maxHealth}";
    }

    private void SyncHealthTextSorting()
    {
        if (hpTextRenderer == null || spriteRenderer == null)
        {
            return;
        }

        hpTextRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        hpTextRenderer.sortingOrder = spriteRenderer.sortingOrder + 10;
    }
}
