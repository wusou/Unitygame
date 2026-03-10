using UnityEngine;

/// <summary>
/// 示例敌人技能效果：对玩家造成伤害并可附带击退。
/// </summary>
[CreateAssetMenu(menuName = "Demo/Enemies/Skill Effects/Damage Skill Effect", fileName = "EnemyDamageSkillEffect")]
public class EnemyDamageSkillEffect : EnemySkillEffect
{
    [SerializeField, Min(1)] private int damage = 12;
    [SerializeField] private bool applyKnockback = true;
    [SerializeField] private Vector2 knockbackVelocity = new Vector2(5f, 2f);

    public override void Execute(EnemySkillContext context)
    {
        if (context.TargetHealth == null)
        {
            return;
        }

        context.TargetHealth.TakeDamage(damage);

        if (!applyKnockback || context.TargetBody == null || context.Target == null || context.Caster == null)
        {
            return;
        }

        var fromCaster = context.Target.position.x - context.Caster.transform.position.x;
        var direction = fromCaster >= 0f ? 1f : -1f;

        var velocity = context.TargetBody.velocity;
        velocity.x = knockbackVelocity.x * direction;
        velocity.y = Mathf.Max(velocity.y, knockbackVelocity.y);
        context.TargetBody.velocity = velocity;
    }
}
