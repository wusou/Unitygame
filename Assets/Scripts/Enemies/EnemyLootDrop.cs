using UnityEngine;

/// <summary>
/// 敌人死亡掉落组件：支持随机掉武器。
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class EnemyLootDrop : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float weaponDropChance = 0.35f;
    [SerializeField] private WeaponDefinition[] weaponCandidates;
    [SerializeField] private WeaponPickup weaponPickupPrefab;

    private EnemyBase enemy;

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
    }

    private void OnEnable()
    {
        if (enemy != null)
        {
            enemy.EnemyDied += OnEnemyDied;
        }
    }

    private void OnDisable()
    {
        if (enemy != null)
        {
            enemy.EnemyDied -= OnEnemyDied;
        }
    }

    private void OnEnemyDied(EnemyBase deadEnemy)
    {
        if (weaponPickupPrefab == null || weaponCandidates == null || weaponCandidates.Length == 0)
        {
            return;
        }

        if (Random.value > weaponDropChance)
        {
            return;
        }

        var idx = Random.Range(0, weaponCandidates.Length);
        var selected = weaponCandidates[idx];
        if (selected == null)
        {
            return;
        }

        var pickup = Instantiate(weaponPickupPrefab, deadEnemy.transform.position, Quaternion.identity);
        pickup.SetWeapon(selected);
    }
}
