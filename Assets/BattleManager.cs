using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum BattleTurnState { Start, PlayerSelect, PlayerAction, EnemyAction, Won, Lost }

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    
    [Header("References")]
    public BattleUIController uiController;
    public Transform playerPartyContainer;
    public Transform enemyPartyContainer;
    
    [Header("Battle Settings")]
    public GameObject[] playerCharacterPrefabs;
    public GameObject[] enemyPrefabs;
    public int minEnemies = 1;
    public int maxEnemies = 3;
    
    [Header("Audio")]
    public AudioClip battleMusic;
    public AudioClip victoryMusic;

    [Header("Cinematics")]
    [Tooltip("Camera used for the battle. Falls back to Camera.main if left empty.")]
    public Camera battleCamera;
    [Tooltip("Where the active player character stands during their turn. Falls back to centerFallback if empty.")]
    public Transform centerPoint;
    [Tooltip("World position used as the turn 'center' when no centerPoint Transform is assigned.")]
    public Vector3 centerFallback = new Vector3(4f, 1.6f, 1.035f);
    [Tooltip("Movement speed (units/second) when characters walk to/from their cinematic positions.")]
    public float moveSpeed = 14f;
    [Tooltip("How far in front of the target the attacker stops.")]
    public float frontOffset = 2.5f;
    [Tooltip("Camera field of view while zoomed in on the active player character.")]
    public float zoomedFOV = 50f;
    [Tooltip("Field of view change speed (degrees/second).")]
    public float zoomSpeed = 60f;

    private float defaultFOV;
    private Vector3 actorOriginalPosition;

    private List<BattleCharacter> playerParty = new List<BattleCharacter>();
    private List<BattleCharacter> enemyParty = new List<BattleCharacter>();
    private BattleTurnState state;
    private BattleCharacter currentActor;
    private BattleCharacter currentTarget;
    private BattleAction currentAction;
    private int currentPartyMemberIndex = 0;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Resolve the camera and remember its default field of view for zoom in/out.
        if (battleCamera == null)
        {
            battleCamera = Camera.main;
        }
        if (battleCamera != null)
        {
            defaultFOV = battleCamera.fieldOfView;
        }

        state = BattleTurnState.Start;
        StartCoroutine(SetupBattle());
    }
    
    IEnumerator SetupBattle()
    {
        // Setup UI
        uiController.SetupBattleUI();
        
        // Create player characters
        SetupPlayerParty();
        
        // Create random number of enemies
        SetupEnemyParty();
        
        // Show battle start UI
        uiController.ShowBattleMessage("Battle Start!");
        
        yield return new WaitForSeconds(2f);
        
        // Start first turn
        state = BattleTurnState.PlayerSelect;
        StartPlayerTurn();
    }
    
    void SetupPlayerParty()
    {
        // Create characters from GameState data
        if (GameState.Instance != null)
        {
            Debug.Log("Setting up player party from GameState data");
            // Count how many characters we'll create for spacing calculation
            int characterCount = Mathf.Min(2, GameState.Instance.playerData.partyMembers.Count);
            float spacing = 5f;
            float startY = -(characterCount - 1) * spacing * 0.5f;
            
            for (int i = 0; i < characterCount; i++)
            {
                // Get character data
                CharStat charStats = GameState.Instance.playerData.partyMembers[i];
                
                // Select the appropriate prefab by index or ID
                GameObject prefab = playerCharacterPrefabs[i % playerCharacterPrefabs.Length];
                
                // Instantiate and setup
                GameObject playerObj = Instantiate(prefab, playerPartyContainer);
                
                // Position with spacing
                playerObj.transform.localPosition = new Vector3(0, 0, startY + i * spacing);

                BattleCharacter character = playerObj.GetComponent<BattleCharacter>();
                character.SetupCharacter(charStats);
                
                playerParty.Add(character);
            }
        }
        else
        {
            // Fallback for testing: create 2 default characters
            float spacing = 5f;
            float startY = -0.5f * spacing; // For 2 characters centered

            for (int i = 0; i < 2 && i < playerCharacterPrefabs.Length; i++)
            {
                GameObject playerObj = Instantiate(playerCharacterPrefabs[i], playerPartyContainer);
                
                // Position with spacing
                playerObj.transform.localPosition = new Vector3(0, 0, startY + i * spacing);

                BattleCharacter character = playerObj.GetComponent<BattleCharacter>();
                character.SetupDefaultCharacter($"Player {i+1}");
                playerParty.Add(character);
            }
        }
        
        // Setup UI for player characters
        uiController.SetupPartyUI(playerParty);
    }
    
    void SetupEnemyParty()
    {
        // Random number of enemies
        int enemyCount = Random.Range(minEnemies, maxEnemies + 1);
        
        float spacing = 5f;
        float startY = -(enemyCount - 1) * spacing * 0.5f; // Changed X to Y
        
        for (int i = 0; i < enemyCount; i++)
        {
            // Select random enemy prefab
            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            GameObject enemyObj = Instantiate(enemyPrefab, enemyPartyContainer);
            
            // Position with vertical spacing - modified to use Y instead of X
            enemyObj.transform.localPosition = new Vector3(0, 0, startY + i * spacing);
            
            BattleCharacter enemyCharacter = enemyObj.GetComponent<BattleCharacter>();
            enemyCharacter.isEnemy = true;
            enemyParty.Add(enemyCharacter);
        }
        
        // Setup UI for enemies
        uiController.SetupEnemyUI(enemyParty);
    }
    
    void StartPlayerTurn()
    {
        if (currentPartyMemberIndex >= playerParty.Count)
        {
            currentPartyMemberIndex = 0;
            state = BattleTurnState.EnemyAction;
            StartCoroutine(EnemyTurn());
            return;
        }
        
        currentActor = playerParty[currentPartyMemberIndex];

        // Skip turn if character is incapacitated
        if (!currentActor.CanAct())
        {
            currentPartyMemberIndex++;
            StartPlayerTurn();
            return;
        }

        // Move the active character to the center, zoom in, then show the action menu.
        StartCoroutine(BeginPlayerTurn());
    }

    IEnumerator BeginPlayerTurn()
    {
        // Expire any buffs that have run their course before this character acts.
        currentActor.TickBuffs();

        // Remember where this character started so we can send them back later.
        actorOriginalPosition = currentActor.transform.position;

        Vector3 center = centerPoint != null ? centerPoint.position : centerFallback;

        // Walk to center and zoom in on the character at the same time.
        StartCoroutine(ZoomCamera(zoomedFOV));
        yield return MoveCharacterTo(currentActor.transform, center);

        // Show action selection UI for this character
        uiController.ShowActionMenu(currentActor);
    }
    
    public void OnActionSelected(BattleAction action)
    {
        currentAction = action;

        // Self-targeted actions (Defend, Focus, ...) don't need a target menu.
        if (action.targetType == TargetType.Self)
        {
            currentTarget = currentActor;
            uiController.ShowBattleView();
            state = BattleTurnState.PlayerAction;
            StartCoroutine(PerformAction());
            return;
        }

        // Determine valid targets for this action
        List<BattleCharacter> validTargets = GetValidTargets(action);

        // Show target selection UI
        uiController.ShowTargetMenu(validTargets);
    }
    
    public void OnTargetSelected(BattleCharacter target)
    {
        currentTarget = target;
        
        // Execute action
        state = BattleTurnState.PlayerAction;
        StartCoroutine(PerformAction());
    }
    
    private List<BattleCharacter> GetValidTargets(BattleAction action)
    {
        List<BattleCharacter> validTargets = new List<BattleCharacter>();
        
        if (action.targetType == TargetType.Enemy)
        {
            // For attacks, skills targeting enemies
            foreach (var enemy in enemyParty)
            {
                if (enemy.IsAlive())
                {
                    validTargets.Add(enemy);
                }
            }
        }
        else if (action.targetType == TargetType.Ally)
        {
            // For healing, buffs targeting allies
            foreach (var ally in playerParty)
            {
                validTargets.Add(ally);
            }
        }
        else if (action.targetType == TargetType.AllEnemies)
        {
            // For AOE attacks, return first enemy as reference
            if (enemyParty.Count > 0)
            {
                validTargets.Add(enemyParty[0]);
            }
        }
        else if (action.targetType == TargetType.AllAllies)
        {
            // For AOE healing, return first ally as reference
            if (playerParty.Count > 0)
            {
                validTargets.Add(playerParty[0]);
            }
        }
        
        return validTargets;
    }
    
    IEnumerator PerformAction()
    {
        StartCoroutine(ZoomCamera(defaultFOV));
        // Display action message
        uiController.ShowBattleMessage($"{currentActor.CharacterName} uses {currentAction.actionName}!");

        yield return new WaitForSeconds(0.5f);

        // For an attack (or damaging skill), walk up to the chosen enemy before swinging.
        if (IsApproachAttack(currentAction) && currentTarget != null)
        {
            yield return MoveCharacterTo(currentActor.transform, GetFrontPosition(currentActor, currentTarget));
            currentActor.PlayAttackAnimation();
            yield return new WaitForSeconds(0.4f);
            StartCoroutine(CameraShake(0.2f, 0.3f));
        }

        // Execute action logic
        yield return currentAction.Execute(currentActor, currentTarget);

        // Walk the character back to where they started while the camera zooms back out.
        yield return MoveCharacterTo(currentActor.transform, actorOriginalPosition);

        // Check for battle end conditions
        if (CheckBattleEnd())
            yield break;

        // Move to next character
        currentPartyMemberIndex++;
        state = BattleTurnState.PlayerSelect;
        StartPlayerTurn();
    }
    
    IEnumerator EnemyTurn()
    {
        foreach (var enemy in enemyParty)
        {
            if (!enemy.IsAlive())
                continue;
                
            yield return new WaitForSeconds(1f);
            
            // Expire any buffs that have run their course before this enemy acts.
            enemy.TickBuffs();

            // Enemy selects action and target
            currentActor = enemy;
            currentAction = enemy.SelectAction();
            
            // Choose target based on action type
            if (currentAction.targetType == TargetType.Enemy || currentAction.targetType == TargetType.AllEnemies)
            {
                // Enemy targeting players
                List<BattleCharacter> aliveParty = playerParty.FindAll(p => p.IsAlive());
                if (aliveParty.Count > 0)
                {
                    currentTarget = aliveParty[Random.Range(0, aliveParty.Count)];
                }
            }
            else
            {
                // Enemy targeting other enemies (for healing, etc)
                List<BattleCharacter> aliveEnemies = enemyParty.FindAll(e => e.IsAlive());
                if (aliveEnemies.Count > 0)
                {
                    currentTarget = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
                }
            }

            // Announce the enemy action.
            uiController.ShowBattleMessage($"{currentActor.CharacterName} uses {currentAction.actionName}!");

            yield return new WaitForSeconds(0.5f);

            // Remember the enemy's spot, then approach the target for an attack.
            Vector3 enemyOrigin = enemy.transform.position;

            if (IsApproachAttack(currentAction) && currentTarget != null)
            {
                yield return MoveCharacterTo(enemy.transform, GetFrontPosition(enemy, currentTarget));
                currentActor.PlayAttackAnimation();
                yield return new WaitForSeconds(0.4f);
                StartCoroutine(CameraShake(0.2f, 0.3f));
            }

            // Execute enemy action
            yield return currentAction.Execute(currentActor, currentTarget);

            // Send the enemy back to where it started.
            yield return MoveCharacterTo(enemy.transform, enemyOrigin);

            // Check for battle end
            if (CheckBattleEnd())
                yield break;
        }
        
        // Back to player turn
        currentPartyMemberIndex = 0;
        state = BattleTurnState.PlayerSelect;
        StartPlayerTurn();
    }
    
    bool CheckBattleEnd()
    {
        // Check if all enemies are defeated
        bool allEnemiesDefeated = true;
        foreach (var enemy in enemyParty)
        {
            if (enemy.IsAlive())
            {
                allEnemiesDefeated = false;
                break;
            }
        }
        
        if (allEnemiesDefeated)
        {
            state = BattleTurnState.Won;
            StartCoroutine(EndBattle(true));
            return true;
        }
        
        // Check if all players are defeated
        bool allPlayersDefeated = true;
        foreach (var player in playerParty)
        {
            if (player.IsAlive())
            {
                allPlayersDefeated = false;
                break;
            }
        }
        
        if (allPlayersDefeated)
        {
            state = BattleTurnState.Lost;
            StartCoroutine(EndBattle(false));
            return true;
        }
        
        return false;
    }
    IEnumerator CameraShake(float duration, float magnitude)
    {
        Vector3 originalPosition = battleCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            battleCamera.transform.position = originalPosition + Random.insideUnitSphere * magnitude;
            yield return null;
        }

        battleCamera.transform.position = originalPosition;
    }

    IEnumerator EndBattle(bool victory)
    {
        if (victory)
        {
            uiController.ShowBattleMessage("Victory!");
            // Play victory music
            // AudioManager.Instance.PlayMusic(victoryMusic);

            // Calculate rewards
            yield return new WaitForSeconds(2f);

            uiController.ShowVictoryPanel();

            // Save updated character stats
            if (GameState.Instance != null)
            {
                for (int i = 0; i < playerParty.Count && i < GameState.Instance.playerData.partyMembers.Count; i++)
                {
                    CharStat updatedStat = playerParty[i].GetCharacterStats();
                    GameState.Instance.AddPartyMember(updatedStat);

                }
                GameState.Instance.SaveGame();

            }

        }
        else
        {
            uiController.ShowBattleMessage("Defeat...");

            yield return new WaitForSeconds(3f);

            // Game over or return to last checkpoint
            uiController.ShowDefeatPanel();
        }
        foreach (var member in GameState.Instance.playerData.partyMembers)
        {
            Debug.Log($"Verified {member.characterName}: HP {member.currentHP}/{member.maxHP}");
        }
    }
    
    // Smoothly walks a transform to a world-space target position.
    IEnumerator MoveCharacterTo(Transform mover, Vector3 worldTarget)
    {
        while (Vector3.Distance(mover.position, worldTarget) > 0.05f)
        {
            mover.position = Vector3.MoveTowards(mover.position, worldTarget, moveSpeed * Time.deltaTime);
            yield return null;
        }
        mover.position = worldTarget;
    }

    // Smoothly changes the battle camera's field of view (zoom in/out).
    IEnumerator ZoomCamera(float targetFOV)
    {
        if (battleCamera == null)
            yield break;

        while (Mathf.Abs(battleCamera.fieldOfView - targetFOV) > 0.05f)
        {
            battleCamera.fieldOfView = Mathf.MoveTowards(battleCamera.fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);
            yield return null;
        }
        battleCamera.fieldOfView = targetFOV;
    }

    // True for actions where the actor should walk up to the target and swing:
    // basic attacks and damaging skills.
    bool IsApproachAttack(BattleAction action)
    {
        if (action is AttackAction)
            return true;
        if (action is SkillAction skill && skill.skillType == SkillAction.SkillType.Damage)
            return true;
        return false;
    }

    // Returns a spot just in front of the target, on the attacker's side.
    // Players approach an enemy from its left; enemies approach a player from their right.
    Vector3 GetFrontPosition(BattleCharacter attacker, BattleCharacter target)
    {
        Vector3 pos = target.transform.position;
        float dir = attacker.isEnemy ? 1f : -1f;
        pos.x += dir * frontOffset;
        pos.y = attacker.transform.position.y; // keep the attacker's own height
        return pos;
    }

    public void ReturnToWorld()
    {
        // The overworld was only suspended (loaded additively), so we don't reload
        // it — we hand back to GameState, which reveals the overworld, unloads this
        // battle scene, and clears the defeated enemy. Nothing regenerates.
        if (GameState.Instance != null)
        {
            GameState.Instance.SaveGame();
            GameState.Instance.ReturnFromBattle();
        }
        else
        {
            // Fallback if there is no persistent GameState (e.g. testing the battle
            // scene in isolation).
            SceneManager.LoadScene("OverworldScene");
        }
    }
}