using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerItemInventory))]
public class PlayerNpcInteractor : MonoBehaviour
{
    PlayerInput playerInput;
    PlayerItemInventory inventory;
    InputAction interactAction;
    NpcMerchant nearbyMerchant;
    MapDoor nearbyDoor;
    float merchantDistance = float.MaxValue;
    float doorDistance = float.MaxValue;

    public static NpcMerchant NearbyMerchant { get; private set; }
    public static MapDoor NearbyDoor { get; private set; }

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        inventory = GetComponent<PlayerItemInventory>();
        interactAction = playerInput.actions.FindAction("Grapple", false);
    }

    void Update()
    {
        if (PlayerMenuUI.IsMenuOpen || TradeUI.IsTradeOpen)
        {
            nearbyMerchant = null;
            nearbyDoor = null;
            NearbyMerchant = null;
            NearbyDoor = null;
            return;
        }

        nearbyMerchant = FindNearestMerchant();
        nearbyDoor = FindNearestDoor();
        NearbyMerchant = nearbyMerchant;
        NearbyDoor = nearbyDoor;
        TryInteract();
    }

    NpcMerchant FindNearestMerchant()
    {
        NpcMerchant[] merchants = FindObjectsByType<NpcMerchant>(FindObjectsSortMode.None);
        NpcMerchant closest = null;
        merchantDistance = float.MaxValue;

        foreach (NpcMerchant merchant in merchants)
        {
            if (!merchant.IsPlayerInRange(transform))
                continue;

            float distance = Vector2.Distance(transform.position, merchant.transform.position);
            if (distance < merchantDistance)
            {
                merchantDistance = distance;
                closest = merchant;
            }
        }

        return closest;
    }

    MapDoor FindNearestDoor()
    {
        MapDoor[] doors = FindObjectsByType<MapDoor>(FindObjectsSortMode.None);
        MapDoor closest = null;
        doorDistance = float.MaxValue;

        foreach (MapDoor door in doors)
        {
            if (!door.IsPlayerInRange(transform))
                continue;

            float distance = Vector2.Distance(transform.position, door.transform.position);
            if (distance < doorDistance)
            {
                doorDistance = distance;
                closest = door;
            }
        }

        return closest;
    }

    void TryInteract()
    {
        if (PlayerMenuUI.IsMenuOpen || TradeUI.IsTradeOpen)
            return;

        bool pressed = interactAction != null && interactAction.WasPressedThisFrame();
        if (!pressed && Keyboard.current != null)
            pressed = Keyboard.current.eKey.wasPressedThisFrame;

        if (!pressed)
            return;

        if (nearbyDoor != null && (nearbyMerchant == null || doorDistance <= merchantDistance))
        {
            nearbyDoor.Interact(gameObject);
            return;
        }

        if (nearbyMerchant != null)
            nearbyMerchant.Interact(inventory);
    }
}
