using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private void Start()
    {
        SpawnRelicsFromPreviousRuns();
    }

    private void SpawnRelicsFromPreviousRuns()
    {
        var gm = GameManager.Instance;
        if (gm == null)
        {
            return;
        }

        var corpses = gm.GetCorpsesForLevel(gm.CurrentLevelIndex);
        foreach (var corpse in corpses)
        {
            CorpseManager.Instance?.SpawnCorpseAt(corpse.Position, gm.CurrentLevelIndex);
        }
    }
}
