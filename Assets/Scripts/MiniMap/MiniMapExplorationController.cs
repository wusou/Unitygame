using UnityEngine;

/// <summary>
/// 小地图探索控制：玩家进入范围后点亮对应格子。
/// </summary>
public class MiniMapExplorationController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float revealRadius = 2.5f;
    [SerializeField] private float refreshInterval = 0.2f;

    private MiniMapCell[] cells;
    private float timer;

    private void Awake()
    {
        cells = GetComponentsInChildren<MiniMapCell>(true);
    }

    private void Update()
    {
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }

            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f)
        {
            return;
        }

        timer = refreshInterval;
        RevealNearCells();
    }

    private void RevealNearCells()
    {
        if (cells == null)
        {
            return;
        }

        for (var i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null || cells[i].IsDiscovered)
            {
                continue;
            }

            var dist = Vector2.Distance(player.position, cells[i].transform.position);
            if (dist <= revealRadius)
            {
                cells[i].SetDiscovered(true);
            }
        }
    }
}
