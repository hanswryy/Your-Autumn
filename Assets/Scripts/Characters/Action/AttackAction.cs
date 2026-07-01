using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Attack", menuName = "Battle/Actions/Attack")]
public class AttackAction : BattleAction
{
    public int damageMultiplier = 100; // As percentage
    
    public override IEnumerator Execute(BattleCharacter user, BattleCharacter target, IBattlePresenter presenter)
    {
        // Roll for a critical hit, which doubles the attacker's attack stat.
        bool isCrit;
        int atk = user.GetEffectiveAttack(out isCrit);

        // Calculate damage based on (possibly doubled) attack and target's defense
        int damage = Mathf.Max(1, (atk * damageMultiplier / 100) - (target.defense / 2));

        // Apply damage
        target.TakeDamage(damage);

        // Display damage (announce the crit first, if any)
        if (isCrit)
            presenter.ShowMessage("Critical hit!");
        presenter.ShowDamage(target, damage);

        yield return new WaitForSeconds(1f);
    }
}