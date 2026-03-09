using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在箭矢等投射物上，用来携带武器效果（冻结、灼烧等）。
/// </summary>
public class ProjectileEffectPayload : MonoBehaviour
{
    private GameObject owner;
    private WeaponDefinition sourceWeapon;
    private readonly List<WeaponModifier> runtimeModifiers = new();

    public void Initialize(GameObject attacker, WeaponDefinition weapon, IReadOnlyList<WeaponModifier> modifiers)
    {
        owner = attacker;
        sourceWeapon = weapon;
        runtimeModifiers.Clear();

        if (modifiers == null)
        {
            return;
        }

        for (var i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i] != null)
            {
                runtimeModifiers.Add(modifiers[i]);
            }
        }
    }

    public void ApplyTo(GameObject target)
    {
        for (var i = 0; i < runtimeModifiers.Count; i++)
        {
            runtimeModifiers[i].OnHit(owner, target, sourceWeapon);
        }
    }
}
