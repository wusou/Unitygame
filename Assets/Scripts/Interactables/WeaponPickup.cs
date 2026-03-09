using UnityEngine;

/// <summary>
/// 武器拾取物：支持触发自动拾取，也支持F交互拾取。
/// </summary>
public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private WeaponDefinition weaponDefinition;
    [SerializeField] private bool autoPickupOnTrigger = true;
    [SerializeField] private string interactionTitle = "拾取武器";

    public string InteractionTitle => interactionTitle;

    public void SetWeapon(WeaponDefinition definition)
    {
        weaponDefinition = definition;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        var inventory = interactor.GetPlayerComponent<PlayerWeaponInventory>();
        return inventory != null && weaponDefinition != null;
    }

    public void Interact(PlayerInteractor interactor)
    {
        TryPickup(interactor.GetPlayerComponent<PlayerWeaponInventory>());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoPickupOnTrigger || !other.CompareTag("Player"))
        {
            return;
        }

        var inventory = other.GetComponent<PlayerWeaponInventory>();
        TryPickup(inventory);
    }

    private void TryPickup(PlayerWeaponInventory inventory)
    {
        if (inventory == null || weaponDefinition == null)
        {
            return;
        }

        if (!inventory.TryAddWeapon(weaponDefinition))
        {
            Debug.Log($"背包已满或武器重复: {weaponDefinition.name}");
            return;
        }

        Destroy(gameObject);
    }
}
