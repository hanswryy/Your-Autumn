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
    // Resolved targets for the current action: one for single-target, many for AoE.
    private List<BattleCharacter> currentTargets = new List<BattleCharacter>();
    private BattleAction currentAction;

    // Builds and dispenses the Speed-ordered turn order, rebuilt each round.
    private readonly TurnOrderHandler turnOrder = new TurnOrderHandler();
    
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

        // Build the first round's Speed-based order, then start dispensing turns.
        turnOrder.BuildRound(playerParty, enemyParty);
        AdvanceTurn();
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
    
    // Drives the battle: hands out the next actor in the round's Speed order, starting
    // a fresh round when the current one is exhausted. Players open the action menu;
    // enemies act via AI. Called once at battle start and after every action resolves.
    void AdvanceTurn()
    {
        // The action that just finished may have ended the battle.
        if (CheckBattleEnd())
            return;

        BattleCharacter actor = turnOrder.NextActor();
        if (actor == null)
        {
            // Round finished — rebuild the order from current Speeds (buffs and deaths
            // since last round are now reflected), then take the first actor.
            turnOrder.BuildRound(playerParty, enemyParty);
            actor = turnOrder.NextActor();
            if (actor == null)
                return; // safety: no one left who can act
        }

        currentActor = actor;

        if (actor.isEnemy)
        {
            state = BattleTurnState.EnemyAction;
            StartCoroutine(EnemyAct(actor));
        }
        else
        {
            // Move the active character to the center, zoom in, then show the action menu.
            state = BattleTurnState.PlayerSelect;
            StartCoroutine(BeginPlayerTurn());
        }
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

        switch (action.targetType)
        {
            // Fixed-group modes skip the target-selection menu entirely.
            case TargetType.Self:
                currentTargets = new List<BattleCharacter> { currentActor };
                ResolveSelectedAction();
                break;

            case TargetType.AllEnemies:
                currentTargets = AliveEnemies();
                ResolveSelectedAction();
                break;

            case TargetType.AllAllies:
                currentTargets = AliveAllies();
                ResolveSelectedAction();
                break;

            // Single-target modes let the player pick one target from a menu.
            default:
                uiController.ShowTargetMenu(GetValidTargets(action));
                break;
        }
    }

    public void OnTargetSelected(BattleCharacter target)
    {
        currentTargets = new List<BattleCharacter> { target };
        ResolveSelectedAction();
    }

    // Shared tail once the targets are known: hide the menus and run the action.
    private void ResolveSelectedAction()
    {
        uiController.ShowBattleView();
        state = BattleTurnState.PlayerAction;
        StartCoroutine(PerformAction());
    }

    private List<BattleCharacter> AliveEnemies() => enemyParty.FindAll(e => e.IsAlive());
    private List<BattleCharacter> AliveAllies() => playerParty.FindAll(p => p.IsAlive());

    // Targets offered in the single-target selection menu.
    private List<BattleCharacter> GetValidTargets(BattleAction action)
    {
        return action.targetType == TargetType.Ally ? AliveAllies() : AliveEnemies();
    }
    
    IEnumerator PerformAction()
    {
        StartCoroutine(ZoomCamera(defaultFOV));
        // Display action message
        uiController.ShowBattleMessage($"{currentActor.CharacterName} uses {currentAction.actionName}!");

        yield return new WaitForSeconds(0.5f);

        // Approach only for a SINGLE-target attack. AoE/multi-target attacks stay in place
        // and just swing; non-attacks (buffs/heals) never approach.
        bool isAttack = IsApproachAttack(currentAction) && currentTargets.Count > 0;
        bool approachSingle = isAttack && currentTargets.Count == 1;

        if (isAttack)
        {
            if (approachSingle)
                yield return MoveCharacterTo(currentActor.transform, GetFrontPosition(currentActor, currentTargets[0]));

            currentActor.PlayAttackAnimation();
            yield return new WaitForSeconds(0.4f);
            StartCoroutine(CameraShake(0.2f, 0.3f));
        }

        // Execute action logic against every resolved target.
        yield return currentAction.Execute(currentActor, currentTargets, uiController);

        // The player always walked to the center at the start of their turn (see
        // BeginPlayerTurn), so they always return home — whether they approached a single
        // target, stayed put for an AoE, or used a self/item action from the center.
        yield return MoveCharacterTo(currentActor.transform, actorOriginalPosition);

        // Hand back to the turn-order driver (it checks for battle end first).
        AdvanceTurn();
    }
    
    // Resolves a single enemy's turn (the turn-order driver decides when an enemy acts,
    // so this no longer loops over the whole enemy party).
    IEnumerator EnemyAct(BattleCharacter enemy)
    {
        yield return new WaitForSeconds(1f);

        // Expire any buffs that have run their course before this enemy acts.
        enemy.TickBuffs();

        // Enemy selects action and resolves its target(s).
        currentActor = enemy;
        currentAction = enemy.SelectAction();
        currentTargets = ResolveEnemyTargets(currentAction);

        // Announce the enemy action.
        uiController.ShowBattleMessage($"{currentActor.CharacterName} uses {currentAction.actionName}!");

        yield return new WaitForSeconds(0.5f);

        // Remember the enemy's spot. Approach only for a single-target attack; AoE stays put.
        Vector3 enemyOrigin = enemy.transform.position;
        bool isAttack = IsApproachAttack(currentAction) && currentTargets.Count > 0;
        bool approachSingle = isAttack && currentTargets.Count == 1;

        if (isAttack)
        {
            if (approachSingle)
                yield return MoveCharacterTo(enemy.transform, GetFrontPosition(enemy, currentTargets[0]));

            currentActor.PlayAttackAnimation();
            yield return new WaitForSeconds(0.4f);
            StartCoroutine(CameraShake(0.2f, 0.3f));
        }

        // Execute enemy action against all resolved targets.
        yield return currentAction.Execute(currentActor, currentTargets, uiController);

        // Send the enemy back only if it walked out.
        if (approachSingle)
            yield return MoveCharacterTo(enemy.transform, enemyOrigin);

        // Hand back to the turn-order driver (it checks for battle end first).
        AdvanceTurn();
    }

    // Picks an enemy's targets: the player party for offensive actions, fellow enemies
    // otherwise (e.g. healing). AoE modes hit the whole group; single modes hit one at random.
    private List<BattleCharacter> ResolveEnemyTargets(BattleAction action)
    {
        bool targetsPlayers = action.targetType == TargetType.Enemy || action.targetType == TargetType.AllEnemies;
        List<BattleCharacter> pool = targetsPlayers ? AliveAllies() : AliveEnemies();

        if (pool.Count == 0)
            return new List<BattleCharacter>();

        if (action.targetType == TargetType.AllEnemies || action.targetType == TargetType.AllAllies)
            return pool;

        return new List<BattleCharacter> { pool[Random.Range(0, pool.Count)] };
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
        // A skill "approaches" if any of its effects deals damage.
        if (action is SkillAction skill)
            return skill.effects.Exists(e => e != null && e.kind == SkillEffect.Kind.Damage);
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