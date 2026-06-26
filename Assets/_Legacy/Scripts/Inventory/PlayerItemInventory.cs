using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerItemInventory : MonoBehaviour
{
    [Serializable]
    public struct ItemEntry
    {
        public ItemType type;
        public int count;
    }

    readonly Dictionary<ItemType, int> itemCounts = new();

    public event Action InventoryChanged;

    public int GetCount(ItemType type)
    {
        return itemCounts.TryGetValue(type, out int count) ? count : 0;
    }

    public IReadOnlyList<ItemType> GetItemTypes()
    {
        var types = new List<ItemType>(itemCounts.Keys);
        types.Sort((a, b) => a.CompareTo(b));
        return types;
    }

    public void AddItem(ItemType type, int amount = 1)
    {
        if (amount <= 0)
            return;

        itemCounts.TryGetValue(type, out int current);
        itemCounts[type] = current + amount;
        InventoryChanged?.Invoke();
    }

    public bool RemoveItem(ItemType type, int amount = 1)
    {
        if (amount <= 0 || GetCount(type) < amount)
            return false;

        int remaining = GetCount(type) - amount;
        if (remaining <= 0)
            itemCounts.Remove(type);
        else
            itemCounts[type] = remaining;

        InventoryChanged?.Invoke();
        return true;
    }

    public List<ItemEntry> ExportEntries()
    {
        var entries = new List<ItemEntry>(itemCounts.Count);
        foreach ((ItemType type, int count) in itemCounts)
        {
            entries.Add(new ItemEntry
            {
                type = type,
                count = count,
            });
        }

        return entries;
    }

    public void ImportEntries(IReadOnlyList<ItemEntry> entries)
    {
        itemCounts.Clear();
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].count <= 0)
                    continue;

                itemCounts[entries[i].type] = entries[i].count;
            }
        }

        InventoryChanged?.Invoke();
    }
}
