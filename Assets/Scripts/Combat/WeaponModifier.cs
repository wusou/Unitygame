using UnityEngine;

/// <summary>
/// 武器扩展点：项目组成员通过继承本类实现新效果。
/// </summary>
public abstract class WeaponModifier : ScriptableObject
{
    public virtual int ModifyDamage(WeaponDefinition weapon, int currentDamage)
    {
        return currentDamage;
    }

    public virtual float ModifyCooldown(WeaponDefinition weapon, float currentCooldown)
    {
        return currentCooldown;
    }

    public virtual void OnHit(GameObject attacker, GameObject target, WeaponDefinition weapon)
    {
    }

    public virtual void OnProjectileSpawn(GameObject attacker, GameObject projectile, WeaponDefinition weapon)
    {
    }
}
