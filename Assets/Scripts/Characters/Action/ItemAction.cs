using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Action", menuName = "Battle/Actions/Item")]
public class ItemAction : BattleAction
{
    public ItemData item;

    public override IEnumerator Execute(BattleCharacter user, IReadOnlyList<BattleCharacter> targets, IBattlePresenter presenter)
    {
        // Check the item is still in the inventory, and consume one (once, not per target).
        if (GameState.Instance != null)
        {
            int itemIndex = GameState.Instance.playerData.inventoryItems.FindIndex(i => i.itemId == item.itemId);
            if (itemIndex < 0 || GameState.Instance.playerData.inventoryItems[itemIndex].quantity <= 0)
            {
                presenter.ShowMessage("No " + item.itemName + " left!");
                yield return new WaitForSeconds(1f);
                yield break;
            }

            GameState.Instance.RemoveItem(item.itemId, 1);
        }

        // Apply item effects to each target.
        foreach (var target in targets)
        {
            if (target == null) continue;

            if (item.restoreHP > 0)
            {
                target.Heal(item.restoreHP);
                presenter.ShowHealing(target, item.restoreHP);
            }

            if (item.restoreMP > 0)
            {
                int newMP = Mathf.Min(target.maxMP, target.currentMP + item.restoreMP);
                int mpRestored = newMP - target.currentMP;
                target.currentMP = newMP;

                presenter.ShowMPRestored(target, mpRestored);
            }
        }

        yield return new WaitForSeconds(1f);
    }
}
