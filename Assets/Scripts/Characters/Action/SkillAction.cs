using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Battle/Actions/Skill")]
public class SkillAction : BattleAction
{
    [Tooltip("Effects applied to each target, in order. Combine several (e.g. a Damage " +
             "effect + a Buff effect) for a skill that does more than one thing.")]
    public List<SkillEffect> effects = new List<SkillEffect>();

    public override IEnumerator Execute(BattleCharacter user, IReadOnlyList<BattleCharacter> targets, IBattlePresenter presenter)
    {
        // Check MP cost (charged once for the whole skill).
        if (user.currentMP < mpCost)
        {
            presenter.ShowMessage("Not enough MP!");
            yield return new WaitForSeconds(1f);
            yield break;
        }

        user.UseMP(mpCost);

        // Apply every effect to every target.
        foreach (var target in targets)
        {
            if (target == null) continue;
            foreach (var effect in effects)
                effect?.Apply(user, target, presenter);
        }

        yield return new WaitForSeconds(1f);
    }
}
