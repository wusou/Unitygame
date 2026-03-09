using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器定义（ScriptableObject）。
/// 设计思路：把数据和行为扩展点分离，后续增加新武器只需新建资产+新 Modifier。
/// </summary>
[CreateAssetMenu(menuName = "Demo/Combat/Weapon Definition", fileName = "WeaponDefinition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("基础信息")]
    [SerializeField] private string weaponId = "weapon.default";
    [SerializeField] private string displayName = "Default Weapon";
    [SerializeField] private string description = "Replace with your own weapon design.";
    [SerializeField] private Sprite icon;
    [SerializeField] private bool canDrop = true;

    [Header("战斗参数")]
    [SerializeField] private WeaponAttackMode attackMode = WeaponAttackMode.Melee;
    [SerializeField] private int baseDamage = 10;
    [SerializeField] private float range = 1.5f;
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private GameObject projectilePrefab;

    [Header("扩展效果")]
    [SerializeField] private List<WeaponModifier> modifiers = new();

    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public bool CanDrop => canDrop;
    public WeaponAttackMode AttackMode => attackMode;
    public int BaseDamage => baseDamage;
    public float Range => range;
    public float Cooldown => cooldown;
    public GameObject ProjectilePrefab => projectilePrefab;
    public IReadOnlyList<WeaponModifier> Modifiers => modifiers;

    public static WeaponDefinition CreateRuntime(
        string id,
        string name,
        WeaponAttackMode mode,
        int damage,
        float attackRange,
        float attackCooldown,
        GameObject projectile,
        Sprite weaponIcon = null,
        bool dropable = true)
    {
        var weapon = CreateInstance<WeaponDefinition>();
        weapon.weaponId = string.IsNullOrWhiteSpace(id) ? "weapon.runtime" : id;
        weapon.displayName = string.IsNullOrWhiteSpace(name) ? "Runtime Weapon" : name;
        weapon.description = "Runtime generated fallback weapon.";
        weapon.icon = weaponIcon;
        weapon.canDrop = dropable;
        weapon.attackMode = mode;
        weapon.baseDamage = Mathf.Max(1, damage);
        weapon.range = Mathf.Max(0.1f, attackRange);
        weapon.cooldown = Mathf.Max(0.05f, attackCooldown);
        weapon.projectilePrefab = projectile;
        weapon.modifiers = new List<WeaponModifier>();
        return weapon;
    }
}
