using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-450)]
public class SaveGameManager : MonoBehaviour
{
    [Serializable]
    private class SaveSlotFile
    {
        public int formatVersion = 1;
        public int slotIndex;
        public string slotDisplayName;
        public long savedAtUnix;
        public SaveSnapshot snapshot;
    }

    [Serializable]
    private class SaveSnapshot
    {
        public string sceneName;
        public SerializableVector3 playerPosition;
        public int playerHealth;
        public int playerCoins;
        public int playerBonusDamage;
        public float playerBonusSpeed;
        public int inventoryCurrentIndex;
        public SavedWeaponRecord[] inventoryWeapons;

        public int runCount;
        public int currentLevelIndex;
        public int difficulty;
        public SavedCorpseRecord[] corpses;
    }

    [Serializable]
    private struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(Vector3 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    private class SavedWeaponRecord
    {
        public string weaponId;
        public string displayName;
        public int attackMode;
        public int baseDamage;
        public float range;
        public float cooldown;
        public bool canDrop;
    }

    [Serializable]
    private class SavedCorpseRecord
    {
        public float positionX;
        public float positionY;
        public int levelIndex;
        public int relicType;
        public int relicValue;
        public int runNumber;
        public bool isLooted;
    }

    public class SaveSlotSummary
    {
        public int SlotIndex { get; }
        public bool Exists { get; }
        public string DisplayName { get; }
        public string SceneName { get; }
        public DateTime SavedAtLocal { get; }
        public string SavedAtText => Exists ? SavedAtLocal.ToString("yyyy-MM-dd HH:mm:ss") : "-";

        public SaveSlotSummary(int slotIndex, bool exists, string displayName, string sceneName, DateTime savedAtLocal)
        {
            SlotIndex = slotIndex;
            Exists = exists;
            DisplayName = displayName;
            SceneName = sceneName;
            SavedAtLocal = savedAtLocal;
        }
    }

    public static SaveGameManager Instance { get; private set; }

    [Header("存档配置")]
    [SerializeField, Min(1)] private int maxSlots = 12;
    [SerializeField] private string saveFolderName = "SaveSlots";
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField, Min(1f)] private float loadApplyTimeout = 8f;

    private SaveSlotFile pendingLoadFile;
    private Coroutine pendingApplyRoutine;
    private readonly Dictionary<string, WeaponDefinition> weaponById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WeaponDefinition> weaponByName = new(StringComparer.OrdinalIgnoreCase);

    public int MaxSlots => Mathf.Max(1, maxSlots);
    public event Action SlotsChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static SaveGameManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        var existing = FindObjectOfType<SaveGameManager>();
        if (existing != null)
        {
            return existing;
        }

        var go = new GameObject("SaveGameManager");
        return go.AddComponent<SaveGameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        EnsureSaveDirectory();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ConfigureSlotCount(int slotCount)
    {
        var clamped = Mathf.Max(1, slotCount);
        if (maxSlots == clamped)
        {
            return;
        }

        maxSlots = clamped;
        SlotsChanged?.Invoke();
    }

    public List<SaveSlotSummary> GetSlotSummaries()
    {
        var result = new List<SaveSlotSummary>(MaxSlots);
        for (var i = 0; i < MaxSlots; i++)
        {
            result.Add(GetSlotSummary(i));
        }

        return result;
    }

    public SaveSlotSummary GetSlotSummary(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
        {
            return new SaveSlotSummary(slotIndex, false, DefaultSlotName(slotIndex), string.Empty, DateTime.MinValue);
        }

        if (!TryReadSlotFile(slotIndex, out var file))
        {
            return new SaveSlotSummary(slotIndex, false, DefaultSlotName(slotIndex), string.Empty, DateTime.MinValue);
        }

        var displayName = string.IsNullOrWhiteSpace(file.slotDisplayName) ? DefaultSlotName(slotIndex) : file.slotDisplayName;
        var sceneName = file.snapshot != null ? file.snapshot.sceneName : string.Empty;
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(Math.Max(0, file.savedAtUnix)).LocalDateTime;
        return new SaveSlotSummary(slotIndex, true, displayName, sceneName, dateTime);
    }

