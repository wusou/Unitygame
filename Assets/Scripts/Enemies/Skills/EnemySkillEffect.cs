using UnityEngine;

/// <summary>
/// 敌人技能扩展点：项目组成员通过继承实现新技能效果。
/// </summary>
public abstract class EnemySkillEffect : ScriptableObject
{
    public virtual void Execute(EnemySkillContext context)
    {
    }
}
