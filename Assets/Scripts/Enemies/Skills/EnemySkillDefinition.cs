using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人技能定义（可配置冷却、施法距离、生效状态和效果列表）。
/// </summary>
[CreateAssetMenu(menuName = "Demo/Enemies/Skill Definition", fileName = "EnemySkillDefinition")]
public class EnemySkillDefinition : ScriptableObject
{
    [Header("基础信息")]
    [SerializeField] private string skillId = "enemy.skill.default";
    [SerializeField] private string displayName = "Enemy Skill";

    [Header("触发条件")]
    [SerializeField, Min(0.05f)] private float cooldown = 2f;
    [SerializeField, Min(0f)] private float minDistance = 0f;
    [SerializeField, Min(0.1f)] private float maxDistance = 3f;
    [SerializeField] private bool usableInPatrol;
    [SerializeField] private bool usableInChase = true;
    [SerializeField] private bool usableInAttack = true;

    [Header("技能效果")]
    [SerializeField] private List<EnemySkillEffect> effects = new();

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public float Cooldown => cooldown;
    public IReadOnlyList<EnemySkillEffect> Effects => effects;

    public bool CanUse(EnemyState ownerState, float distanceToTarget, Transform target)
    {
        if (target == null)
        {
            return false;
        }

        var maxDist = Mathf.Max(minDistance, maxDistance);
        if (distanceToTarget < minDistance || distanceToTarget > maxDist)
        {
            return false;
        }

        return ownerState switch
        {
            EnemyState.Patrol => usableInPatrol,
            EnemyState.Chase => usableInChase,
            EnemyState.Attack => usableInAttack,
            _ => false
        };
    }

    public void Execute(EnemySkillContext context)
    {
        for (var i = 0; i < effects.Count; i++)
        {
            effects[i]?.Execute(context);
        }
    }
}
