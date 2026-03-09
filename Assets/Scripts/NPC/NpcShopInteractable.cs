using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC商店/对话一体组件：
/// - 交互时先显示对话
/// - 可按配置尝试购买武器
/// </summary>
public class NpcShopInteractable : MonoBehaviour, IInteractable
{
    [Serializable]
    public class ShopItem
    {
        public WeaponDefinition weapon;
        public int price = 10;
    }

    [Header("基础")]
    [SerializeField] private string interactionTitle = "交谈";
    [SerializeField] private string npcName = "商人";

    [Header("对话")]
    [SerializeField] private List<string> dialogueLines = new();

    [Header("商店")]
    [SerializeField] private bool enableShop = true;
    [SerializeField] private List<ShopItem> shopItems = new();

    private int dialogueIndex;

    public string InteractionTitle => interactionTitle;

    public bool CanInteract(PlayerInteractor interactor)
    {
        return interactor != null;
    }

    public void Interact(PlayerInteractor interactor)
    {
        ShowDialogue();

        if (!enableShop)
        {
            return;
        }

        TryBuyFirstAffordable(interactor);
    }

    private void ShowDialogue()
    {
        if (dialogueLines.Count == 0)
        {
            NpcDialoguePanel.Show($"{npcName}: 欢迎光临。", 2f);
            return;
        }

        var line = dialogueLines[dialogueIndex];
        dialogueIndex = (dialogueIndex + 1) % dialogueLines.Count;
        NpcDialoguePanel.Show($"{npcName}: {line}", 3f);
    }

    private void TryBuyFirstAffordable(PlayerInteractor interactor)
    {
        var wallet = interactor.GetPlayerComponent<PlayerWallet>();
        var inventory = interactor.GetPlayerComponent<PlayerWeaponInventory>();
        if (wallet == null || inventory == null)
        {
            return;
        }

        for (var i = 0; i < shopItems.Count; i++)
        {
            var item = shopItems[i];
            if (item.weapon == null)
            {
                continue;
            }

            if (wallet.Coins < item.price)
            {
                continue;
            }

            if (!inventory.TryAddWeapon(item.weapon))
            {
                NpcDialoguePanel.Show($"{npcName}: 你的武器栏已满。", 2f);
                return;
            }

            wallet.SpendCoins(item.price);
            NpcDialoguePanel.Show($"购买成功: {item.weapon.name} -{item.price}金币", 2.5f);
            return;
        }

        NpcDialoguePanel.Show($"{npcName}: 你暂时买不了新武器。", 2f);
    }
}
