using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人死亡掉落组件：每个掉落物独立概率，并支持“什么都不掉落”的概率。
/// 注意：本脚本仅保留新版逻辑，不再兼容旧版字段。
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class EnemyLootDrop : MonoBehaviour
{
    [System.Serializable]
    private class WeaponDropEntry
    {
        public WeaponDefinition weapon = null;

        [Min(0f)]
        public float probability = 0.2f;
    }

    [Header("掉落拾取预制体（必须）")]
    [SerializeField] private WeaponPickup weaponPickupPrefab;

    [Header("概率配置")]
    [SerializeField, Min(0f)] private float noneDropProbability = 0.35f;
    [SerializeField] private WeaponDropEntry[] weaponDropEntries;
    [SerializeField] private bool stopRollingWhenNoneDropped = true;

    [Header("掉落数量")]
    [SerializeField, Min(1)] private int minDropCount = 1;
    [SerializeField, Min(1)] private int maxDropCount = 1;
    [SerializeField] private bool avoidDuplicateInOneDeath = true;

    [Header("掉落范围")]
    [SerializeField] private Vector2 dropOffsetXRange = new Vector2(-0.6f, 0.6f);
    [SerializeField] private Vector2 dropOffsetYRange = new Vector2(0f, 0.35f);

    [Header("掉落拾取行为")]
    [SerializeField] private bool autoPickupDroppedWeapon;

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
        if (weaponPickupPrefab == null || deadEnemy == null)
        {
            return;
        }

        if (!HasDropTableConfigured())
        {
            return;
        }

        DropByTable(deadEnemy);
    }

    private bool HasDropTableConfigured()
    {
        if (weaponDropEntries == null || weaponDropEntries.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < weaponDropEntries.Length; i++)
        {
            var entry = weaponDropEntries[i];
            if (entry != null && entry.weapon != null && entry.probability > 0f)
            {
                return true;
            }
        }

        return false;
    }

    private void DropByTable(EnemyBase deadEnemy)
    {
        var countMin = Mathf.Max(1, Mathf.Min(minDropCount, maxDropCount));
        var countMax = Mathf.Max(countMin, Mathf.Max(minDropCount, maxDropCount));
        var dropCount = Random.Range(countMin, countMax + 1);

        var usedEntryIndices = new List<int>();

        for (var i = 0; i < dropCount; i++)
        {
            if (!TryPickWeapon(usedEntryIndices, out var selectedWeapon, out var selectedEntryIndex))
            {
                if (stopRollingWhenNoneDropped)
                {
                    break;
                }

                continue;
            }

            SpawnPickup(deadEnemy, selectedWeapon);

            if (avoidDuplicateInOneDeath && selectedEntryIndex >= 0)
            {
                usedEntryIndices.Add(selectedEntryIndex);
            }
        }
    }

    private bool TryPickWeapon(List<int> usedEntryIndices, out WeaponDefinition selectedWeapon, out int selectedEntryIndex)
    {
        selectedWeapon = null;
        selectedEntryIndex = -1;

        var totalProbability = Mathf.Max(0f, noneDropProbability);

        for (var i = 0; i < weaponDropEntries.Length; i++)
        {
            var entry = weaponDropEntries[i];
            if (entry == null || entry.weapon == null || entry.probability <= 0f)
            {
                continue;
            }

            if (avoidDuplicateInOneDeath && usedEntryIndices.Contains(i))
            {
                continue;
            }

            totalProbability += entry.probability;
        }

        if (totalProbability <= 0f)
        {
            return false;
        }

        var roll = Random.value * totalProbability;

        if (roll < noneDropProbability)
        {
            return false;
        }

        roll -= noneDropProbability;

        for (var i = 0; i < weaponDropEntries.Length; i++)
        {
            var entry = weaponDropEntries[i];
            if (entry == null || entry.weapon == null || entry.probability <= 0f)
            {
                continue;
            }

            if (avoidDuplicateInOneDeath && usedEntryIndices.Contains(i))
            {
                continue;
            }

            if (roll < entry.probability)
            {
                selectedWeapon = entry.weapon;
                selectedEntryIndex = i;
                return true;
            }

            roll -= entry.probability;
        }

        return false;
    }

    private void SpawnPickup(EnemyBase deadEnemy, WeaponDefinition weapon)
    {
        if (weapon == null)
        {
            return;
        }

        var spawnPos = deadEnemy.transform.position + new Vector3(
            Random.Range(dropOffsetXRange.x, dropOffsetXRange.y),
            Random.Range(dropOffsetYRange.x, dropOffsetYRange.y),
            0f);

        var pickup = Instantiate(weaponPickupPrefab, spawnPos, Quaternion.identity);
        pickup.SetWeapon(weapon);
        pickup.SetAutoPickup(autoPickupDroppedWeapon);
    }
}
