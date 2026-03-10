using UnityEngine;

/// <summary>
/// 梯子地形配置器：挂到梯子触发器上后，玩家即可进入攀爬状态。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LadderAuthoring : MonoBehaviour
{
    [SerializeField] private bool forceTrigger = true;

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
        if (col == null)
        {
            return;
        }

        if (forceTrigger)
        {
            col.isTrigger = true;
        }
    }
}
