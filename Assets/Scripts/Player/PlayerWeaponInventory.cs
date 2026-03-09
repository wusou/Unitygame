using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家武器背包：
/// 1. 限制最大持有数量
/// 2. 支持动态切换
/// 3. 支持按槽位选择与丢弃
/// 4. 提供事件给UI刷新
/// </summary>
public class PlayerWeaponInventory : MonoBehaviour
{
    [SerializeField] private int maxWeaponSlots = 8;
    [SerializeField] private bool replaceOldestWhenFull;
    [SerializeField] private bool allowDuplicateWeapons;
    [SerializeField] private List<WeaponDefinition> startingWeapons = new();

    private readonly List<WeaponDefinition> weapons = new();
    private int currentIndex;

    public event Action<WeaponDefinition> CurrentWeaponChanged;
    public event Action InventoryChanged;

    public int MaxWeaponSlots => maxWeaponSlots;
    public int CurrentIndex => weapons.Count == 0 ? -1 : Mathf.Clamp(currentIndex, 0, weapons.Count - 1);
    public int WeaponCount => weapons.Count;
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

    public void EnsureCapacityAtLeast(int minSlots)
    {
        var clamped = Mathf.Max(1, minSlots);
        if (maxWeaponSlots >= clamped)
        {
            return;
        }

        maxWeaponSlots = clamped;
        InventoryChanged?.Invoke();
    }

    public bool TryAddWeapon(WeaponDefinition weapon)
    {
        return TryAddWeapon(weapon, out _);
    }

    public bool TryAddWeapon(WeaponDefinition weapon, out string failReason)
    {
        failReason = null;

        if (weapon == null)
        {
            failReason = "武器为空。";
            return false;
        }

        if (!allowDuplicateWeapons && weapons.Contains(weapon))
        {
            failReason = $"已拥有武器：{weapon.DisplayName}";
            return false;
        }

        if (weapons.Count >= maxWeaponSlots)
        {
            if (!replaceOldestWhenFull)
            {
                failReason = $"背包已满：{weapons.Count}/{maxWeaponSlots}";
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

    public bool TrySelectSlot(int slotIndexOneBased)
    {
        if (slotIndexOneBased <= 0 || slotIndexOneBased > weapons.Count)
        {
            return false;
        }

        var targetIndex = slotIndexOneBased - 1;
        if (targetIndex == currentIndex)
        {
            return true;
        }

        currentIndex = targetIndex;
        NotifyWeaponChanged();
        return true;
    }

    public bool TryDropCurrent(out WeaponDefinition droppedWeapon, out string failReason)
    {
        droppedWeapon = null;
        failReason = null;

        if (weapons.Count == 0)
        {
            failReason = "没有可丢弃的武器。";
            return false;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, weapons.Count - 1);
        var current = weapons[currentIndex];

        if (current != null && !current.CanDrop)
        {
            failReason = $"武器 {current.DisplayName} 不可丢弃。";
            return false;
        }

        weapons.RemoveAt(currentIndex);
        if (weapons.Count == 0)
        {
            currentIndex = 0;
        }
        else if (currentIndex >= weapons.Count)
        {
            currentIndex = weapons.Count - 1;
        }

        droppedWeapon = current;
        NotifyWeaponChanged();
        return true;
    }

    private void NotifyWeaponChanged()
    {
        InventoryChanged?.Invoke();
        CurrentWeaponChanged?.Invoke(CurrentWeapon);
    }
}
