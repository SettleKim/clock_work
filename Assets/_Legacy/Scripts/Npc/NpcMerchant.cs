using UnityEngine;

public class NpcMerchant : MonoBehaviour
{
    [SerializeField] float interactionRadius = 2.5f;
    [SerializeField] ItemType costItem = ItemType.Scrap;
    [SerializeField] int costAmount = 1;
    [SerializeField] ItemType rewardItem = ItemType.Potion;
    [SerializeField] int rewardAmount = 1;

    public ItemType CostItem => costItem;
    public int CostAmount => costAmount;
    public ItemType RewardItem => rewardItem;
    public int RewardAmount => rewardAmount;

    public bool IsPlayerInRange(Transform player)
    {
        if (player == null)
            return false;

        return Vector2.Distance(transform.position, player.position) <= interactionRadius;
    }

    public void Interact(PlayerItemInventory inventory)
    {
        TradeUI.OpenTrade(this, inventory);
    }

    public bool TryTrade(PlayerItemInventory inventory)
    {
        if (inventory == null || inventory.GetCount(costItem) < costAmount)
            return false;

        inventory.RemoveItem(costItem, costAmount);
        inventory.AddItem(rewardItem, rewardAmount);
        return true;
    }
}
