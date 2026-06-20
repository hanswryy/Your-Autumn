using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Battle/Actions/Skill")]
public class SkillAction : BattleAction
{
    public enum SkillType { Damage, Heal, Buff, Debuff }
    
    public SkillType skillType;
    public int power = 20;
    
    public override IEnumerator Execute(BattleCharacter user, BattleCharacter target)
    {
        // Check MP cost
        if (user.currentMP < mpCost)
        {
            BattleManager.Instance.uiController.ShowBattleMessage("Not enough MP!");
            yield return new WaitForSeconds(1f);
            yield break;
        }
        
        // Use MP
        user.UseMP(mpCost);
        
        switch (skillType)
        {
            case SkillType.Damage:
                // Calculate damage
                int damage = Mathf.Max(1, user.attack + power - (target.defense / 2));
                target.TakeDamage(damage);
                BattleManager.Instance.uiController.ShowDamageNumber(target.transform.position, damage);
                break;
                
            case SkillType.Heal:
                // Calculate healing
                int healing = power;
                target.Heal(healing);
                BattleManager.Instance.uiController.ShowHealingNumber(target.transform.position, healing);
                break;
                
            case SkillType.Buff:
                // Implement buff logic
                break;
                
            case SkillType.Debuff:
                // Implement debuff logic
                break;
        }
        
        yield return new WaitForSeconds(1f);
    }
}