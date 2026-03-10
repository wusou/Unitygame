using UnityEngine;

/// <summary>
/// 陷阱扩展点：项目组成员继承本类实现新的陷阱效果。
/// </summary>
public abstract class TrapEffect : ScriptableObject
{
    public virtual void OnEnter(TrapContext context)
    {
    }

    public virtual void OnStay(TrapContext context)
    {
    }

    public virtual void OnExit(TrapContext context)
    {
    }
}
