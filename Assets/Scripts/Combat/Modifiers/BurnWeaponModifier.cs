using UnityEngine;

/// <summary>
/// 持续灼烧示例：定时扣血。
/// </summary>
[CreateAssetMenu(menuName = "Demo/Combat/Modifiers/Burn", fileName = "BurnModifier")]
public class BurnWeaponModifier : WeaponModifier
{
    [SerializeField] private float duration = 4f;
    [SerializeField] private float damagePerSecond = 3f;

    public override void OnHit(GameObject attacker, GameObject target, WeaponDefinition weapon)
    {
        var status = target.GetComponent<EnemyStatusController>();
        status?.ApplyBurn(duration, damagePerSecond);
    }
}
