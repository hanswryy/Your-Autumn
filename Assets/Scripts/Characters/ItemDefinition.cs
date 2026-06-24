using UnityEngine;

// Standalone, designer-authored item template.
// Create assets via: Assets > Create > Items > Item Definition
[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public ItemType itemType;
    public Sprite icon;

    [Header("Shop")]
    public int buyPrice = 0;
    public bool sellable = true;

    [Header("Battle Use")]
    public bool usableInBattle = false;
    public int restoreHP = 0;
    public int restoreMP = 0;

    [Header("Equipment")]
    public int attackBonus = 0;
    public int defenseBonus = 0;
    public int speedBonus = 0;

    // Build a runtime/save-serializable instance from this template.
    public ItemData CreateInstance(int quantity = 1)
    {
        return new ItemData
        {
            itemId         = itemId,
            itemName       = itemName,
            description    = description,
            itemType       = itemType,
            quantity       = quantity,
            usableInBattle = usableInBattle,
            restoreHP      = restoreHP,
            restoreMP      = restoreMP,
            attackBonus    = attackBonus,
            defenseBonus   = defenseBonus,
            speedBonus     = speedBonus,
        };
    }
}
