using UnityEngine;

/// <summary>
/// 敌人技能施放上下文：技能效果从这里读取施法者和目标。
/// </summary>
public readonly struct EnemySkillContext
{
    public EnemySkillContext(
        EnemySkillController controller,
        EnemyBase caster,
        Transform target,
        EnemyState ownerState,
        float distanceToTarget)
    {
        Controller = controller;
        Caster = caster;
        Target = target;
        OwnerState = ownerState;
        DistanceToTarget = distanceToTarget;

        if (target != null)
        {
            TargetHealth = target.GetComponent<PlayerHealth>();
            TargetBody = target.GetComponent<Rigidbody2D>();
        }
        else
        {
            TargetHealth = null;
            TargetBody = null;
        }
    }

    public EnemySkillController Controller { get; }
    public EnemyBase Caster { get; }
    public Transform Target { get; }
    public PlayerHealth TargetHealth { get; }
    public Rigidbody2D TargetBody { get; }
    public EnemyState OwnerState { get; }
    public float DistanceToTarget { get; }
}
