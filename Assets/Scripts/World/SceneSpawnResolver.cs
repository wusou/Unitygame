using UnityEngine;

/// <summary>
/// 场景载入后把玩家放到指定出生点。
/// 用法：每个关卡放一个该组件即可。
/// </summary>
public class SceneSpawnResolver : MonoBehaviour
{
    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        var points = FindObjectsOfType<SpawnPoint>();
        if (points == null || points.Length == 0)
        {
            return;
        }

        SpawnPoint selected = null;
        var pendingId = SceneTransitionContext.PendingSpawnId;

        if (!string.IsNullOrWhiteSpace(pendingId))
        {
            for (var i = 0; i < points.Length; i++)
            {
                if (points[i].SpawnId == pendingId)
                {
                    selected = points[i];
                    break;
                }
            }
        }

        if (selected == null)
        {
            for (var i = 0; i < points.Length; i++)
            {
                if (points[i].IsDefaultSpawn)
                {
                    selected = points[i];
                    break;
                }
            }
        }

        selected ??= points[0];
        player.transform.position = selected.transform.position;
        SceneTransitionContext.PendingSpawnId = null;
    }
}
