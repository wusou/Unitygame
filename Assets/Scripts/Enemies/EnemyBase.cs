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
    [SerializeField] protected LayerMask groundLayer;

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

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (showHealthText)
        {
            CreateHealthTextIfNeeded();
            RefreshHealthText();
        }
    }

    protected virtual void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    protected virtual void Update()
    {
        if (state == EnemyState.Dead)
        {
            return;
        }

        if (showHealthText && hpText != null)
        {
            hpText.transform.position = transform.position + healthTextOffset;
            hpText.text = $"HP {currentHealth}/{maxHealth}";
        }

        if (player == null)
        {
            TryFindPlayer();
            Patrol();
            return;
        }

        var dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            state = EnemyState.Attack;
            Attack();
        }
        else if (dist <= detectRange)
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
        var dir = player.position.x >= transform.position.x ? 1 : -1;
        MoveHorizontal(dir * moveSpeed);
    }

    protected abstract void Patrol();
    protected abstract void Attack();

    protected virtual void Die()
    {
        state = EnemyState.Dead;
        rb.velocity = Vector2.zero;
        rb.simulated = false;
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (hpText != null)
        {
            hpText.text = "HP 0/0";
            Destroy(hpText.gameObject, 0.5f);
        }

        Destroy(gameObject, 0.75f);
    }

    protected void MoveHorizontal(float speedX)
    {
        rb.velocity = new Vector2(speedX, rb.velocity.y);
        if (Mathf.Abs(speedX) > Mathf.Epsilon)
        {
            facing = speedX > 0f ? 1 : -1;
            spriteRenderer.flipX = facing < 0;
        }
    }

    private void TryFindPlayer()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void CreateHealthTextIfNeeded()
    {
        hpText = GetComponentInChildren<TextMeshPro>();
        if (hpText != null)
        {
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
    }

    private void RefreshHealthText()
    {
        if (!showHealthText || hpText == null)
        {
            return;
        }

        hpText.text = $"HP {currentHealth}/{maxHealth}";
    }
}
