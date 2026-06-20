using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Attack", menuName = "Battle/Actions/Attack")]
public class AttackAction : BattleAction
{
    public int damageMultiplier = 100; // As percentage
    
    public override IEnumerator Execute(BattleCharacter user, BattleCharacter target)
    {
        // Calculate damage based on user's attack and target's defense
        int damage = Mathf.Max(1, (user.attack * damageMultiplier / 100) - (target.defense / 2));
        
        // Apply damage
        target.TakeDamage(damage);
        
        // Display damage
        BattleManager.Instance.uiController.ShowDamageNumber(target.transform.position, damage);
        
        yield return new WaitForSeconds(1f);
    }
}