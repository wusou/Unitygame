using UnityEngine;

/// <summary>
/// 击退效果示例：命中后把敌人向远离攻击者方向击退，并附带短硬直。
/// </summary>
[CreateAssetMenu(menuName = "Demo/Combat/Modifiers/Knockback", fileName = "KnockbackModifier")]
public class KnockbackWeaponModifier : WeaponModifier
{
    [SerializeField] private float horizontalVelocity = 6f;
    [SerializeField] private float verticalVelocity = 1.2f;
    [SerializeField] private float stunDuration = 0.18f;
    [SerializeField] private bool useAttackerDirection = true;
    [SerializeField] private int fixedDirection = 1;

    public override void OnHit(GameObject attacker, GameObject target, WeaponDefinition weapon)
    {
        if (target == null)
        {
            return;
        }

        var enemy = target.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            return;
        }

        var dir = ResolveDirection(attacker, target);
        var velocity = new Vector2(Mathf.Abs(horizontalVelocity) * dir, verticalVelocity);
        enemy.ApplyKnockback(velocity, stunDuration);
    }

    private int ResolveDirection(GameObject attacker, GameObject target)
    {
        if (!useAttackerDirection || attacker == null || target == null)
        {
            return fixedDirection >= 0 ? 1 : -1;
        }

        return target.transform.position.x >= attacker.transform.position.x ? 1 : -1;
    }
}
