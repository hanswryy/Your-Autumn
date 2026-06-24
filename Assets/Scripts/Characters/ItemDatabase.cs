using System.Collections.Generic;
using UnityEngine;

// Central registry of all item definitions.
// Create one asset at: Assets > Create > Items > Item Database
// Put it in a Resources folder named "ItemDatabase" for the auto-load fallback.
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Items/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [System.Serializable]
    public class StartingItem
    {
        public ItemDefinition item;
        public int quantity = 1;
    }

    [Header("All Items In Game")]
    public List<ItemDefinition> allItems = new List<ItemDefinition>();

    [Header("New Game Defaults")]
    public float startingCurrency = 0f;
    public List<StartingItem> startingItems = new List<StartingItem>();

    private Dictionary<string, ItemDefinition> lookup;

    public ItemDefinition GetItem(string itemId)
    {
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(itemId, out var def) ? def : null;
    }

    void BuildLookup()
    {
        lookup = new Dictionary<string, ItemDefinition>();
        foreach (var item in allItems)
        {
            if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
            lookup[item.itemId] = item;
        }
    }
}
