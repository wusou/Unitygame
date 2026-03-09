using UnityEngine;

/// <summary>
/// 冻结效果示例：降低敌人的移动速度。
/// </summary>
[CreateAssetMenu(menuName = "Demo/Combat/Modifiers/Freeze", fileName = "FreezeModifier")]
public class FreezeWeaponModifier : WeaponModifier
{
    [SerializeField] private float duration = 2f;
    [SerializeField] private float moveMultiplier = 0.3f;

    public override void OnHit(GameObject attacker, GameObject target, WeaponDefinition weapon)
    {
        var status = target.GetComponent<EnemyStatusController>();
        status?.ApplyFreeze(duration, moveMultiplier);
    }
}
