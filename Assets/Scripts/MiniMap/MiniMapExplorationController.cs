using System;
using UnityEngine;

/// <summary>
/// 小地图探索控制：玩家进入范围后点亮对应格子。
/// </summary>
public class MiniMapExplorationController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float revealRadius = 2.5f;
    [SerializeField] private float refreshInterval = 0.2f;

    [Header("侧滚动分段探索")]
    [SerializeField] private bool useHorizontalSegmentation = true;
    [SerializeField] private float worldMinX = -20f;
    [SerializeField] private float worldMaxX = 120f;

    private MiniMapCell[] cells;
    private float timer;

    private void Awake()
    {
        cells = GetComponentsInChildren<MiniMapCell>(true);
        SortCellsByRootOrder();
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
        if (cells == null || cells.Length == 0)
        {
            return;
        }

        if (useHorizontalSegmentation)
        {
            RevealByHorizontalSegments();
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

    private void RevealByHorizontalSegments()
    {
        if (worldMaxX <= worldMinX + 0.01f)
        {
            worldMaxX = worldMinX + 0.01f;
        }

        var playerX = player.position.x;
        if (playerX < worldMinX) worldMinX = playerX;
        if (playerX > worldMaxX) worldMaxX = playerX;

        var segmentCount = Mathf.Max(1, cells.Length);

        for (var i = 0; i < cells.Length; i++)
        {
            var cell = cells[i];
            if (cell == null || cell.IsDiscovered)
            {
                continue;
            }

            var startX = Mathf.Lerp(worldMinX, worldMaxX, i / (float)segmentCount);
            var endX = Mathf.Lerp(worldMinX, worldMaxX, (i + 1f) / segmentCount);
            if (playerX >= startX - revealRadius && playerX <= endX + revealRadius)
            {
                cell.SetDiscovered(true);
            }
        }
    }

    private void SortCellsByRootOrder()
    {
        if (cells == null || cells.Length <= 1)
        {
            return;
        }

        Array.Sort(cells, CompareCells);
    }

    private int CompareCells(MiniMapCell left, MiniMapCell right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        var leftIndex = GetRootChildIndex(left.transform);
        var rightIndex = GetRootChildIndex(right.transform);

        var byRoot = leftIndex.CompareTo(rightIndex);
        if (byRoot != 0)
        {
            return byRoot;
        }

        return string.Compare(left.name, right.name, StringComparison.Ordinal);
    }

    private int GetRootChildIndex(Transform t)
    {
        var cursor = t;
        while (cursor != null && cursor.parent != transform)
        {
            cursor = cursor.parent;
        }

        return cursor != null ? cursor.GetSiblingIndex() : int.MaxValue;
    }
}
