using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] WeaponType weaponType = WeaponType.Hammer;

    void Reset()
    {
        var collider = GetComponent<Collider2D>();
        collider.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerWeaponInventory inventory = other.GetComponent<PlayerWeaponInventory>();
        if (inventory == null)
            return;

        inventory.UnlockWeapon(weaponType);
        Destroy(gameObject);
    }
}
