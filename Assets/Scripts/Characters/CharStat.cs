using System;
using UnityEngine;

[System.Serializable]
public class CharStat
{
    public string characterId;
    public string characterName;
    public int level = 1;
    public int maxHP = 100;
    public int currentHP = 100;
    public int maxMP = 50;
    public int currentMP = 50;
    public int attack = 10;
    public int defense = 5;
    public int speed = 5;
    public int critChance = 5;
    public int experience = 0;
}