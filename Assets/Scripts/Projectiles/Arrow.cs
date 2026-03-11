using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Arrow : MonoBehaviour
{
    [Header("基础")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float destroyDelayOnHit = 0.02f;

    [Header("轨迹")]
    [SerializeField] private bool enableTrail = true;
    [SerializeField] private float trailTime = 0.15f;
    [SerializeField] private float trailStartWidth = 0.08f;
    [SerializeField] private float trailEndWidth = 0.02f;
    [SerializeField] private Color trailStartColor = new Color(1f, 0.95f, 0.6f, 0.9f);
    [SerializeField] private Color trailEndColor = new Color(1f, 0.8f, 0.2f, 0f);

    private int damage;
    private int direction = 1;
    private ProjectileOwner owner;
    private bool hasHit;

    private Collider2D arrowCollider;
    private Rigidbody2D rb;
    private TrailRenderer trail;

    public void Initialize(int facingDirection, int dmg, ProjectileOwner projectileOwner)
    {
        direction = facingDirection >= 0 ? 1 : -1;
        damage = dmg;
        owner = projectileOwner;

        // 让箭头朝向与移动方向一致。
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;

        Destroy(gameObject, lifetime);
    }

    private void Awake()
    {
        arrowCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        if (enableTrail)
        {
            EnsureTrail();
        }
    }

    private void Update()
    {
        if (hasHit)
        {
            return;
        }

        transform.Translate(Vector2.right * direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || other == null)
        {
            return;
        }

        if (IsLadderCollider(other))
        {
            return;
        }

        if (owner == ProjectileOwner.Player && other.CompareTag("Player"))
        {
            return;
        }

        if (owner == ProjectileOwner.Enemy && other.CompareTag("Enemy"))
        {
            return;
        }

        var isPlayerTarget = other.CompareTag("Player");
        var isEnemyTarget = other.CompareTag("Enemy");

        if (isPlayerTarget)
        {
            var playerHealth = other.GetComponent<PlayerHealth>();
            playerHealth?.TakeDamage(damage);
        }
        else if (isEnemyTarget)
        {
            var enemy = other.GetComponent<EnemyBase>();
            enemy?.TakeDamage(damage);
        }
        else if (other.isTrigger)
        {
            // 交互触发器（梯子、门等）不应阻挡投射物。
            return;
        }

        var payload = GetComponent<ProjectileEffectPayload>();
        payload?.ApplyTo(other.gameObject);

        ImpactAndDestroy();
    }

    private static bool IsLadderCollider(Collider2D other)
    {
        return other.GetComponent<LadderAuthoring>() != null ||
               other.GetComponentInParent<LadderAuthoring>() != null;
    }

    private void ImpactAndDestroy()
    {
        hasHit = true;

        if (arrowCollider != null)
        {
            arrowCollider.enabled = false;
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }

        if (trail != null)
        {
            trail.emitting = false;
        }

        Destroy(gameObject, destroyDelayOnHit);
    }

    private void EnsureTrail()
    {
        trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
        }

        trail.time = trailTime;
        trail.startWidth = trailStartWidth;
        trail.endWidth = trailEndWidth;
        trail.startColor = trailStartColor;
        trail.endColor = trailEndColor;
        trail.minVertexDistance = 0.02f;

        if (trail.material == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                trail.material = new Material(shader);
            }
        }
    }
}

