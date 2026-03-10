using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 陷阱定义：挂载一组 TrapEffect，触发时按顺序执行。
/// </summary>
[CreateAssetMenu(menuName = "Demo/World/Trap Definition", fileName = "TrapDefinition")]
public class TrapDefinition : ScriptableObject
{
    [Header("基础信息")]
    [SerializeField] private string trapId = "trap.default";
    [SerializeField] private string displayName = "Default Trap";
    [SerializeField] private string description = "Replace with your own trap design.";

    [Header("效果列表")]
    [SerializeField] private List<TrapEffect> effects = new();

    public string TrapId => trapId;
    public string DisplayName => displayName;
    public string Description => description;
    public IReadOnlyList<TrapEffect> Effects => effects;

    public void TriggerEnter(TrapContext context)
    {
        for (var i = 0; i < effects.Count; i++)
        {
            effects[i]?.OnEnter(context);
        }
    }

    public void TriggerStay(TrapContext context)
    {
        for (var i = 0; i < effects.Count; i++)
        {
            effects[i]?.OnStay(context);
        }
    }

    public void TriggerExit(TrapContext context)
    {
        for (var i = 0; i < effects.Count; i++)
        {
            effects[i]?.OnExit(context);
        }
    }
}
