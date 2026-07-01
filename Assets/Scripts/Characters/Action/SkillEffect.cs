using UnityEngine;

// Which stat a buff/debuff touches. Top-level so both BattleCharacter and SkillEffect
// can use it without an awkward SkillAction.StatType reference.
public enum StatType { Attack, Defense, Speed }

// One composable effect inside a SkillAction. A skill runs its list of effects against
// each target, so a single skill can — for example — deal Damage AND apply a Buff simply
// by holding two effects.
//
// Authoring skills (combining/tuning effects) is pure data — no code change. The only
// thing that touches this switch is adding a brand-new KIND of effect, which is rare and
// lives in exactly one place.
[System.Serializable]
public class SkillEffect
{
    public enum Kind { Damage, Heal, Buff, GuaranteeCrit }

    public Kind kind = Kind.Damage;

    [Header("Damage (Kind = Damage)")]
    [Tooltip("Percent of the user's Attack applied as damage (150 = 1.5x).")]
    public int attackScalePercent = 100;
    [Tooltip("Flat power added on top of the attack-scaled damage.")]
    public int power = 0;

    [Header("Heal (Kind = Heal)")]
    public int healAmount = 20;

    [Header("Buff / Debuff (Kind = Buff — use a negative amount to debuff)")]
    public StatType stat = StatType.Attack;
    public int statAmount = 5;
    [Tooltip("How many of the target's turns the buff/debuff lasts.")]
    public int duration = 1;

    // Applies this effect to a single target. `user` is the caster (needed for damage
    // scaling and self-buffs like Focus). Instant; the owning SkillAction handles pacing.
    public void Apply(BattleCharacter user, BattleCharacter target, IBattlePresenter presenter)
    {
        switch (kind)
        {
            case Kind.Damage:
            {
                // Scales from the user's Attack (doubled on a crit) plus flat power.
                bool isCrit;
                int atk = user.GetEffectiveAttack(out isCrit);
                int damage = Mathf.Max(1, (atk * attackScalePercent / 100) + power - (target.defense / 2));
                target.TakeDamage(damage);
                if (isCrit) presenter.ShowMessage("Critical hit!");
                presenter.ShowDamage(target, damage);
                break;
            }

            case Kind.Heal:
                target.Heal(healAmount);
                presenter.ShowHealing(target, healAmount);
                break;

            case Kind.Buff:
                target.ApplyBuff(stat, statAmount, duration);
                presenter.ShowMessage($"{target.CharacterName}'s {stat} {(statAmount >= 0 ? "rose" : "fell")}!");
                break;

            case Kind.GuaranteeCrit:
                user.GuaranteeNextCrit();
                presenter.ShowMessage($"{user.CharacterName} focuses! Next attack will be critical!");
                break;
        }
    }
}
