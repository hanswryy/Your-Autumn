// Presentation abstraction that battle actions talk to, instead of reaching into a
// concrete UI singleton (BattleManager.Instance.uiController). BattleManager passes an
// implementation (BattleUIController) into each action's Execute, so actions depend on
// this interface only — they stay decoupled from the UI and are unit-testable with a
// mock presenter.
public interface IBattlePresenter
{
    void ShowMessage(string message);
    void ShowDamage(BattleCharacter target, int amount);
    void ShowHealing(BattleCharacter target, int amount);
    void ShowMPRestored(BattleCharacter target, int amount);
}
