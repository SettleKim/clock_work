using UnityEngine;
using UnityEngine.InputSystem;

public class TradeUI : MonoBehaviour
{
    const float PanelWidth = 300f;
    const float PanelHeight = 210f;

    static TradeUI instance;

    NpcMerchant activeMerchant;
    PlayerItemInventory activeInventory;
    string statusMessage = "";
    float statusTimer;
    int openedFrame;

    public static bool IsTradeOpen { get; private set; }

    public static void OpenTrade(NpcMerchant merchant, PlayerItemInventory inventory)
    {
        if (merchant == null || inventory == null)
            return;

        if (instance == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                instance = player.GetComponent<TradeUI>();
            if (instance == null && player != null)
                instance = player.AddComponent<TradeUI>();
        }

        if (instance == null)
            return;

        instance.activeMerchant = merchant;
        instance.activeInventory = inventory;
        instance.statusMessage = "안녕! 고철을 포션으로 바꿔줄게.";
        instance.statusTimer = 3f;
        instance.openedFrame = Time.frameCount;
        IsTradeOpen = true;
    }

    public static void CloseTrade()
    {
        IsTradeOpen = false;
        if (instance != null)
            instance.activeMerchant = null;
    }

    void Update()
    {
        if (!IsTradeOpen)
            return;

        if (statusTimer > 0f)
            statusTimer -= Time.unscaledDeltaTime;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame && Time.frameCount > openedFrame)
            CloseTrade();

        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            TryTrade();
    }

    void TryTrade()
    {
        if (activeMerchant == null || activeInventory == null)
            return;

        if (activeMerchant.TryTrade(activeInventory))
        {
            statusMessage = "거래 완료!";
            statusTimer = 2f;
        }
        else
            statusMessage = "고철이 부족해.";
    }

    void OnGUI()
    {
        if (!IsTradeOpen || activeMerchant == null)
            return;

        float x = Screen.width * 0.5f - PanelWidth * 0.5f;
        float y = Screen.height * 0.5f - PanelHeight * 0.5f;

        DrawRect(x, y, PanelWidth, PanelHeight, new Color(0.06f, 0.08f, 0.1f, 0.95f));
        DrawRect(x, y, PanelWidth, PanelHeight, new Color(0.35f, 0.55f, 0.7f, 0.85f), false);

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
        };
        var bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
        };

        GUI.Label(new Rect(x + 16f, y + 12f, PanelWidth - 32f, 24f), "NPC 거래", titleStyle);
        GUI.Label(new Rect(x + 16f, y + 40f, PanelWidth - 32f, 60f), statusMessage, bodyStyle);

        string costName = ItemDatabase.GetDisplayName(activeMerchant.CostItem);
        string rewardName = ItemDatabase.GetDisplayName(activeMerchant.RewardItem);
        GUI.Label(
            new Rect(x + 16f, y + 88f, PanelWidth - 32f, 48f),
            $"{costName} {activeMerchant.CostAmount}개 → {rewardName} {activeMerchant.RewardAmount}개\n" +
            $"보유 고철: {activeInventory.GetCount(ItemType.Scrap)}   포션: {activeInventory.GetCount(ItemType.Potion)}",
            bodyStyle);

        const float buttonWidth = 120f;
        const float buttonHeight = 32f;
        float buttonX = x + PanelWidth * 0.5f - buttonWidth * 0.5f;
        float buttonY = y + PanelHeight - buttonHeight - 16f;

        DrawRect(buttonX, buttonY, buttonWidth, buttonHeight, new Color(0.18f, 0.28f, 0.36f, 0.98f));
        if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "거래 (Enter)"))
            TryTrade();
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
