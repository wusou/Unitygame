using UnityEngine;
using UnityEngine.InputSystem;

public enum WeaponType
{
    Melee = 0,
    Bow = 1
}

[RequireComponent(typeof(PlayerController), typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("武器切换")]
    [SerializeField] private WeaponType currentWeapon = WeaponType.Melee;
    [SerializeField] private InputActionReference switchWeaponAction;

    [Header("近战参数")]
    [SerializeField] private int meleeDamage = 25;
    [SerializeField] private float meleeRange = 1.5f;
    [SerializeField] private float meleeCooldown = 0.4f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayer;

    [Header("远程参数")]
    [SerializeField] private int bowDamage = 15;
    [SerializeField] private float bowCooldown = 0.6f;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawn;

    [Header("Input System")]
    [SerializeField] private InputActionReference meleeAction;
    [SerializeField] private InputActionReference bowAction;

    [HideInInspector] public int bonusDamage;

    private PlayerController controller;
    private Animator animator;
    private float meleeTimer;
    private float bowTimer;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        meleeAction?.action?.Enable();
        bowAction?.action?.Enable();
        switchWeaponAction?.action?.Enable();
    }

    private void OnDisable()
    {
        meleeAction?.action?.Disable();
        bowAction?.action?.Disable();
        switchWeaponAction?.action?.Disable();
    }

    private void Update()
    {
        meleeTimer -= Time.deltaTime;
        bowTimer -= Time.deltaTime;

        if (ReadSwitchWeaponPressed())
        {
            ToggleWeapon();
        }

        if (currentWeapon == WeaponType.Melee && ReadMeleePressed() && meleeTimer <= 0f)
        {
            MeleeAttack();
        }

        if (currentWeapon == WeaponType.Bow && ReadBowPressed() && bowTimer <= 0f)
        {
            BowAttack();
        }
    }

    private bool ReadSwitchWeaponPressed()
    {
        if (switchWeaponAction != null && switchWeaponAction.action != null)
        {
            return switchWeaponAction.action.WasPressedThisFrame();
        }

        return Input.GetKeyDown(KeyCode.Q);
    }

    private bool ReadMeleePressed()
    {
        if (meleeAction != null && meleeAction.action != null)
        {
            return meleeAction.action.WasPressedThisFrame();
        }

        return Input.GetKeyDown(KeyCode.J);
    }

    private bool ReadBowPressed()
    {
        if (bowAction != null && bowAction.action != null)
        {
            return bowAction.action.WasPressedThisFrame();
        }

        return Input.GetKeyDown(KeyCode.K);
    }

    public void ToggleWeapon()
    {
        currentWeapon = currentWeapon == WeaponType.Melee ? WeaponType.Bow : WeaponType.Melee;
    }

    private void MeleeAttack()
    {
        if (attackPoint == null)
        {
            return;
        }

        meleeTimer = meleeCooldown;
        animator.SetTrigger("Attack");

        // 忽略Layer配置错误，直接按组件识别敌人。
        var hits = Physics2D.OverlapCircleAll(attackPoint.position, meleeRange);
        foreach (var hit in hits)
        {
            if (hit == null || hit.gameObject == gameObject)
            {
                continue;
            }

            var enemy = hit.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(meleeDamage + bonusDamage);
            }
        }
    }

    private void BowAttack()
    {
        if (arrowSpawn == null)
        {
            return;
        }

        bowTimer = bowCooldown;
        animator.SetTrigger("Bow");

        var arrowObject = arrowPrefab != null
            ? Instantiate(arrowPrefab, arrowSpawn.position, Quaternion.identity)
            : CreateFallbackArrow(arrowSpawn.position);

        if (arrowObject == null)
        {
            return;
        }

        var arrowScript = arrowObject.GetComponent<Arrow>();
        if (arrowScript != null)
        {
            arrowScript.Initialize(controller.Facing, bowDamage + bonusDamage, ProjectileOwner.Player);
        }
    }

    private GameObject CreateFallbackArrow(Vector3 spawnPosition)
    {
        var fallback = new GameObject("RuntimePlayerArrow");
        fallback.transform.position = spawnPosition;

        var rb = fallback.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var collider = fallback.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        fallback.AddComponent<Arrow>();
        return fallback;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, meleeRange);
    }
}
