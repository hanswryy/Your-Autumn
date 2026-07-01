using System.Collections.Generic;
using System.Linq;

// Builds and dispenses the per-round turn order for a battle.
//
// Octopath-style: at the start of each round every living actor (players + enemies)
// is gathered and sorted by Speed descending, then each acts once in that order.
// Because the order is rebuilt every round, Speed buffs/debuffs applied during a round
// only take effect on the NEXT round's ordering.
//
// This is a plain C# class (no MonoBehaviour needed) — BattleManager owns an instance.
public class TurnOrderHandler
{
    private readonly List<BattleCharacter> order = new List<BattleCharacter>();
    private int index;

    // The current round's order, exposed read-only for an optional turn-order UI.
    public IReadOnlyList<BattleCharacter> Order => order;

    // True once every actor in the current round has been handed out.
    public bool RoundComplete => index >= order.Count;

    // Rebuilds the order for a new round from the living members of both sides, sorted
    // by Speed (fastest first). Ties are broken deterministically: players before
    // enemies, then by the order they were passed in — OrderByDescending is a stable
    // sort, so the input order is preserved within equal Speed.
    public void BuildRound(IEnumerable<BattleCharacter> players, IEnumerable<BattleCharacter> enemies)
    {
        List<BattleCharacter> living = new List<BattleCharacter>();

        foreach (var p in players)
            if (p != null && p.IsAlive()) living.Add(p);
        foreach (var e in enemies)
            if (e != null && e.IsAlive()) living.Add(e);

        order.Clear();
        order.AddRange(living.OrderByDescending(c => c.speed));
        index = 0;
    }

    // Returns the next actor that can still act this round, skipping any that died or
    // became unable to act since the round began. Returns null when the round is over
    // (the caller then starts a new round via BuildRound).
    public BattleCharacter NextActor()
    {
        while (index < order.Count)
        {
            BattleCharacter actor = order[index];
            index++;
            if (actor != null && actor.CanAct())
                return actor;
        }
        return null;
    }
}
