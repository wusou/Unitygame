using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 5f;

    private int damage;
    private int direction = 1;
    private ProjectileOwner owner;

    public void Initialize(int facingDirection, int dmg, ProjectileOwner projectileOwner)
    {
        direction = facingDirection >= 0 ? 1 : -1;
        damage = dmg;
        owner = projectileOwner;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == ProjectileOwner.Player && other.CompareTag("Player"))
        {
            return;
        }

        if (owner == ProjectileOwner.Enemy && other.CompareTag("Enemy"))
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            var playerHealth = other.GetComponent<PlayerHealth>();
            playerHealth?.TakeDamage(damage);
        }
        else if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            enemy?.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
