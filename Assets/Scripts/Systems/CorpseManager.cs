using UnityEngine;

public class CorpseManager : MonoBehaviour
{
    public static CorpseManager Instance { get; private set; }

    [SerializeField] private GameObject corpsePrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SpawnCorpseAt(Vector2 position, int levelIndex)
    {
        if (corpsePrefab == null || GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.CurrentLevelIndex != levelIndex)
        {
            return;
        }

        var corpseGo = Instantiate(corpsePrefab, position, Quaternion.identity);
        var corpse = corpseGo.GetComponent<Corpse>();
        if (corpse != null)
        {
            corpse.SetLevelIndex(levelIndex);
        }
    }
}
