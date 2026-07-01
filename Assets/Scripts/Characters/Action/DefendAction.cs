using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Defend", menuName = "Battle/Actions/Defend")]
public class DefendAction : BattleAction
{
    public override IEnumerator Execute(BattleCharacter user, BattleCharacter target, IBattlePresenter presenter)
    {
        // Set defending status
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