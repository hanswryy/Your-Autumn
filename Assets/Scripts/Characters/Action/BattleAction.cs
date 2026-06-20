using System.Collections;
using UnityEngine;

public enum TargetType { Self, Ally, Enemy, AllAllies, AllEnemies }

public abstract class BattleAction : ScriptableObject
{
    public string actionName;
    public string description;
    public TargetType targetType;
    public int mpCost = 0;
    
    public abstract IEnumerator Execute(BattleCharacter user, BattleCharacter target);
}