using UnityEngine;

/// <summary>
/// 地图切换上下文：跨场景存储玩家出生点ID。
/// </summary>
public static class SceneTransitionContext
{
    public static string PendingSpawnId { get; set; }
}
