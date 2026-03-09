using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景运行时兜底：当旧Prefab脚本引用失联时，自动给玩家挂上新系统关键组件。
/// </summary>
public static class RuntimeGameplayBootstrap
{
    private static bool hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (!hooked)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            hooked = true;
        }

        BootstrapCurrentScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BootstrapCurrentScene();
    }

    private static void BootstrapCurrentScene()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        var controller = Ensure<PlayerController>(player);
        var inventory = Ensure<PlayerWeaponInventory>(player);
        Ensure<PlayerCombat>(player);
        Ensure<PlayerWeaponVisual>(player);
        Ensure<PlayerInteractor>(player);
        Ensure<PlayerWallet>(player);
        Ensure<PlayerHealth>(player);

        if (inventory != null)
        {
            // 旧场景里经常残留 2 格容量，导致永远捡不到掉落。
            inventory.EnsureCapacityAtLeast(8);
        }

        if (controller != null)
        {
            InventoryUIBootstrap.EnsureUI(inventory);
        }
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        return go.AddComponent<T>();
    }
}
