using System;
using UnityEngine;

/// <summary>
/// 玩家货币系统，给NPC商店使用。
/// </summary>
public class PlayerWallet : MonoBehaviour
{
    [SerializeField] private int coins;

    public int Coins => coins;
    public event Action<int> CoinsChanged;

    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        coins += amount;
        CoinsChanged?.Invoke(coins);
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (coins < amount)
        {
            return false;
        }

        coins -= amount;
        CoinsChanged?.Invoke(coins);
        return true;
    }

    public void SetCoinsForSave(int value)
    {
        coins = Mathf.Max(0, value);
        CoinsChanged?.Invoke(coins);
    }
}
