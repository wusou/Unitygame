using UnityEngine;

/// <summary>
/// 小地图单元：被探索后关闭遮罩。
/// </summary>
public class MiniMapCell : MonoBehaviour
{
    [SerializeField] private GameObject fogMask;
    [SerializeField] private bool discoveredOnStart;

    public bool IsDiscovered { get; private set; }

    private void Start()
    {
        SetDiscovered(discoveredOnStart);
    }

    public void SetDiscovered(bool discovered)
    {
        IsDiscovered = discovered;
        if (fogMask != null)
        {
            fogMask.SetActive(!discovered);
        }
    }
}
