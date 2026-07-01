using System.Collections;
using UnityEngine;

public enum TargetType { Self, Ally, Enemy, AllAllies, AllEnemies }

public abstract class BattleAction : ScriptableObject
{
    public string actionName;
    public string description;
    public TargetType targetType;
    public int mpCost = 0;
    
    // `presenter` lets the action report results (messages, damage/heal numbers) without
    // depending on the concrete UI. BattleManager supplies it.
    public abstract IEnumerator Execute(BattleCharacter user, BattleCharacter target, IBattlePresenter presenter);
}