    public bool TrySaveSlot(int slotIndex, bool allowOverwrite, out string message)
    {
        message = string.Empty;

        if (!IsValidSlot(slotIndex))
        {
            message = $"非法存档槽位: {slotIndex + 1}";
            return false;
        }

        var hasExisting = TryReadSlotFile(slotIndex, out var existingFile);
        if (hasExisting && !allowOverwrite)
        {
            message = "该存档槽已有内容，请确认覆盖。";
            return false;
        }

        if (!TryCaptureSnapshot(out var snapshot, out message))
        {
            return false;
        }

        var file = new SaveSlotFile
        {
            slotIndex = slotIndex,
            slotDisplayName = hasExisting && !string.IsNullOrWhiteSpace(existingFile.slotDisplayName)
                ? existingFile.slotDisplayName
                : DefaultSlotName(slotIndex),
            savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            snapshot = snapshot
        };

        if (!WriteSlotFile(slotIndex, file, out message))
        {
            return false;
        }

        message = $"已保存到存档 {slotIndex + 1}";
        SlotsChanged?.Invoke();
        return true;
    }

    public bool TryLoadSlot(int slotIndex, out string message)
    {
        message = string.Empty;

        if (!IsValidSlot(slotIndex))
        {
            message = $"非法存档槽位: {slotIndex + 1}";
            return false;
        }

        if (!TryReadSlotFile(slotIndex, out var file) || file.snapshot == null)
        {
            message = "该存档槽为空。";
            return false;
        }

        var snapshot = file.snapshot;
        if (string.IsNullOrWhiteSpace(snapshot.sceneName))
        {
            message = "存档场景信息无效。";
            return false;
        }

        if (!GameManager.SceneExistsInBuild(snapshot.sceneName))
        {
            message = $"场景 {snapshot.sceneName} 不在 Build Settings 中。";
            return false;
        }

        var gm = GameManager.Instance;
        if (gm != null)
        {
            var levelIndex = snapshot.currentLevelIndex;
            if (gm.TryGetLevelIndexBySceneName(snapshot.sceneName, out var mappedIndex))
            {
                levelIndex = mappedIndex;
            }

            gm.ApplyRuntimeState(
                snapshot.runCount,
                levelIndex,
                snapshot.difficulty,
                ConvertCorpseRecordsToRuntime(snapshot.corpses));
        }

        pendingLoadFile = file;
        SceneTransitionContext.PendingSpawnId = null;
        Time.timeScale = 1f;

        if (gm != null)
        {
            gm.LoadSceneByName(snapshot.sceneName);
        }
        else
        {
            SceneManager.LoadScene(snapshot.sceneName);
        }

        message = $"正在载入存档 {slotIndex + 1}...";
        return true;
    }

    public bool TryDeleteSlot(int slotIndex, out string message)
    {
        message = string.Empty;

        if (!IsValidSlot(slotIndex))
        {
            message = $"非法存档槽位: {slotIndex + 1}";
            return false;
        }

        var path = GetSlotFilePath(slotIndex);
        if (!File.Exists(path))
        {
            message = "该存档槽本来就是空的。";
            return false;
        }

        try
        {
            File.Delete(path);
            message = $"已删除存档 {slotIndex + 1}";
            SlotsChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            message = $"删除失败: {ex.Message}";
            return false;
        }
    }

    public bool TryRenameSlot(int slotIndex, string newName, out string message)
    {
        message = string.Empty;

        if (!IsValidSlot(slotIndex))
        {
            message = $"非法存档槽位: {slotIndex + 1}";
            return false;
        }

        if (!TryReadSlotFile(slotIndex, out var file) || file.snapshot == null)
        {
            message = "该存档槽为空，无法重命名。";
            return false;
        }

        var trimmed = string.IsNullOrWhiteSpace(newName) ? string.Empty : newName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            message = "名称不能为空。";
            return false;
        }

