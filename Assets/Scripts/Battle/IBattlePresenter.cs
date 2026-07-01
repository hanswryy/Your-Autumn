// Presentation abstraction that battle actions talk to. BattleManager passes an
// implementation (BattleUIController) into each action's Execute, so actions depend on
// this interface only
public interface IBattlePresenter
{
    void ShowMessage(string message);
    void ShowDamage(BattleCharacter target, int amount);
    void ShowHealing(BattleCharacter target, int amount);
    void ShowMPRestored(BattleCharacter target, int amount);
}
