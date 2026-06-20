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

    [Header("Battle Transition Data")]
    public Vector3 playerPositionBeforeBattle;
    public string lastBattleEnemyId;
    public bool returningFromBattle = false;

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

    public int currentNodeIndex = -1;

    public void StartBattle(GameObject enemy)
    {
        // Store player position
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerPositionBeforeBattle = player.transform.position;
            Debug.Log($"Saved player position before battle: {playerPositionBeforeBattle}");
        }

        // Store enemy ID (using instance ID if no other ID system exists)
        lastBattleEnemyId = enemy.name + "_" + enemy.GetInstanceID().ToString();
        Debug.Log($"Starting battle with enemy: {lastBattleEnemyId}");

        // Set returning from battle flag to true
        returningFromBattle = true;

        // Load battle scene
        SceneManager.LoadScene("BattleScene");
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
        hero.characterId = "hero";  // IMPORTANT: Use consistent ID format
        hero.characterName = "Hero";
        hero.level = 1;
        hero.maxHP = 90;
        hero.currentHP = 90;
        hero.maxMP = 30;
        hero.currentMP = 30;
        hero.attack = 45;
        hero.defense = 5;
        hero.speed = 8;
        playerData.partyMembers.Add(hero);
        
        // Create the defender character
        CharStat defender = new CharStat();
        defender.characterId = "cecil";  // IMPORTANT: Use consistent ID format
        defender.characterName = "Cecil";
        defender.level = 1;
        defender.maxHP = 120;
        defender.currentHP = 120;
        defender.maxMP = 50;
        defender.currentMP = 50;
        defender.attack = 15;
        defender.defense = 40;
        defender.speed = 6;
        playerData.partyMembers.Add(defender);
    }

    private void AddInitialItems()
    {
        // Add some starting potions
        ItemData potion = new ItemData();
        potion.itemId = "potion";
        potion.itemName = "Potion";
        potion.description = "Restores 30 HP";
        potion.itemType = ItemType.Consumable;
        potion.quantity = 3;
        potion.restoreHP = 30;
        potion.usableInBattle = true;
        playerData.inventoryItems.Add(potion);
        
        // Add starting ether
        ItemData ether = new ItemData();
        ether.itemId = "ether";
        ether.itemName = "Ether";
        ether.description = "Restores 15 MP";
        ether.itemType = ItemType.Consumable;
        ether.quantity = 2;
        ether.restoreMP = 15;
        ether.usableInBattle = true;
        playerData.inventoryItems.Add(ether);
    }
}
