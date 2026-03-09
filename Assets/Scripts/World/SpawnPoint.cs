using UnityEngine;

/// <summary>
/// 场景出生点标记。
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId = "default";
    [SerializeField] private bool isDefaultSpawn;

    public string SpawnId => spawnId;
    public bool IsDefaultSpawn => isDefaultSpawn;
}
