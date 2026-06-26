using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] ItemType itemType = ItemType.Scrap;
    [SerializeField] int amount = 1;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerItemInventory inventory = other.GetComponent<PlayerItemInventory>();
        if (inventory == null)
            return;

        inventory.AddItem(itemType, amount);
        Destroy(gameObject);
    }
}
