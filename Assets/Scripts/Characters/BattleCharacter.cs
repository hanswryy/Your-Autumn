using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleCharacter : MonoBehaviour
{
    [Header("Character Info")]
    public string characterId;
    public string CharacterName;
    public int level = 1;
    public bool isEnemy = false;
    
    [Header("Stats")]
    public int maxHP = 100;
    public int currentHP;
    public int maxMP = 50;
    public int currentMP;
    public int attack = 10;
    public int defense = 5;
    public int speed = 5;
    public int critChance = 5; // % chance an attack crits (doubles this character's attack)
    
    [Header("Visual")]
    public SpriteRenderer characterRenderer; // Replace Image with SpriteRenderer
    public Animator animator;

    [Header("Actions")]
    public List<BattleAction> availableActions = new List<BattleAction>();
    
    private bool isDefending = false;
    private bool actionsInitialized = false;

    // Base (un-buffed) combat stats so in-battle buffs/debuffs don't get saved permanently.
    private int baseAttack;
    private int baseDefense;
    private int baseSpeed;
    private bool baseStatsCaptured = false;

    // Temporary, turn-limited stat changes applied by skills.
    private class ActiveBuff
    {
        public SkillAction.StatType stat;
        public int amount;
        public int turnsRemaining;
    }
    private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    // When set (e.g. by Focus), this character's next attack is a guaranteed critical.
    private bool guaranteedCrit = false;

    // True once HP/MP have been loaded from a CharStat (or a default setup), so Start()
    // doesn't overwrite the loaded values with full HP/MP.
    private bool statsInitialized = false;

    void Awake()
    {
        // Get animator component
        animator = GetComponent<Animator>();
    }

    // Add this method to trigger attack animation
    public void PlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("attack");
            Debug.Log($"{CharacterName} playing attack animation");
        }
    }

    
    void Start()
    {
        // Only fill HP/MP for characters that weren't set up from saved data (e.g. enemies).
        // Player characters get their current HP/MP from SetupCharacter and must not be reset.
        if (!statsInitialized)
        {
            currentHP = maxHP;
            currentMP = maxMP;
            statsInitialized = true;
        }

        // Make sure this character has its actions (no-op if already set up).
        InitializeActions();
        CaptureBaseStats();
    }

    // Records the un-buffed combat stats once, so temporary battle buffs aren't persisted.
    private void CaptureBaseStats()
    {
        if (baseStatsCaptured)
            return;

        baseAttack = attack;
        baseDefense = defense;
        baseSpeed = speed;
        baseStatsCaptured = true;
    }

    // Builds the action list: default Attack/Defend plus any character-specific skills.
    // Safe to call multiple times and regardless of Awake/Start ordering.
    public void InitializeActions()
    {
        if (actionsInitialized)
            return;

        // Add default Attack (index 0) and Defend (index 1) if not already present.
        if (availableActions.Count == 0)
        {
            AttackAction basicAttack = ScriptableObject.CreateInstance<AttackAction>();
            basicAttack.actionName = "Attack";
            basicAttack.description = "Basic attack";
            basicAttack.targetType = TargetType.Enemy;
            availableActions.Add(basicAttack);

            DefendAction defend = ScriptableObject.CreateInstance<DefendAction>();
            defend.actionName = "Defend";
            defend.description = "Defend against attacks";
            defend.targetType = TargetType.Self;
            availableActions.Add(defend);
        }

        // Add character-specific skills based on the character's id.
        AddCharacterSkills();

        actionsInitialized = true;
    }

    private void AddCharacterSkills()
    {
        switch (characterId)
        {
            case "hero":
                // Strike: efficient attack that scales from the attack stat.
                SkillAction strike = ScriptableObject.CreateInstance<SkillAction>();
                strike.actionName = "Strike";
                strike.description = "A focused blow dealing 1.5x Attack.";
                strike.targetType = TargetType.Enemy;
                strike.skillType = SkillAction.SkillType.Damage;
                strike.mpCost = 5;
                strike.attackScalePercent = 150; // hits for 1.5x the user's attack
                strike.power = 0;
                availableActions.Add(strike);

                // Focus: makes this character's next attack a guaranteed critical hit.
                SkillAction focus = ScriptableObject.CreateInstance<SkillAction>();
                focus.actionName = "Focus";
                focus.description = "Concentrate so your next attack is a guaranteed critical.";
                focus.targetType = TargetType.Self;
                focus.skillType = SkillAction.SkillType.GuaranteeCrit;
                focus.mpCost = 8;
                availableActions.Add(focus);
                break;

            case "cecil":
                // Shield: raises the chosen ally's Defense (can target self).
                SkillAction shield = ScriptableObject.CreateInstance<SkillAction>();
                shield.actionName = "Shield";
                shield.description = "Bolster an ally's Defense.";
                shield.targetType = TargetType.Ally;
                shield.skillType = SkillAction.SkillType.Buff;
                shield.mpCost = 10;
                shield.affectedStat = SkillAction.StatType.Defense;
                shield.statAmount = 5;
                shield.duration = 2; // lasts through the target's next two turns
                availableActions.Add(shield);
                break;
        }
    }

    public void SetupCharacter(CharStat stats)
    {
        characterId = stats.characterId;
        CharacterName = stats.characterName;
        level = stats.level;
        maxHP = stats.maxHP;
        currentHP = stats.currentHP;
        maxMP = stats.maxMP;
        currentMP = stats.currentMP;
        attack = stats.attack;
        defense = stats.defense;
        speed = stats.speed;
        critChance = stats.critChance;
        statsInitialized = true; // keep the loaded HP/MP; don't let Start() reset them

        // Build actions now that we know which character this is.
        InitializeActions();
        CaptureBaseStats();
    }

    public void SetupDefaultCharacter(string name)
    {
        CharacterName = name;
        level = 1;
        maxHP = 100;
        currentHP = 100;
        maxMP = 50;
        currentMP = 50;
        attack = 10;
        defense = 5;
        speed = 5;
        statsInitialized = true;

        CaptureBaseStats();
    }

    public BattleAction SelectAction()
    {
        if (isEnemy)
        {
            // Simple AI: just use basic attack for now
            return availableActions[0];
        }
        
        // Player actions are selected via UI
        return null;
    }
    
    public bool IsAlive()
    {
        return currentHP > 0;
    }
    
    public bool CanAct()
    {
        return IsAlive();
    }
    
    public void TakeDamage(int damage)
    {
        // Apply defense if defending
        if (isDefending)
        {
            damage = Mathf.Max(1, damage / 2);
            isDefending = false;
        }
        
        currentHP = Mathf.Max(0, currentHP - damage);

        // If this is an enemy and they're defeated, make them inactive
        if (isEnemy && currentHP <= 0)
        {
            // Add a slight delay before deactivating to show the death
            StartCoroutine(DeactivateAfterDelay(0.5f));
        }
        
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    private IEnumerator DeactivateAfterDelay(float delay)
    {
        // Wait for animations/effects to finish
        yield return new WaitForSeconds(delay);

        // Make the enemy visually inactive but keep the component active
        // for battle logic to complete properly
        Debug.Log("dead");
        this.gameObject.SetActive(false);
    }

    // Applies a stat change right now and tracks it so it expires after `duration` of this
    // character's own turns. Positive amount = buff, negative = debuff.
    public void ApplyBuff(SkillAction.StatType stat, int amount, int duration)
    {
        ModifyStat(stat, amount);
        activeBuffs.Add(new ActiveBuff { stat = stat, amount = amount, turnsRemaining = duration });
    }

    // Called at the start of this character's turn: counts down buffs and reverts expired ones
    // before the character acts.
    public void TickBuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].turnsRemaining--;
            if (activeBuffs[i].turnsRemaining <= 0)
            {
                ModifyStat(activeBuffs[i].stat, -activeBuffs[i].amount);
                activeBuffs.RemoveAt(i);
            }
        }
    }

    private void ModifyStat(SkillAction.StatType stat, int amount)
    {
        switch (stat)
        {
            case SkillAction.StatType.Attack:
                attack = Mathf.Max(0, attack + amount);
                break;
            case SkillAction.StatType.Defense:
                defense = Mathf.Max(0, defense + amount);
                break;
            case SkillAction.StatType.Speed:
                speed = Mathf.Max(0, speed + amount);
                break;
        }
    }

    // Makes this character's next attack a guaranteed critical hit (used by Focus).
    public void GuaranteeNextCrit()
    {
        guaranteedCrit = true;
    }

    // Resolves the attack power for a single attack: rolls (or forces) a critical,
    // which doubles the attack stat. Consumes any pending guaranteed-crit flag.
    public int GetEffectiveAttack(out bool isCritical)
    {
        isCritical = guaranteedCrit || (Random.Range(0, 100) < critChance);
        guaranteedCrit = false; // a guaranteed crit is spent on this attack
        return isCritical ? attack * 2 : attack;
    }

    public void SetDefending(bool defending)
    {
        isDefending = defending;
    }
    
    public void UseMP(int amount)
    {
        currentMP = Mathf.Max(0, currentMP - amount);
    }
    
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }
    
    public CharStat GetCharacterStats()
    {
        CharStat stats = new CharStat();
        stats.characterId = !string.IsNullOrEmpty(characterId)
            ? characterId
            : CharacterName.ToLower().Replace(" ", "_");
        stats.characterName = CharacterName;
        stats.level = level;
        stats.maxHP = maxHP;
        stats.currentHP = currentHP;
        stats.maxMP = maxMP;
        stats.currentMP = currentMP;
        // Persist the base stats so temporary in-battle buffs/debuffs don't accumulate in saves.
        stats.attack = baseStatsCaptured ? baseAttack : attack;
        stats.defense = baseStatsCaptured ? baseDefense : defense;
        stats.speed = baseStatsCaptured ? baseSpeed : speed;
        stats.critChance = critChance;

        return stats;
    }
}