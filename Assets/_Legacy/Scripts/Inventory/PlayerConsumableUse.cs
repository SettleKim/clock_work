using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerItemInventory))]
[RequireComponent(typeof(Health))]
public class PlayerConsumableUse : MonoBehaviour
{
    [SerializeField] ItemType consumableType = ItemType.Potion;

    PlayerInput playerInput;
    PlayerItemInventory inventory;
    Health health;
    InputAction useAction;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        inventory = GetComponent<PlayerItemInventory>();
        health = GetComponent<Health>();
        useAction = playerInput.actions.FindAction("UseConsumable", false);
    }

    void Update()
    {
        if (PlayerMenuUI.IsMenuOpen || TradeUI.IsTradeOpen)
            return;

        bool pressed = useAction != null && useAction.WasPressedThisFrame();
        if (!pressed && Keyboard.current != null)
            pressed = Keyboard.current.qKey.wasPressedThisFrame;

        if (pressed)
            TryUseConsumable();
    }

    void TryUseConsumable()
    {
        if (!ItemDatabase.IsConsumable(consumableType))
            return;

        if (inventory.GetCount(consumableType) <= 0)
            return;

        if (health.CurrentHealth >= health.MaxHealth)
            return;

        if (!inventory.RemoveItem(consumableType, 1))
            return;

        health.Heal(ItemDatabase.GetHealAmount(consumableType));
    }

    void OnGUI()
    {
        if (PlayerMenuUI.IsMenuOpen || TradeUI.IsTradeOpen)
            return;

        const float slotSize = 48f;
        const float padding = 12f;
        float x = Screen.width - slotSize - padding;
        float y = Screen.height - slotSize - padding;

        int count = inventory.GetCount(consumableType);
        Color potionColor = ItemDatabase.GetColor(consumableType);

        DrawRect(x, y, slotSize, slotSize, new Color(0.08f, 0.1f, 0.12f, 0.9f));
        DrawRect(x, y, slotSize, slotSize, new Color(0.35f, 0.75f, 0.45f, 0.85f), false);
        DrawRect(x + 10f, y + 8f, slotSize - 20f, slotSize - 22f, potionColor);

        var nameStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            normal = { textColor = Color.white },
        };
        var countStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperRight,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.95f, 0.7f) },
        };
        var keyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerLeft,
            fontSize = 10,
            normal = { textColor = new Color(0.8f, 0.85f, 0.9f) },
        };

        GUI.Label(new Rect(x, y + slotSize - 18f, slotSize, 16f), "포션", nameStyle);
        GUI.Label(new Rect(x + 2f, y + 2f, slotSize - 4f, 18f), count.ToString(), countStyle);
        GUI.Label(new Rect(x + 4f, y + 2f, 20f, 16f), "Q", keyStyle);
    }

    static void DrawRect(float x, float y, float w, float h, Color color, bool filled = true)
    {
        var prev = GUI.color;
        GUI.color = color;
        if (filled)
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        else
        {
            GUI.DrawTexture(new Rect(x, y, w, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y + h - 2f, w, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y, 2f, h), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + w - 2f, y, 2f, h), Texture2D.whiteTexture);
        }
        GUI.color = prev;
    }
}
