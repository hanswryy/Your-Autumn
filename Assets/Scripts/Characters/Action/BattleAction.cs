using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TargetType { Self, Ally, Enemy, AllAllies, AllEnemies }

public abstract class BattleAction : ScriptableObject
{
    public string actionName;
    public string description;
    public TargetType targetType;
    public int mpCost = 0;

    // Runs against every resolved target (one for single-target moves, many for AoE).
    // `presenter` reports results without coupling the action to concrete UI.
    public abstract IEnumerator Execute(BattleCharacter user, IReadOnlyList<BattleCharacter> targets, IBattlePresenter presenter);
}
