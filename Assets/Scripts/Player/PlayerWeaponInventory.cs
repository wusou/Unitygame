using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家武器背包：
/// 1. 限制最大持有数量
/// 2. 支持动态切换
/// 3. 提供事件给UI刷新
/// </summary>
public class PlayerWeaponInventory : MonoBehaviour
{
    [SerializeField] private int maxWeaponSlots = 2;
    [SerializeField] private bool replaceOldestWhenFull = false;
    [SerializeField] private bool allowDuplicateWeapons = false;
    [SerializeField] private List<WeaponDefinition> startingWeapons = new();

    private readonly List<WeaponDefinition> weapons = new();
    private int currentIndex;

    public event Action<WeaponDefinition> CurrentWeaponChanged;
    public event Action InventoryChanged;

    public int MaxWeaponSlots => maxWeaponSlots;
    public IReadOnlyList<WeaponDefinition> Weapons => weapons;

    public WeaponDefinition CurrentWeapon
    {
        get
        {
            if (weapons.Count == 0)
            {
                return null;
            }

            currentIndex = Mathf.Clamp(currentIndex, 0, weapons.Count - 1);
            return weapons[currentIndex];
        }
    }

    private void Awake()
    {
        for (var i = 0; i < startingWeapons.Count; i++)
        {
            TryAddWeapon(startingWeapons[i]);
        }

        NotifyWeaponChanged();
    }

    public bool TryAddWeapon(WeaponDefinition weapon)
    {
        if (weapon == null)
        {
            return false;
        }

        if (!allowDuplicateWeapons && weapons.Contains(weapon))
        {
            return false;
        }

        if (weapons.Count >= maxWeaponSlots)
        {
            if (!replaceOldestWhenFull)
            {
                return false;
            }

            weapons.RemoveAt(0);
            currentIndex = Mathf.Clamp(currentIndex - 1, 0, Mathf.Max(0, weapons.Count - 1));
        }

        weapons.Add(weapon);
        if (weapons.Count == 1)
        {
            currentIndex = 0;
        }

        NotifyWeaponChanged();
        return true;
    }

    public void SwitchNext()
    {
        if (weapons.Count <= 1)
        {
            return;
        }

        currentIndex = (currentIndex + 1) % weapons.Count;
        NotifyWeaponChanged();
    }

    public void SwitchPrevious()
    {
        if (weapons.Count <= 1)
        {
            return;
        }

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = weapons.Count - 1;
        }

        NotifyWeaponChanged();
    }

    private void NotifyWeaponChanged()
    {
        InventoryChanged?.Invoke();
        CurrentWeaponChanged?.Invoke(CurrentWeapon);
    }
}
