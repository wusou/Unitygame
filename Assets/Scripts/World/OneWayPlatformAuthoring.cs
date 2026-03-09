using UnityEngine;

/// <summary>
/// 一键把平台配置为“从下跳上去、从上站住”的单向平台。
/// 再配合 PlayerController 的蹲+跳，就能从上方穿下去。
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PlatformEffector2D))]
public class OneWayPlatformAuthoring : MonoBehaviour
{
    [SerializeField] private float surfaceArc = 170f;

    private void Reset()
    {
        ApplyConfig();
    }

    private void Awake()
    {
        ApplyConfig();
    }

    private void ApplyConfig()
    {
        var col = GetComponent<Collider2D>();
        var effector = GetComponent<PlatformEffector2D>();

        col.usedByEffector = true;
        effector.useOneWay = true;
        effector.surfaceArc = surfaceArc;
        effector.useSideFriction = false;
        effector.useSideBounce = false;
    }
}
