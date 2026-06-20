using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleCharacter : MonoBehaviour
{
    [Header("Character Info")]
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
    
    [Header("Visual")]
    public SpriteRenderer characterRenderer; // Replace Image with SpriteRenderer
    public Animator animator;

    [Header("Actions")]
    public List<BattleAction> availableActions = new List<BattleAction>();
    
    private bool isDefending = false;

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
        currentHP = maxHP;
        currentMP = maxMP;
        
        // Add basic actions
        if (availableActions.Count == 0)
        {
            // Add default attack action
            AttackAction basicAttack = ScriptableObject.CreateInstance<AttackAction>();
            basicAttack.actionName = "Attack";
            basicAttack.description = "Basic attack";
            basicAttack.targetType = TargetType.Enemy;
            availableActions.Add(basicAttack);
            
            // Add defend action
            DefendAction defend = ScriptableObject.CreateInstance<DefendAction>();
            defend.actionName = "Defend";
            defend.description = "Defend against attacks";
            defend.targetType = TargetType.Self;
            availableActions.Add(defend);
        }
    }
    
    public void SetupCharacter(CharStat stats)
    {
        CharacterName = stats.characterName;
        level = stats.level;
        maxHP = stats.maxHP;
        currentHP = stats.currentHP;
        maxMP = stats.maxMP;
        currentMP = stats.currentMP;
        attack = stats.attack;
        defense = stats.defense;
        speed = stats.speed;
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
        stats.characterId = CharacterName.ToLower().Replace(" ", "_");
        stats.characterName = CharacterName;
        stats.level = level;
        stats.maxHP = maxHP;
        stats.currentHP = currentHP;
        stats.maxMP = maxMP;
        stats.currentMP = currentMP;
        stats.attack = attack;
        stats.defense = defense;
        stats.speed = speed;
        
        return stats;
    }
}