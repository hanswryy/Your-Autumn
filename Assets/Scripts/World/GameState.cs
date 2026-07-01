using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    [System.Serializable]
    public class PlayerData
    {
        public List<CharStat> partyMembers = new List<CharStat>();
        public List<ItemData> inventoryItems = new List<ItemData>();
        public float currency = 0f;
        public float playTime = 0f;
    }

    public PlayerData playerData = new PlayerData();
    private string saveFilePath;

    [Header("Item System")]
    [Tooltip("Optional — falls back to Resources/ItemDatabase if left empty")]
    public ItemDatabase itemDatabase;

    [Header("Battle Transition Data")]
    public Vector3 playerPositionBeforeBattle;
    public string lastBattleEnemyId;
    public bool returningFromBattle = false;

    [Header("Battle Flow (additive)")]
    [Tooltip("Battle is loaded additively on top of the overworld so the overworld " +
             "is never unloaded and never regenerates. The overworld is simply " +
             "hidden for the duration of the fight.")]
    public string battleSceneName = "BattleScene";
    [Tooltip("Fade length when hiding/revealing the overworld around a battle.")]
    public float battleFadeDuration = 0.4f;

    // The overworld scene we suspend while a battle is active.
    private Scene overworldScene;
    // The roots we actually disabled, so we re-enable exactly those (leaving
    // intentionally-inactive objects, e.g. closed panels, alone).
    private readonly List<GameObject> suspendedRoots = new List<GameObject>();
    // Direct handle to the enemy that started the fight — far more robust than
    // matching by name. With additive battles this object is still alive on return.
    private GameObject lastBattleEnemy;

    // Awake is called when the script instance is being loaded
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        Instance = this;

        saveFilePath = Application.persistentDataPath + "/playerData.sav";

        if (itemDatabase == null)
            itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");
    }

    // Save game data to file
    public void SaveGame()
    {
        try
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream file = File.Create(saveFilePath);

            formatter.Serialize(file, playerData);
            file.Close();

            Debug.Log("Game saved successfully to: " + saveFilePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error saving game: " + e.Message);
        }
    }

    // Load game data from file
    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream file = File.Open(saveFilePath, FileMode.Open);

                playerData = (PlayerData)formatter.Deserialize(file);
                file.Close();

                Debug.Log("Game loaded successfully from: " + saveFilePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error loading game: " + e.Message);
                InitializeNewGame(); // Initialize if file is corrupted
            }
        }
        else
        {
            Debug.Log("No save file found. Starting with default values.");
            InitializeNewGame(); // Initialize if file is corrupted
        }
    }

    void OnApplicationQuit()
    {
        SaveGame();
    }

    public CharStat GetPartyMember(string characterId)
    {
        return playerData.partyMembers.Find(c => c.characterId == characterId);
    }

    // Add to GameState class
    public void AddItem(ItemData item)
    {
        // Check if stackable item already exists
        int index = playerData.inventoryItems.FindIndex(i =>
            i.itemId == item.itemId && i.itemType == ItemType.Consumable);

        if (index >= 0)
        {
            // Increase quantity for stackable items
            playerData.inventoryItems[index].quantity += item.quantity;
            Debug.Log($"Added {item.quantity} {item.itemName} to inventory (new total: {playerData.inventoryItems[index].quantity})");
        }
        else
        {
            // Add new item
            playerData.inventoryItems.Add(item);
            Debug.Log($"Added {item.itemName} to inventory");
        }
    }

    public void RemoveItem(string itemId, int quantity = 1)
    {
        int index = playerData.inventoryItems.FindIndex(i => i.itemId == itemId);

        if (index >= 0)
        {
            if (playerData.inventoryItems[index].quantity > quantity)
            {
                playerData.inventoryItems[index].quantity -= quantity;
            }
            else
            {
                playerData.inventoryItems.RemoveAt(index);
            }
        }
    }

    // How many of an item the player currently carries.
    public int GetItemCount(string itemId)
    {
        ItemData item = playerData.inventoryItems.Find(i => i.itemId == itemId);
        return item != null ? item.quantity : 0;
    }

    // ── Currency ─────────────────────────────────────────────────────────────
    public float Currency => playerData.currency;

    public bool CanAfford(float amount) => playerData.currency >= amount;

    public void AddCurrency(float amount)
    {
        playerData.currency += amount;
        Debug.Log($"Added {amount} currency (new total: {playerData.currency})");
    }

    // Returns true if the player had enough and the amount was deducted.
    public bool SpendCurrency(float amount)
    {
        if (!CanAfford(amount)) return false;
        playerData.currency -= amount;
        Debug.Log($"Spent {amount} currency (remaining: {playerData.currency})");
        return true;
    }

    // Give an item to the player for free (e.g. story rewards), by id from the database.
    public bool GiveItem(string itemId, int quantity = 1)
    {
        if (itemDatabase == null)
        {
            Debug.LogWarning("[GameState] No ItemDatabase assigned; cannot give item.");
            return false;
        }

        ItemDefinition def = itemDatabase.GetItem(itemId);
        if (def == null)
        {
            Debug.LogWarning($"[GameState] Unknown item id '{itemId}'.");
            return false;
        }

        AddItem(def.CreateInstance(quantity));
        SaveGame();
        return true;
    }

    // ── Purchasing ───────────────────────────────────────────────────────────
    // Buys one (or more) of an item by id from the ItemDatabase.
    // Returns true on success; false if the item is unknown or unaffordable.
    public bool PurchaseItem(string itemId, int quantity = 1)
    {
        if (itemDatabase == null)
        {
            Debug.LogWarning("[GameState] No ItemDatabase assigned; cannot purchase.");
            return false;
        }

        ItemDefinition def = itemDatabase.GetItem(itemId);
        if (def == null)
        {
            Debug.LogWarning($"[GameState] Unknown item id '{itemId}'.");
            return false;
        }

        float totalCost = def.buyPrice * quantity;
        if (!SpendCurrency(totalCost))
        {
            Debug.Log($"Not enough currency to buy {quantity}x {def.itemName} (cost {totalCost}, have {playerData.currency})");
            return false;
        }

        AddItem(def.CreateInstance(quantity));
        SaveGame();
        return true;
    }

    // Add or update a party member
    public void AddPartyMember(CharStat character)
    {
        // Check if character with same ID already exists in party
        int index = playerData.partyMembers.FindIndex(c => c.characterId == character.characterId);

        if (index >= 0)
        {
            // Update existing character
            playerData.partyMembers[index] = character;
            Debug.Log($"Updated party member: {character.characterId}");
        }
        else
        {
            // Add new character
            playerData.partyMembers.Add(character);
            Debug.Log($"Added new party member: {character.characterId}");
        }
    }

    // Remove a party member
    public void RemovePartyMember(CharStat character)
    {
        int index = playerData.partyMembers.FindIndex(c => c.characterId == character.characterId);

        if (index >= 0)
        {
            playerData.partyMembers.RemoveAt(index);
            Debug.Log($"Removed party member: {character.characterId}");
        }
    }

    public void StartBattle(GameObject enemy)
    {
        // Ignore re-triggers while a battle transition is already playing.
        if (BattleTransitionController.IsTransitioning)
            return;

        // Remember the overworld so we can suspend/restore it; the active scene at
        // this moment IS the overworld.
        overworldScene = SceneManager.GetActiveScene();

        // Store player position (kept for compatibility; with additive battles the
        // player never actually moves).
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerPositionBeforeBattle = player.transform.position;
            Debug.Log($"Saved player position before battle: {playerPositionBeforeBattle}");
        }

        // Keep a direct handle to the enemy so we can remove it on return without
        // fragile name matching.
        lastBattleEnemy = enemy;
        lastBattleEnemyId = enemy.name + "_" + enemy.GetInstanceID().ToString();
        Debug.Log($"Starting battle with enemy: {lastBattleEnemyId}");

        // Set returning from battle flag to true
        returningFromBattle = true;

        // Play the Persona-style slow-mo zoom on the enemy, then load the battle
        // scene additively. A controller placed in the scene lets you tune the effect
        // in the inspector; otherwise we spin up a default one so it works out of the box.
        BattleTransitionController transition = FindObjectOfType<BattleTransitionController>();
        if (transition == null)
        {
            transition = new GameObject("BattleTransition").AddComponent<BattleTransitionController>();
        }
        transition.BeginTransition(enemy);
    }

    // ── Additive battle flow ──────────────────────────────────────────────────
    // Called by BattleTransitionController once the battle scene has finished
    // loading additively and been activated. We hide the overworld (without
    // unloading it) so only the battle renders/updates.
    public void SuspendOverworldForBattle(Scene battleScene)
    {
        if (battleScene.IsValid())
            SceneManager.SetActiveScene(battleScene);

        suspendedRoots.Clear();
        if (overworldScene.IsValid())
        {
            foreach (GameObject root in overworldScene.GetRootGameObjects())
            {
                if (root.activeSelf)
                {
                    suspendedRoots.Add(root);
                    root.SetActive(false);
                }
            }
        }
    }

    // Called by BattleManager when the fight is over. Reveals the overworld, unloads
    // the battle scene, and clears out the defeated enemy. The player never moved,
    // so there is nothing to reposition and nothing to regenerate.
    public void ReturnFromBattle()
    {
        StartCoroutine(ReturnFromBattleRoutine());
    }

    private IEnumerator ReturnFromBattleRoutine()
    {
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeToBlack(battleFadeDuration);

        // Re-enable exactly the roots we suspended.
        foreach (GameObject root in suspendedRoots)
            if (root != null) root.SetActive(true);
        suspendedRoots.Clear();

        if (overworldScene.IsValid())
            SceneManager.SetActiveScene(overworldScene);

        // Hand camera control back to gameplay — the battle transition left the
        // CinemachineBrain disabled and the camera parked at the zoom pose. Doing this
        // while the screen is still black lets it re-frame before we fade in.
        BattleTransitionController.RestoreOverworldCamera();

        // Unload the battle scene (it was loaded additively).
        if (!string.IsNullOrEmpty(battleSceneName) &&
            SceneManager.GetSceneByName(battleSceneName).isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(battleSceneName);
        }

        // Remove the enemy we just defeated.
        if (lastBattleEnemy != null)
            Destroy(lastBattleEnemy);
        lastBattleEnemy = null;

        returningFromBattle = false;

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeFromBlack(battleFadeDuration);
    }

    private float timeSinceLastSave = 0f;

    void Start()
    {
        LoadGame();
        Debug.Log("GameState initialized and loaded");
    }

    void Update()
    {
        // Update play time
        playerData.playTime += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.P))
        {
            // Save game state
            SaveGame();
        }

        // debug to load saved game
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadGame();
            Debug.Log("Loaded saved game data");
        }

        // debug go to Battle scene
        if (Input.GetKeyDown(KeyCode.B))
        {
            SceneManager.LoadScene("BattleScene");
            Debug.Log("Switched to Battle scene");
        }
    }
    
    public void InitializeNewGame()
{
    // Clear existing data
    playerData.partyMembers.Clear();
    playerData.inventoryItems.Clear();
    playerData.currency = 0;
    playerData.playTime = 0;
    
    // Add initial party members
    AddInitialPartyMembers();
    
    // Add initial items
    AddInitialItems();
    
    // Save the initial state
    SaveGame();
    
    Debug.Log("New game initialized with default values");
}

    private void AddInitialPartyMembers()
    {
        // Create the hero character
        CharStat hero = new CharStat();
        hero.characterId = "hero";
        hero.characterName = "Hero";
        hero.level = 1;
        hero.maxHP = 90;
        hero.currentHP = 90;
        hero.maxMP = 30;
        hero.currentMP = 30;
        hero.attack = 45;
        hero.defense = 5;
        hero.speed = 8;
        hero.critChance = 10;
        playerData.partyMembers.Add(hero);
        
        // Create the defender character
        CharStat defender = new CharStat();
        defender.characterId = "cecil";
        defender.characterName = "Cecil";
        defender.level = 1;
        defender.maxHP = 120;
        defender.currentHP = 120;
        defender.maxMP = 50;
        defender.currentMP = 50;
        defender.attack = 15;
        defender.defense = 15;
        defender.speed = 6;
        defender.critChance = 5;
        playerData.partyMembers.Add(defender);
    }

    private void AddInitialItems()
    {
        if (itemDatabase == null)
        {
            Debug.LogWarning("[GameState] No ItemDatabase assigned; starting with empty inventory.");
            return;
        }

        playerData.currency = itemDatabase.startingCurrency;

        foreach (var entry in itemDatabase.startingItems)
        {
            if (entry == null || entry.item == null) continue;
            playerData.inventoryItems.Add(entry.item.CreateInstance(entry.quantity));
        }
    }
}
