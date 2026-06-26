using UnityEngine;

public static class ItemDatabase
{
    public static string GetDisplayName(ItemType type)
    {
        return type switch
        {
            ItemType.Scrap => "고철",
            ItemType.Potion => "포션",
            _ => "알 수 없음",
        };
    }

    public static string GetDescription(ItemType type)
    {
        return type switch
        {
            ItemType.Scrap => "녹슨 금속 조각. 제작 재료로 사용할 수 있다.",
            ItemType.Potion => "체력을 회복하는 소모성 아이템. Q키로 사용.",
            _ => "",
        };
    }

    public static Color GetColor(ItemType type)
    {
        return type switch
        {
            ItemType.Scrap => new Color(0.62f, 0.64f, 0.68f),
            ItemType.Potion => new Color(0.35f, 0.82f, 0.45f),
            _ => Color.gray,
        };
    }

    public static bool IsConsumable(ItemType type)
    {
        return type == ItemType.Potion;
    }

    public static float GetHealAmount(ItemType type)
    {
        return type switch
        {
            ItemType.Potion => 5f,
            _ => 0f,
        };
    }
}
