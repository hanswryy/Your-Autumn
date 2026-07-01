using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Battle/Actions/Skill")]
public class SkillAction : BattleAction
{
    public enum SkillType { Damage, Heal, Buff, Debuff, GuaranteeCrit }
    public enum StatType { Attack, Defense, Speed }

    public SkillType skillType;
    public int power = 20;

    [Header("Damage scaling")]
    [Tooltip("Percentage of the user's attack stat added to damage skills (100 = 1x attack).")]
    public int attackScalePercent = 100;

    [Header("Buff / Debuff")]
    [Tooltip("Which stat a Buff/Debuff skill modifies.")]
    public StatType affectedStat = StatType.Attack;
    [Tooltip("How much the stat changes (positive for Buff, magnitude for Debuff).")]
    public int statAmount = 5;
    [Tooltip("How many of the affected character's turns the buff/debuff lasts before wearing off.")]
    public int duration = 1;

    public override IEnumerator Execute(BattleCharacter user, BattleCharacter target, IBattlePresenter presenter)
    {
        // Check MP cost
        if (user.currentMP < mpCost)
        {
            presenter.ShowMessage("Not enough MP!");
            yield return new WaitForSeconds(1f);
            yield break;
        }

        // Use MP
        user.UseMP(mpCost);

        switch (skillType)
        {
            case SkillType.Damage:
                // Damage scales from the user's attack stat (doubled on a critical) plus flat power.
                bool isCrit;
                int atk = user.GetEffectiveAttack(out isCrit);
                int damage = Mathf.Max(1, (atk * attackScalePercent / 100) + power - (target.defense / 2));
                target.TakeDamage(damage);
                if (isCrit)
                    presenter.ShowMessage("Critical hit!");
                presenter.ShowDamage(target, damage);
                break;

            case SkillType.Heal:
                // Calculate healing
                int healing = power;
                target.Heal(healing);
                presenter.ShowHealing(target, healing);
                break;

            case SkillType.Buff:
                target.ApplyBuff(affectedStat, statAmount, duration);
                presenter.ShowMessage($"{target.CharacterName}'s {affectedStat} rose!");
                break;

            case SkillType.Debuff:
                target.ApplyBuff(affectedStat, -statAmount, duration);
                presenter.ShowMessage($"{target.CharacterName}'s {affectedStat} fell!");
                break;

            case SkillType.GuaranteeCrit:
                user.GuaranteeNextCrit();
                presenter.ShowMessage($"{user.CharacterName} focuses! Next attack will be critical!");
                break;
        }

        yield return new WaitForSeconds(1f);
    }
}