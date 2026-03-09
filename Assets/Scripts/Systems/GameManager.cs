using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("关卡配置")]
    [SerializeField] private string[] levelScenes = { "SampleScene" };
    [SerializeField] private string[] levelNames = { "第一章：演示关卡" };
    [SerializeField] private string victorySceneName = "VictoryScreen";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public IReadOnlyList<string> LevelNames => levelNames;

    public int RunCount { get; private set; } = 1;
    public int CurrentLevelIndex { get; private set; }
    public int Difficulty { get; private set; } = 1;

    public List<CorpseData> AllCorpses { get; } = new();

    public event Action<int> RunChanged;

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

    public void StartNewRun()
    {
        CurrentLevelIndex = 0;
        LoadLevelByIndex(CurrentLevelIndex);
    }

    public void NextLevel()
    {
        CurrentLevelIndex++;
        if (CurrentLevelIndex >= levelScenes.Length)
        {
            LoadSceneWithFallback(victorySceneName);
            return;
        }

        LoadLevelByIndex(CurrentLevelIndex);
    }

    public void LoadMainMenu()
    {
        LoadSceneWithFallback(mainMenuSceneName);
    }

    public void LoadSceneByName(string sceneName)
    {
        LoadSceneWithFallback(sceneName);
    }

    public void OnPlayerDeath(Vector2 deathPos)
    {
        var roll = UnityEngine.Random.Range(0, 3);
        var relicType = (RelicType)roll;
        var relicValue = relicType switch
        {
            RelicType.Damage => 5,
            RelicType.Speed => 15,
            _ => 15
        };

        AllCorpses.Add(new CorpseData
        {
            Position = deathPos,
            LevelIndex = CurrentLevelIndex,
            RelicType = relicType,
            RelicValue = relicValue,
            RunNumber = RunCount,
            IsLooted = false
        });
    }

    public void Respawn()
    {
        RunCount++;
        Difficulty = Mathf.Min(5, 1 + RunCount / 3);
        RunChanged?.Invoke(RunCount);
        StartNewRun();
    }

    public List<CorpseData> GetCorpsesForLevel(int levelIndex)
    {
        return AllCorpses.FindAll(c => c.LevelIndex == levelIndex && !c.IsLooted);
    }

    public string GetCurrentLevelName()
    {
        if (CurrentLevelIndex < 0 || CurrentLevelIndex >= levelNames.Length)
        {
            return string.Empty;
        }

        return levelNames[CurrentLevelIndex];
    }

    public static bool SceneExistsInBuild(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        var normalized = sceneName.EndsWith(".scene", StringComparison.OrdinalIgnoreCase)
            ? sceneName
            : sceneName + ".scene";

        var targetFile = System.IO.Path.GetFileName(normalized);
        for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.Equals(System.IO.Path.GetFileName(path), targetFile, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(System.IO.Path.GetFileNameWithoutExtension(path), sceneName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void LoadLevelByIndex(int index)
    {
        if (index < 0 || index >= levelScenes.Length)
        {
            Debug.LogError($"关卡索引越界: {index}");
            return;
        }

        LoadSceneWithFallback(levelScenes[index]);
    }

    private void LoadSceneWithFallback(string sceneName)
    {
        if (SceneExistsInBuild(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneManager.LoadScene(0);
            return;
        }

        Debug.LogError($"未找到场景: {sceneName}，且 Build Settings 为空。\n请先把目标场景加入 Build Settings。");
    }
}
