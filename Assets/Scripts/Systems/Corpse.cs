using UnityEngine;

public class Corpse : MonoBehaviour
{
    private const float PositionTolerance = 0.5f;

    private int levelIndex;
    private CorpseData relicData;

    public void SetLevelIndex(int index)
    {
        levelIndex = index;

        if (GameManager.Instance != null && GameManager.Instance.CurrentLevelIndex != levelIndex)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Destroy(gameObject);
            return;
        }

        CorpseData closest = null;
        var minDist = float.MaxValue;

        foreach (var data in GameManager.Instance.AllCorpses)
        {
            if (data.LevelIndex != levelIndex || data.IsLooted)
            {
                continue;
            }

            var dist = Vector2.Distance(data.Position, transform.position);
            if (dist <= PositionTolerance && dist < minDist)
            {
                minDist = dist;
                closest = data;
            }
        }

        relicData = closest;
        if (relicData == null)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (relicData == null || relicData.IsLooted || !other.CompareTag("Player"))
        {
            return;
        }

        var playerCombat = other.GetComponent<PlayerCombat>();
        var playerController = other.GetComponent<PlayerController>();

        switch (relicData.RelicType)
        {
            case RelicType.Damage:
                if (playerCombat != null)
                {
                    playerCombat.bonusDamage += relicData.RelicValue;
                }
                break;
            case RelicType.Speed:
                if (playerController != null)
                {
                    playerController.bonusSpeed += relicData.RelicValue;
                }
                break;
            case RelicType.Hp:
                break;
        }

        relicData.IsLooted = true;
        Destroy(gameObject);
    }
}
