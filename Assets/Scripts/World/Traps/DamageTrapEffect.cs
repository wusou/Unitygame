using UnityEngine;

/// <summary>
/// 示例陷阱效果：触发时造成伤害。
/// </summary>
[CreateAssetMenu(menuName = "Demo/World/Trap Effects/Damage Trap Effect", fileName = "DamageTrapEffect")]
public class DamageTrapEffect : TrapEffect
{
    [Header("伤害配置")]
    [SerializeField, Min(0)] private int damageOnEnter = 15;
    [SerializeField, Min(0)] private int damageOnStay = 0;
    [SerializeField, Min(0)] private int damageOnExit = 0;

    [Header("影响目标")]
    [SerializeField] private bool damagePlayer = true;
    [SerializeField] private bool damageEnemy;

    public override void OnEnter(TrapContext context)
    {
        ApplyDamage(context, damageOnEnter);
    }

    public override void OnStay(TrapContext context)
    {
        ApplyDamage(context, damageOnStay);
    }

    public override void OnExit(TrapContext context)
    {
        ApplyDamage(context, damageOnExit);
    }

    private void ApplyDamage(TrapContext context, int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        if (damagePlayer && context.TargetPlayerHealth != null)
        {
            context.TargetPlayerHealth.TakeDamage(damage);
        }

        if (damageEnemy && context.TargetEnemy != null)
        {
            context.TargetEnemy.TakeDamage(damage);
        }
    }
}
