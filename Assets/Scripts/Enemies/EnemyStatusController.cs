using UnityEngine;

/// <summary>
/// 敌人状态控制：处理冻结减速、燃烧持续伤害。
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class EnemyStatusController : MonoBehaviour
{
    [SerializeField] private float burnTickInterval = 0.5f;

    private EnemyBase enemy;

    private float freezeTimer;
    private float freezeMoveMultiplier = 1f;

    private float burnTimer;
    private float burnDps;
    private float burnTickTimer;

    public float MovementMultiplier => freezeTimer > 0f ? freezeMoveMultiplier : 1f;

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
    }

    private void Update()
    {
        UpdateFreeze();
        UpdateBurn();
    }

    public void ApplyFreeze(float duration, float moveMultiplier)
    {
        if (duration <= 0f)
        {
            return;
        }

        freezeTimer = Mathf.Max(freezeTimer, duration);
        freezeMoveMultiplier = Mathf.Clamp(moveMultiplier, 0.05f, 1f);
    }

    public void ApplyBurn(float duration, float dps)
    {
        if (duration <= 0f || dps <= 0f)
        {
            return;
        }

        burnTimer = Mathf.Max(burnTimer, duration);
        burnDps = Mathf.Max(burnDps, dps);
    }

    private void UpdateFreeze()
    {
        if (freezeTimer <= 0f)
        {
            return;
        }

        freezeTimer -= Time.deltaTime;
        if (freezeTimer <= 0f)
        {
            freezeMoveMultiplier = 1f;
        }
    }

    private void UpdateBurn()
    {
        if (burnTimer <= 0f || enemy == null)
        {
            return;
        }

        burnTimer -= Time.deltaTime;
        burnTickTimer -= Time.deltaTime;

        if (burnTickTimer > 0f)
        {
            return;
        }

        burnTickTimer = burnTickInterval;
        var damage = Mathf.Max(1, Mathf.RoundToInt(burnDps * burnTickInterval));
        enemy.TakeDamage(damage);

        if (burnTimer <= 0f)
        {
            burnDps = 0f;
        }
    }
}
