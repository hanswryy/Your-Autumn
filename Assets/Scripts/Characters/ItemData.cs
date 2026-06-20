using UnityEngine;

public enum ItemType { Consumable, Equipment, KeyItem }

[System.Serializable]
public class ItemData
{
    public string itemId;
    public string itemName;
    public string description;
    public ItemType itemType;
    public int quantity = 1;
    
    // Battle item properties
    public bool usableInBattle = false;
    public int restoreHP = 0;
    public int restoreMP = 0;
    
    // Equipment properties
    public int attackBonus = 0;
    public int defenseBonus = 0;
    public int speedBonus = 0;
}