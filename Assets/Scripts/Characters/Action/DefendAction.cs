using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Defend", menuName = "Battle/Actions/Defend")]
public class DefendAction : BattleAction
{
    public override IEnumerator Execute(BattleCharacter user, IReadOnlyList<BattleCharacter> targets, IBattlePresenter presenter)
    {
        // Defend is inherently self-targeted.
        user.SetDefending(true);

        // Visual feedback
        if (user.animator != null)
        {
            user.animator.SetTrigger("Defend");
        }

        yield return new WaitForSeconds(0.5f);

        presenter.ShowMessage($"{user.CharacterName} is defending!");

        yield return new WaitForSeconds(1f);
    }
}
