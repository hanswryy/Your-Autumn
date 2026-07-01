using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUIController : MonoBehaviour, IBattlePresenter
{
    [Header("Panel References")]
    public GameObject actionPanel;
    public GameObject targetPanel;
    public GameObject messagePanel;
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    
    [Header("UI Elements")]
    public TextMeshProUGUI battleMessageText;
    public Button attackButton;
    public Button skillsButton;
    public Button itemsButton;
    public Button defendButton;
    public Transform playerStatsPanel;
    public Transform enemyStatsPanel;
    public GameObject characterStatPrefab;
    public GameObject targetButtonPrefab;
    public Transform targetButtonContainer;
    
    [Header("Skill and Item Menu")]
    public GameObject skillListPanel;
    public GameObject itemListPanel;
    public GameObject actionButtonPrefab;
    public Transform skillListContainer;
    public Transform itemListContainer;
    
    [Header("UI Effects")]
    public GameObject damageNumberPrefab;
    public GameObject healNumberPrefab;
    public GameObject mpNumberPrefab;
    
    private List<BattleCharacter> playerParty;
    private List<BattleCharacter> enemyParty;
    private List<GameObject> characterStatDisplays = new List<GameObject>();
    
    public void SetupBattleUI()
    {
        HideAllPanels();
        ShowStatPanels();
    }
    
    public void SetupPartyUI(List<BattleCharacter> party)
    {
        playerParty = party;
        
        // Create UI for each party member
        foreach (var character in party)
        {
            GameObject statDisplay = Instantiate(characterStatPrefab, playerStatsPanel);
            SetupCharacterStatDisplay(statDisplay, character);
            characterStatDisplays.Add(statDisplay);
        }
    }
    
    public void SetupEnemyUI(List<BattleCharacter> enemies)
    {
        enemyParty = enemies;
        
        // Create UI for each enemy
        foreach (var enemy in enemies)
        {
            GameObject statDisplay = Instantiate(characterStatPrefab, enemyStatsPanel);
            SetupCharacterStatDisplay(statDisplay, enemy);
            characterStatDisplays.Add(statDisplay);
        }
    }
    
    private void SetupCharacterStatDisplay(GameObject display, BattleCharacter character)
    {
        // Set name
        display.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = character.CharacterName;
        
        // Set HP bar
        Slider hpSlider = display.transform.Find("HPBar").GetComponent<Slider>();
        hpSlider.maxValue = character.maxHP;
        hpSlider.value = character.currentHP;
        
        // Set HP text
        TextMeshProUGUI hpText = display.transform.Find("HPText").GetComponent<TextMeshProUGUI>();
        hpText.text = $"{character.currentHP}/{character.maxHP}";
        
        // Set MP bar if exists
        Slider mpSlider = display.transform.Find("MPBar")?.GetComponent<Slider>();
        if (mpSlider != null)
        {
            mpSlider.maxValue = character.maxMP;
            mpSlider.value = character.currentMP;
            
            // Set MP text
            TextMeshProUGUI mpText = display.transform.Find("MPText").GetComponent<TextMeshProUGUI>();
            if (mpText != null)
            {
                mpText.text = $"{character.currentMP}/{character.maxMP}";
            }
        }
    }
    
    public void UpdateCharacterStats()
    {
        // Update player party stats
        for (int i = 0; i < playerParty.Count && i < characterStatDisplays.Count; i++)
        {
            GameObject display = characterStatDisplays[i];
            BattleCharacter character = playerParty[i];
            
            // Update HP
            Slider hpSlider = display.transform.Find("HPBar").GetComponent<Slider>();
            hpSlider.value = character.currentHP;
            
            TextMeshProUGUI hpText = display.transform.Find("HPText").GetComponent<TextMeshProUGUI>();
            hpText.text = $"{character.currentHP}/{character.maxHP}";
            
            // Update MP
            Slider mpSlider = display.transform.Find("MPBar")?.GetComponent<Slider>();
            if (mpSlider != null)
            {
                mpSlider.value = character.currentMP;
                
                TextMeshProUGUI mpText = display.transform.Find("MPText").GetComponent<TextMeshProUGUI>();
                if (mpText != null)
                {
                    mpText.text = $"{character.currentMP}/{character.maxMP}";
                }
            }
        }
        
        // Update enemy stats
        for (int i = 0; i < enemyParty.Count; i++)
        {
            GameObject display = characterStatDisplays[i + playerParty.Count];
            BattleCharacter enemy = enemyParty[i];
            
            // Update HP
            Slider hpSlider = display.transform.Find("HPBar").GetComponent<Slider>();
            hpSlider.value = enemy.currentHP;
            
            TextMeshProUGUI hpText = display.transform.Find("HPText").GetComponent<TextMeshProUGUI>();
            hpText.text = $"{enemy.currentHP}/{enemy.maxHP}";
        }
    }
    
    public void ShowActionMenu(BattleCharacter character)
    {
        HideAllPanels();
        ShowStatPanels();

        actionPanel.SetActive(true);
        
        // Setup button listeners
        attackButton.onClick.RemoveAllListeners();
        attackButton.onClick.AddListener(() => {
            BattleManager.Instance.OnActionSelected(character.availableActions[0]);
        });
        
        skillsButton.onClick.RemoveAllListeners();
        skillsButton.onClick.AddListener(() => {
            ShowSkillList(character);
        });
        
        itemsButton.onClick.RemoveAllListeners();
        itemsButton.onClick.AddListener(() => {
            ShowItemList();
        });
        
        defendButton.onClick.RemoveAllListeners();
        defendButton.onClick.AddListener(() => {
            BattleManager.Instance.OnActionSelected(character.availableActions[1]);
        });
    }
    
    public void ShowSkillList(BattleCharacter character)
    {
        // Hide action panel, show skill list
        actionPanel.SetActive(false);
        skillListPanel.SetActive(true);
        
        // Clear existing skills
        foreach (Transform child in skillListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Add skills
        foreach (var action in character.availableActions)
        {
            // Skip attack and defend which are already on main menu
            if (action.actionName == "Attack" || action.actionName == "Defend")
                continue;
                
            if (action is SkillAction)
            {
                GameObject buttonObj = Instantiate(actionButtonPrefab, skillListContainer);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                
                buttonText.text = $"{action.actionName} (MP: {action.mpCost})";
                
                // Disable button if not enough MP
                button.interactable = character.currentMP >= action.mpCost;
                
                button.onClick.AddListener(() => {
                    BattleManager.Instance.OnActionSelected(action);
                    skillListPanel.SetActive(false);
                    ShowStatPanels();
                });
            }
        }
        
        // Add back button
        GameObject backButton = Instantiate(actionButtonPrefab, skillListContainer);
        backButton.GetComponentInChildren<TextMeshProUGUI>().text = "Back";
        backButton.GetComponent<Button>().onClick.AddListener(() => {
            skillListPanel.SetActive(false);
            actionPanel.SetActive(true);
        });
    }
    
    public void ShowItemList()
    {
        // Hide action panel, show item list
        actionPanel.SetActive(false);
        itemListPanel.SetActive(true);
        
        // Clear existing items
        foreach (Transform child in itemListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Add items from inventory
        if (GameState.Instance != null)
        {
            foreach (var item in GameState.Instance.playerData.inventoryItems)
            {
                if (item.usableInBattle && item.quantity > 0)
                {
                    GameObject buttonObj = Instantiate(actionButtonPrefab, itemListContainer);
                    Button button = buttonObj.GetComponent<Button>();
                    TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                    
                    buttonText.text = $"{item.itemName} x{item.quantity}";
                    
                    // Create item action
                    ItemAction itemAction = ScriptableObject.CreateInstance<ItemAction>();
                    itemAction.actionName = item.itemName;
                    itemAction.description = item.description;
                    itemAction.item = item;
                    
                    // Set target type based on item properties
                    if (item.restoreHP > 0 || item.restoreMP > 0)
                    {
                        itemAction.targetType = TargetType.Ally;
                    }
                    else
                    {
                        itemAction.targetType = TargetType.Enemy;
                    }
                    
                    button.onClick.AddListener(() => {
                        BattleManager.Instance.OnActionSelected(itemAction);
                        itemListPanel.SetActive(false);
                        ShowStatPanels();
                    });
                }
            }
        }
        
        // Add back button
        GameObject backButton = Instantiate(actionButtonPrefab, itemListContainer);
        backButton.GetComponentInChildren<TextMeshProUGUI>().text = "Back";
        backButton.GetComponent<Button>().onClick.AddListener(() => {
            itemListPanel.SetActive(false);
            actionPanel.SetActive(true);
        });
    }
    
    public void ShowTargetMenu(List<BattleCharacter> targets)
    {
        HideAllPanels();
        ShowStatPanels();
        targetPanel.SetActive(true);
        
        // Clear existing target buttons
        foreach (Transform child in targetButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create target buttons
        foreach (var target in targets)
        {
            GameObject buttonObj = Instantiate(targetButtonPrefab, targetButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            
            buttonText.text = target.CharacterName;
            
            button.onClick.AddListener(() => {
                BattleManager.Instance.OnTargetSelected(target);
                targetPanel.SetActive(false);
            });
        }
        
        // Add back button
        GameObject backButton = Instantiate(targetButtonPrefab, targetButtonContainer);
        backButton.GetComponentInChildren<TextMeshProUGUI>().text = "Back";
        backButton.GetComponent<Button>().onClick.AddListener(() => {
            targetPanel.SetActive(false);
            actionPanel.SetActive(true);
        });
    }
    
    // Hides all the action-selection menus and shows the battlefield/stat panels.
    // Used when an action is confirmed without a target menu (e.g. self-targeted skills).
    public void ShowBattleView()
    {
        actionPanel.SetActive(false);
        skillListPanel.SetActive(false);
        itemListPanel.SetActive(false);
        targetPanel.SetActive(false);
        ShowStatPanels();
    }

    public void ShowBattleMessage(string message)
    {
        messagePanel.SetActive(true);
        battleMessageText.text = message;
        
        // Hide message after delay
        StartCoroutine(HideMessageAfterDelay(2f));
    }
    
    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        messagePanel.SetActive(false);
    }
    
    public void ShowVictoryPanel()
    {
        HideAllPanels();
        victoryPanel.SetActive(true);
        
        // Add continue button functionality
        Button continueButton = victoryPanel.GetComponentInChildren<Button>();
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() => {
            BattleManager.Instance.ReturnToWorld();
        });
    }
    
    public void ShowDefeatPanel()
    {
        HideAllPanels();
        defeatPanel.SetActive(true);
        
        // Add retry button functionality
        Button retryButton = defeatPanel.transform.Find("RetryButton").GetComponent<Button>();
        retryButton.onClick.RemoveAllListeners();
        retryButton.onClick.AddListener(() => {
            // Restart battle
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        });
        
        // Add return button functionality
        Button returnButton = defeatPanel.transform.Find("ReturnButton").GetComponent<Button>();
        returnButton.onClick.RemoveAllListeners();
        returnButton.onClick.AddListener(() => {
            BattleManager.Instance.ReturnToWorld();
        });
    }
    
    void HideAllPanels()
    {
        actionPanel.SetActive(false);
        targetPanel.SetActive(false);
        messagePanel.SetActive(false);
        skillListPanel.SetActive(false);
        itemListPanel.SetActive(false);
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        playerStatsPanel.gameObject.SetActive(false);
        enemyStatsPanel.gameObject.SetActive(false);
    }
    
    public void ShowDamageNumber(Vector3 position, int damage)
    {
        GameObject damageObj = Instantiate(damageNumberPrefab, transform);
        damageObj.transform.position = Camera.main.WorldToScreenPoint(position + Vector3.up);
        
        TextMeshProUGUI damageText = damageObj.GetComponent<TextMeshProUGUI>();
        damageText.text = damage.ToString();
        
        Destroy(damageObj, 2f);
        
        // Update character stats
        UpdateCharacterStats();
    }
    
    public void ShowHealingNumber(Vector3 position, int healing)
    {
        GameObject healObj = Instantiate(healNumberPrefab, transform);
        healObj.transform.position = Camera.main.WorldToScreenPoint(position + Vector3.up);
        
        TextMeshProUGUI healText = healObj.GetComponent<TextMeshProUGUI>();
        healText.text = "+" + healing.ToString();
        
        Destroy(healObj, 2f);
        
        // Update character stats
        UpdateCharacterStats();
    }
    
    public void ShowMPRestoredNumber(Vector3 position, int mp)
    {
        GameObject mpObj = Instantiate(mpNumberPrefab, transform);
        mpObj.transform.position = Camera.main.WorldToScreenPoint(position + Vector3.up * 1.5f);
        
        TextMeshProUGUI mpText = mpObj.GetComponent<TextMeshProUGUI>();
        mpText.text = "MP +" + mp.ToString();
        
        Destroy(mpObj, 2f);
        
        // Update character stats
        UpdateCharacterStats();
    }

    // ── IBattlePresenter ──────────────────────────────────────────────────────
    // Thin adapters so battle actions can report results without knowing about this
    // class's position-based UI methods.
    public void ShowMessage(string message) => ShowBattleMessage(message);
    public void ShowDamage(BattleCharacter target, int amount) => ShowDamageNumber(target.transform.position, amount);
    public void ShowHealing(BattleCharacter target, int amount) => ShowHealingNumber(target.transform.position, amount);
    public void ShowMPRestored(BattleCharacter target, int amount) => ShowMPRestoredNumber(target.transform.position, amount);
    
    private void ShowStatPanels()
    {
        playerStatsPanel.gameObject.SetActive(true);
        enemyStatsPanel.gameObject.SetActive(true);
    }
}
