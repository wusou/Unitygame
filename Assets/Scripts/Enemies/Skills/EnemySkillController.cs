using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人技能控制器：负责按优先顺序施放技能并处理冷却。
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class EnemySkillController : MonoBehaviour
{
    [SerializeField] private List<EnemySkillDefinition> skillDefinitions = new();
    [SerializeField, Min(0f)] private float globalCooldown = 0.2f;
    [SerializeField] private bool stopHorizontalVelocityOnCast = true;

    private EnemyBase enemy;
    private Rigidbody2D body;

    private readonly Dictionary<EnemySkillDefinition, float> skillReadyTimes = new();
    private float nextGlobalReadyTime;

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        body = GetComponent<Rigidbody2D>();
    }

    public bool TryCast(Transform target, float distanceToTarget, EnemyState ownerState)
    {
        if (enemy == null || target == null || skillDefinitions == null || skillDefinitions.Count == 0)
        {
            return false;
        }

        var now = Time.time;
        if (now < nextGlobalReadyTime)
        {
            return false;
        }

        for (var i = 0; i < skillDefinitions.Count; i++)
        {
            var skill = skillDefinitions[i];
            if (skill == null)
            {
                continue;
            }

            if (!skill.CanUse(ownerState, distanceToTarget, target))
            {
                continue;
            }

            if (skillReadyTimes.TryGetValue(skill, out var readyTime) && now < readyTime)
            {
                continue;
            }

            if (stopHorizontalVelocityOnCast && body != null)
            {
                body.velocity = new Vector2(0f, body.velocity.y);
            }

            var context = new EnemySkillContext(this, enemy, target, ownerState, distanceToTarget);
            skill.Execute(context);

            skillReadyTimes[skill] = now + Mathf.Max(0.05f, skill.Cooldown);
            nextGlobalReadyTime = now + Mathf.Max(0f, globalCooldown);
            return true;
        }

        return false;
    }
}
