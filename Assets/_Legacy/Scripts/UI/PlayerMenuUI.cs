using UnityEngine;
using UnityEngine.InputSystem;

public enum MenuPanel
{
    Status,
    Inventory,
}

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerItemInventory))]
[RequireComponent(typeof(PlayerWeaponInventory))]
public class PlayerMenuUI : MonoBehaviour
{
    const int InventoryColumns = 4;
    const int InventoryRows = 3;

    [SerializeField] Key menuToggleKey = Key.Tab;

    PlayerInput playerInput;
    Health health;
    PlayerItemInventory itemInventory;
    PlayerWeaponInventory weaponInventory;
    InputAction menuToggleAction;

    MenuPanel currentPanel = MenuPanel.Status;
    bool isOpen;

    public static bool IsMenuOpen { get; private set; }

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        health = GetComponent<Health>();
        itemInventory = GetComponent<PlayerItemInventory>();
        weaponInventory = GetComponent<PlayerWeaponInventory>();

        menuToggleAction = playerInput.actions.FindAction("MenuToggle", false);
    }

    void Update()
    {
        if (menuToggleAction != null && menuToggleAction.WasPressedThisFrame())
            ToggleMenu();
        else if (Keyboard.current != null && Keyboard.current[menuToggleKey].wasPressedThisFrame)
            ToggleMenu();

        if (!isOpen)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            currentPanel = MenuPanel.Status;
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            currentPanel = MenuPanel.Inventory;
    }

    void ToggleMenu()
    {
        isOpen = !isOpen;
        IsMenuOpen = isOpen;
        if (isOpen)
            currentPanel = MenuPanel.Status;
    }

    void OnGUI()
    {
        if (!isOpen)
            return;

        const float panelWidth = 420f;
        const float panelHeight = 300f;
        float x = (Screen.width - panelWidth) * 0.5f;
        float y = (Screen.height - panelHeight) * 0.5f;

        DrawRect(x, y, panelWidth, panelHeight, new Color(0.06f, 0.08f, 0.1f, 0.94f));
        DrawRect(x, y, panelWidth, panelHeight, new Color(0.35f, 0.55f, 0.7f, 0.85f), false);

        DrawTabs(x, y, panelWidth);
        DrawPanelContent(x, y + 52f, panelWidth, panelHeight - 52f);

        var hintStyle = MakeLabelStyle(11, new Color(0.75f, 0.8f, 0.85f));
        GUI.Label(new Rect(x, y + panelHeight - 24f, panelWidth, 18f), "← 상태  |  인벤토리 →   Tab: 닫기", hintStyle);
    }

    void DrawTabs(float panelX, float panelY, float panelWidth)
    {
        const float tabWidth = 140f;
        const float tabHeight = 36f;
        float tabY = panelY + 10f;
        float statusX = panelX + panelWidth * 0.5f - tabWidth - 24f;
        float inventoryX = panelX + panelWidth * 0.5f + 24f;

        DrawTab(statusX, tabY, tabWidth, tabHeight, "상태", currentPanel == MenuPanel.Status);
        DrawTab(inventoryX, tabY, tabWidth, tabHeight, "인벤토리", currentPanel == MenuPanel.Inventory);

        var arrowStyle = MakeLabelStyle(18, new Color(0.55f, 0.85f, 1f));
        GUI.Label(new Rect(panelX + panelWidth * 0.5f - 12f, tabY + 4f, 24f, 24f), "⇄", arrowStyle);
    }

    void DrawTab(float x, float y, float width, float height, string label, bool selected)
    {
        Color bg = selected ? new Color(0.18f, 0.28f, 0.36f, 0.98f) : new Color(0.1f, 0.12f, 0.15f, 0.85f);
        DrawRect(x, y, width, height, bg);
        if (selected)
            DrawRect(x, y, width, height, new Color(0.45f, 0.85f, 1f, 0.9f), false);

        var style = MakeLabelStyle(14, selected ? Color.white : new Color(0.75f, 0.78f, 0.82f));
        GUI.Label(new Rect(x, y + 8f, width, height), label, style);
    }

    void DrawPanelContent(float x, float y, float width, float height)
    {
        if (currentPanel == MenuPanel.Status)
            DrawStatusPanel(x + 16f, y + 8f, width - 32f, height - 40f);
        else
            DrawInventoryPanel(x + 16f, y + 8f, width - 32f, height - 40f);
    }

    void DrawStatusPanel(float x, float y, float width, float height)
    {
        var titleStyle = MakeLabelStyle(16, new Color(0.85f, 0.92f, 1f));
        var labelStyle = MakeLabelStyle(13, new Color(0.82f, 0.86f, 0.9f));
        var valueStyle = MakeLabelStyle(13, Color.white);

        GUI.Label(new Rect(x, y, width, 24f), "캐릭터 상태", titleStyle);

        float rowY = y + 36f;
        const float rowHeight = 26f;

        DrawStatusRow(x, rowY, width, rowHeight, "체력", $"{health.CurrentHealth:0} / {health.MaxHealth:0}", labelStyle, valueStyle);
        rowY += rowHeight;

        WeaponStats stats = weaponInventory.GetCurrentStats();
        DrawStatusRow(x, rowY, width, rowHeight, "장착 무기", stats.displayName, labelStyle, valueStyle);
        rowY += rowHeight;

        DrawStatusRow(x, rowY, width, rowHeight, "공격력", stats.damage.ToString("0"), labelStyle, valueStyle);
        rowY += rowHeight;

        DrawStatusRow(x, rowY, width, rowHeight, "공격 속도", stats.canCharge ? "차지 가능" : "일반", labelStyle, valueStyle);
        rowY += rowHeight + 8f;

        float barWidth = width - 80f;
        float barX = x + 72f;
        DrawHealthBar(barX, rowY, barWidth, 14f, health.CurrentHealth / health.MaxHealth);
    }

    void DrawStatusRow(float x, float y, float width, float height, string label, string value, GUIStyle labelStyle, GUIStyle valueStyle)
    {
        GUI.Label(new Rect(x, y, 70f, height), label, labelStyle);
        GUI.Label(new Rect(x + 72f, y, width - 72f, height), value, valueStyle);
    }

    void DrawHealthBar(float x, float y, float width, float height, float ratio)
    {
        DrawRect(x, y, width, height, new Color(0.12f, 0.12f, 0.14f, 0.95f));
        DrawRect(x, y, width * Mathf.Clamp01(ratio), height, new Color(0.35f, 0.88f, 0.48f, 0.95f));
    }

    void DrawInventoryPanel(float x, float y, float width, float height)
    {
        var titleStyle = MakeLabelStyle(16, new Color(0.85f, 0.92f, 1f));
        var emptyStyle = MakeLabelStyle(12, new Color(0.65f, 0.68f, 0.72f));

        GUI.Label(new Rect(x, y, width, 24f), "인벤토리", titleStyle);

        const float slotSize = 56f;
        const float slotGap = 8f;
        float gridWidth = InventoryColumns * slotSize + (InventoryColumns - 1) * slotGap;
        float startX = x + (width - gridWidth) * 0.5f;
        float startY = y + 40f;

        var itemTypes = itemInventory.GetItemTypes();
        int slotIndex = 0;
        int totalSlots = InventoryColumns * InventoryRows;

        for (int i = 0; i < totalSlots; i++)
        {
            int col = i % InventoryColumns;
            int row = i / InventoryColumns;
            float slotX = startX + col * (slotSize + slotGap);
            float slotY = startY + row * (slotSize + slotGap);

            if (slotIndex < itemTypes.Count)
            {
                ItemType type = itemTypes[slotIndex];
                DrawItemSlot(slotX, slotY, slotSize, type, itemInventory.GetCount(type));
                slotIndex++;
            }
            else
            {
                DrawEmptySlot(slotX, slotY, slotSize);
            }
        }

        if (itemTypes.Count == 0)
        {
            GUI.Label(new Rect(x, startY + 3 * (slotSize + slotGap) + 8f, width, 20f),
                "아이템이 없습니다. 고철을 줍으면 여기에 표시됩니다.", emptyStyle);
        }
    }

    void DrawItemSlot(float x, float y, float size, ItemType type, int count)
    {
        DrawRect(x, y, size, size, new Color(0.12f, 0.14f, 0.18f, 0.95f));
        DrawRect(x, y, size, size, new Color(0.4f, 0.65f, 0.82f, 0.75f), false);

        Color itemColor = ItemDatabase.GetColor(type);
        DrawRect(x + 10f, y + 10f, size - 20f, size - 28f, itemColor);

        var nameStyle = MakeLabelStyle(10, Color.white);
        GUI.Label(new Rect(x, y + size - 20f, size, 16f), ItemDatabase.GetDisplayName(type), nameStyle);

        if (count > 1)
        {
            var countStyle = MakeLabelStyle(11, new Color(1f, 0.95f, 0.7f));
            GUI.Label(new Rect(x + size - 22f, y + 4f, 18f, 16f), count.ToString(), countStyle);
        }
    }

    void DrawEmptySlot(float x, float y, float size)
    {
        DrawRect(x, y, size, size, new Color(0.08f, 0.09f, 0.11f, 0.7f));
        DrawRect(x, y, size, size, new Color(0.22f, 0.26f, 0.3f, 0.45f), false);
    }

    static GUIStyle MakeLabelStyle(int fontSize, Color color)
    {
        return new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = fontSize,
            normal = { textColor = color },
        };
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