        file.slotDisplayName = trimmed;
        if (!WriteSlotFile(slotIndex, file, out message))
        {
            return false;
        }

        message = $"存档 {slotIndex + 1} 已重命名。";
        SlotsChanged?.Invoke();
        return true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingLoadFile == null || pendingLoadFile.snapshot == null)
        {
            return;
        }

        if (pendingApplyRoutine != null)
        {
            StopCoroutine(pendingApplyRoutine);
        }

        pendingApplyRoutine = StartCoroutine(ApplyPendingLoadAfterSceneReady());
    }

    private IEnumerator ApplyPendingLoadAfterSceneReady()
    {
        var snapshot = pendingLoadFile != null ? pendingLoadFile.snapshot : null;
        if (snapshot == null)
        {
            yield break;
        }

        // 等待场景对象 Start/Awake 执行完，再应用存档位置与属性。
        yield return null;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            var levelIndex = snapshot.currentLevelIndex;
            if (gm.TryGetLevelIndexBySceneName(snapshot.sceneName, out var mappedIndex))
            {
                levelIndex = mappedIndex;
            }

            gm.ApplyRuntimeState(
                snapshot.runCount,
                levelIndex,
                snapshot.difficulty,
                ConvertCorpseRecordsToRuntime(snapshot.corpses));
        }

        var timeoutAt = Time.unscaledTime + Mathf.Max(1f, loadApplyTimeout);
        GameObject player = null;

        while (Time.unscaledTime < timeoutAt)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                break;
            }

            yield return null;
        }

        if (player == null)
        {
            Debug.LogWarning("读档失败：未找到 Player 对象。请检查玩家 Tag 是否为 Player。");
            ClearPendingLoad();
            yield break;
        }

        ApplySnapshotToPlayer(player, snapshot);
        ClearPendingLoad();
    }

    private void ApplySnapshotToPlayer(GameObject player, SaveSnapshot snapshot)
    {
        if (player == null || snapshot == null)
        {
            return;
        }

        player.transform.position = snapshot.playerPosition.ToVector3();

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        var health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.RestoreHealthForSave(snapshot.playerHealth);
        }

        var wallet = player.GetComponent<PlayerWallet>();
        if (wallet != null)
        {
            wallet.SetCoinsForSave(snapshot.playerCoins);
        }

        var combat = player.GetComponent<PlayerCombat>();
        if (combat != null)
        {
            combat.bonusDamage = snapshot.playerBonusDamage;
        }

        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.bonusSpeed = snapshot.playerBonusSpeed;
        }

        var inventory = player.GetComponent<PlayerWeaponInventory>();
        if (inventory != null)
        {
            var loadedWeapons = ResolveWeapons(snapshot.inventoryWeapons);
            inventory.EnsureCapacityAtLeast(Mathf.Max(8, loadedWeapons.Count));
            inventory.ReplaceWeaponsFromSave(loadedWeapons, snapshot.inventoryCurrentIndex);
        }
    }

    private bool TryCaptureSnapshot(out SaveSnapshot snapshot, out string message)
    {
        snapshot = null;
        message = string.Empty;

        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || string.IsNullOrWhiteSpace(activeScene.name))
        {
            message = "当前场景无效，无法存档。";
            return false;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            message = "未找到 Player 对象，无法存档。";
            return false;
        }

        var health = player.GetComponent<PlayerHealth>();
        var wallet = player.GetComponent<PlayerWallet>();
        var controller = player.GetComponent<PlayerController>();
        var combat = player.GetComponent<PlayerCombat>();
        var inventory = player.GetComponent<PlayerWeaponInventory>();

        var gm = GameManager.Instance;

        snapshot = new SaveSnapshot
        {
            sceneName = activeScene.name,
            playerPosition = new SerializableVector3(player.transform.position),
            playerHealth = health != null ? health.CurrentHealth : 100,
            playerCoins = wallet != null ? wallet.Coins : 0,
            playerBonusDamage = combat != null ? combat.bonusDamage : 0,
            playerBonusSpeed = controller != null ? controller.bonusSpeed : 0f,
            inventoryCurrentIndex = inventory != null ? inventory.CurrentIndex : -1,
            inventoryWeapons = CaptureInventoryWeapons(inventory),
            runCount = gm != null ? gm.RunCount : 1,
            currentLevelIndex = gm != null ? gm.CurrentLevelIndex : 0,
            difficulty = gm != null ? gm.Difficulty : 1,
            corpses = gm != null ? ConvertCorpsesToRecords(gm.AllCorpses) : Array.Empty<SavedCorpseRecord>()
        };

        if (gm != null && gm.TryGetLevelIndexBySceneName(snapshot.sceneName, out var mappedIndex))
        {
            snapshot.currentLevelIndex = mappedIndex;
        }

        return true;
    }

    private SavedWeaponRecord[] CaptureInventoryWeapons(PlayerWeaponInventory inventory)
    {
        if (inventory == null || inventory.Weapons == null || inventory.Weapons.Count == 0)
        {
            return Array.Empty<SavedWeaponRecord>();
        }

        var list = new List<SavedWeaponRecord>(inventory.Weapons.Count);
        for (var i = 0; i < inventory.Weapons.Count; i++)
        {
            var weapon = inventory.Weapons[i];
            if (weapon == null)
            {
                continue;
            }

            list.Add(new SavedWeaponRecord
            {
                weaponId = weapon.WeaponId,
                displayName = weapon.DisplayName,
                attackMode = (int)weapon.AttackMode,
                baseDamage = weapon.BaseDamage,
                range = weapon.Range,
                cooldown = weapon.Cooldown,
                canDrop = weapon.CanDrop
            });
        }

        return list.ToArray();
    }

    private List<WeaponDefinition> ResolveWeapons(SavedWeaponRecord[] records)
    {
        var result = new List<WeaponDefinition>();
        if (records == null || records.Length == 0)
        {
            return result;
        }

        BuildWeaponCache();

        for (var i = 0; i < records.Length; i++)
        {
            var record = records[i];
            if (record == null)
            {
                continue;
            }

            var resolved = ResolveWeapon(record);
            if (resolved != null)
            {
                result.Add(resolved);
            }
        }

        return result;
    }

    private WeaponDefinition ResolveWeapon(SavedWeaponRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.weaponId) && weaponById.TryGetValue(record.weaponId, out var byId) && byId != null)
        {
            return byId;
        }

        if (!string.IsNullOrWhiteSpace(record.displayName) && weaponByName.TryGetValue(record.displayName, out var byName) && byName != null)
        {
            return byName;
        }

        var attackMode = Enum.IsDefined(typeof(WeaponAttackMode), record.attackMode)
            ? (WeaponAttackMode)record.attackMode
            : WeaponAttackMode.Melee;

        var runtimeId = string.IsNullOrWhiteSpace(record.weaponId)
            ? $"weapon.runtime.saved.{Guid.NewGuid():N}"
            : record.weaponId;

        var runtimeName = string.IsNullOrWhiteSpace(record.displayName) ? "恢复武器" : record.displayName;

        return WeaponDefinition.CreateRuntime(
            runtimeId,
            runtimeName,
            attackMode,
            Mathf.Max(1, record.baseDamage),
            Mathf.Max(0.5f, record.range),
            Mathf.Max(0.05f, record.cooldown),
            null,
            null,
            record.canDrop);
    }

    private void BuildWeaponCache()
    {
        weaponById.Clear();
        weaponByName.Clear();

        var weapons = Resources.FindObjectsOfTypeAll<WeaponDefinition>();
        for (var i = 0; i < weapons.Length; i++)
        {
            var weapon = weapons[i];
            if (weapon == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(weapon.WeaponId) && !weaponById.ContainsKey(weapon.WeaponId))
            {
                weaponById.Add(weapon.WeaponId, weapon);
            }

            if (!string.IsNullOrWhiteSpace(weapon.DisplayName) && !weaponByName.ContainsKey(weapon.DisplayName))
            {
                weaponByName.Add(weapon.DisplayName, weapon);
            }
        }
    }

    private SavedCorpseRecord[] ConvertCorpsesToRecords(List<CorpseData> corpses)
    {
        if (corpses == null || corpses.Count == 0)
        {
            return Array.Empty<SavedCorpseRecord>();
        }

        var records = new SavedCorpseRecord[corpses.Count];
        for (var i = 0; i < corpses.Count; i++)
        {
            var corpse = corpses[i];
            if (corpse == null)
            {
                continue;
            }

            records[i] = new SavedCorpseRecord
            {
                positionX = corpse.Position.x,
                positionY = corpse.Position.y,
                levelIndex = corpse.LevelIndex,
                relicType = (int)corpse.RelicType,
                relicValue = corpse.RelicValue,
                runNumber = corpse.RunNumber,
                isLooted = corpse.IsLooted
            };
        }

        return records;
    }

    private List<CorpseData> ConvertCorpseRecordsToRuntime(SavedCorpseRecord[] records)
    {
        var result = new List<CorpseData>();
        if (records == null || records.Length == 0)
        {
            return result;
        }

        for (var i = 0; i < records.Length; i++)
        {
            var record = records[i];
            if (record == null)
            {
                continue;
            }

            var relicType = Enum.IsDefined(typeof(RelicType), record.relicType)
                ? (RelicType)record.relicType
                : RelicType.Hp;

            result.Add(new CorpseData
            {
                Position = new Vector2(record.positionX, record.positionY),
                LevelIndex = record.levelIndex,
                RelicType = relicType,
                RelicValue = record.relicValue,
                RunNumber = record.runNumber,
                IsLooted = record.isLooted
            });
        }

        return result;
    }

    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < MaxSlots;
    }

    private string DefaultSlotName(int slotIndex)
    {
        return $"存档 {slotIndex + 1}";
    }

    private void ClearPendingLoad()
    {
        pendingLoadFile = null;
        pendingApplyRoutine = null;
    }

    private bool TryReadSlotFile(int slotIndex, out SaveSlotFile file)
    {
        file = null;

        var path = GetSlotFilePath(slotIndex);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            file = JsonUtility.FromJson<SaveSlotFile>(json);
            if (file == null)
            {
                return false;
            }

            file.slotIndex = slotIndex;
            return file.snapshot != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"读取存档失败 ({slotIndex + 1}): {ex.Message}");
            return false;
        }
    }

    private bool WriteSlotFile(int slotIndex, SaveSlotFile file, out string message)
    {
        message = string.Empty;

        try
        {
            EnsureSaveDirectory();

            file.slotIndex = slotIndex;
            if (file.savedAtUnix <= 0)
            {
                file.savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            if (string.IsNullOrWhiteSpace(file.slotDisplayName))
            {
                file.slotDisplayName = DefaultSlotName(slotIndex);
            }

            var path = GetSlotFilePath(slotIndex);
            var json = JsonUtility.ToJson(file, true);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception ex)
        {
            message = $"写入存档失败: {ex.Message}";
            return false;
        }
    }

    private void EnsureSaveDirectory()
    {
        var dir = GetSaveDirectoryPath();
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private string GetSaveDirectoryPath()
    {
        return Path.Combine(Application.persistentDataPath, saveFolderName);
    }

    private string GetSlotFilePath(int slotIndex)
    {
        var fileName = $"slot_{slotIndex + 1:00}.json";
        return Path.Combine(GetSaveDirectoryPath(), fileName);
    }
}

