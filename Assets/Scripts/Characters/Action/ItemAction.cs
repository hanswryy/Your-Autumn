using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Action", menuName = "Battle/Actions/Item")]
public class ItemAction : BattleAction
{
    public ItemData item;
    
    public override IEnumerator Execute(BattleCharacter user, BattleCharacter target, IBattlePresenter presenter)
    {
        // Check if item exists in inventory
        if (GameState.Instance != null)
        {
            int itemIndex = GameState.Instance.playerData.inventoryItems.FindIndex(i => i.itemId == item.itemId);
            if (itemIndex < 0 || GameState.Instance.playerData.inventoryItems[itemIndex].quantity <= 0)
            {
                presenter.ShowMessage("No " + item.itemName + " left!");
                yield return new WaitForSeconds(1f);
                yield break;
            }

            // Use item (reduce quantity)
            GameState.Instance.RemoveItem(item.itemId, 1);
        }

        // Apply item effects
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

        yield return new WaitForSeconds(1f);
    }
}