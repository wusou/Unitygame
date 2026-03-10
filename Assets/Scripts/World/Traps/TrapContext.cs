using UnityEngine;

/// <summary>
/// 陷阱效果上下文：效果脚本可从这里获取目标和触发器信息。
/// </summary>
public readonly struct TrapContext
{
    public TrapContext(
        TrapZone zone,
        GameObject targetObject,
        Collider2D targetCollider,
        PlayerHealth targetPlayerHealth,
        EnemyBase targetEnemy,
        float deltaTime)
    {
        Zone = zone;
        TrapObject = zone != null ? zone.gameObject : null;
        TargetObject = targetObject;
        TargetCollider = targetCollider;
        TargetPlayerHealth = targetPlayerHealth;
        TargetEnemy = targetEnemy;
        DeltaTime = deltaTime;
    }

    public TrapZone Zone { get; }
    public GameObject TrapObject { get; }
    public GameObject TargetObject { get; }
    public Collider2D TargetCollider { get; }
    public PlayerHealth TargetPlayerHealth { get; }
    public EnemyBase TargetEnemy { get; }
    public float DeltaTime { get; }

    public bool HasPlayerTarget => TargetPlayerHealth != null;
    public bool HasEnemyTarget => TargetEnemy != null;
}
