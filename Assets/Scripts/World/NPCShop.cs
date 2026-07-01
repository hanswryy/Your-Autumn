using System.Collections.Generic;
using Fungus;
using UnityEngine;

// Attach to a shopkeeper NPC alongside InteractTrigger.
// Drag this into the InteractTrigger's Interactable slot.
//
// Fungus wiring:
//   - OnInteract runs the shop block.
//   - Call RefreshShop at the top of the Shop block to sync the flowchart variables
//     used for graying-out options and showing carried counts.
//   - To buy, set the String variable selectedItemVar (e.g. "SelectedItem") to the item id,
//     then use Call Method -> BuySelectedItem.  (Fungus "Call Method" cannot pass arguments,
//     so the id is read from a flowchart variable. "Invoke Method" -> BuyItem also works.)
//   - BuySelectedItem/BuyItem writes the result into purchaseResultVar.
//
// Per-item flowchart variables (create these in the Variables panel):
//   Boolean  CanAfford_<itemId>   -> bind to the Menu option's "Interactable" field
//   Integer  Count_<itemId>       -> use as {$Count_<itemId>} in Say / Menu text
public class NPCShop : MonoBehaviour, IInteractable
{
    [Header("Fungus")]
    public Flowchart flowchart;
    [Tooltip("Block executed when the player talks to this shopkeeper")]
    public string blockName = "Shop";

    [Header("Shop Stock")]
    [Tooltip("Item ids this shop sells (must exist in the ItemDatabase)")]
    public List<string> shopItemIds = new List<string>();

    [Header("Fungus Variable Sync")]
    [Tooltip("String variable holding the item id to buy (set it before calling BuySelectedItem)")]
    public string selectedItemVar = "SelectedItem";
    [Tooltip("Boolean variable set after each BuyItem call")]
    public string purchaseResultVar = "PurchaseSuccess";
    [Tooltip("Integer variable kept in sync with the player's currency")]
    public string currencyVar = "Currency";
    [Tooltip("Prefix for per-item affordability booleans, e.g. CanAfford_potion")]
    public string canAffordPrefix = "CanAfford_";
    [Tooltip("Prefix for per-item carried-count integers, e.g. Count_potion")]
    public string countPrefix = "Count_";

    private InteractTrigger interactTrigger;

    void Awake()
    {
        interactTrigger = GetComponent<InteractTrigger>();
    }

    // Called by InteractTrigger on Space press.
    public void OnInteract()
    {
        if (flowchart == null) { Debug.LogWarning("[NPCShop] No Flowchart assigned.", this); return; }
        if (flowchart.HasExecutingBlocks()) return; // already running — don't start a duplicate
        RefreshShop();
        flowchart.ExecuteBlock(blockName);
    }

    // ── Call at the top of the Shop block (and after buying) ──────────────────
    // Refreshes currency, per-item affordability, and carried counts.
    public void RefreshShop()
    {
        if (flowchart == null || GameState.Instance == null) return;

        SyncCurrencyToFungus();

        ItemDatabase db = GameState.Instance.itemDatabase;
        foreach (string id in shopItemIds)
        {
            if (string.IsNullOrEmpty(id)) continue;

            ItemDefinition def = db != null ? db.GetItem(id) : null;
            if (def == null)
                Debug.LogWarning($"[NPCShop] Item id '{id}' not found in ItemDatabase — option will be grayed out.", this);

            int price = def != null ? def.buyPrice : int.MaxValue;

            string affordKey = canAffordPrefix + id;
            if (flowchart.HasVariable(affordKey))
                flowchart.SetBooleanVariable(affordKey, def != null && GameState.Instance.CanAfford(price));
            else
                Debug.LogWarning($"[NPCShop] Flowchart has no boolean variable '{affordKey}'.", this);

            string countKey = countPrefix + id;
            if (flowchart.HasVariable(countKey))
                flowchart.SetIntegerVariable(countKey, GameState.Instance.GetItemCount(id));
            else
                Debug.LogWarning($"[NPCShop] Flowchart has no integer variable '{countKey}'.", this);
        }
    }

    // ── Called by Fungus "Call Method" (reads the id from selectedItemVar) ─────
    public void BuySelectedItem()
    {
        string itemId = null;
        if (flowchart != null)
        {
            var v = flowchart.GetVariable<StringVariable>(selectedItemVar);
            if (v != null) itemId = v.Value;
        }

        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning($"[NPCShop] selectedItemVar '{selectedItemVar}' is empty — nothing bought.", this);
            return;
        }

        BuyItem(itemId);
    }

    // ── Called by Fungus "Invoke Method" with the item id as a string argument ─
    public void BuyItem(string itemId)
    {
        bool success = GameState.Instance != null && GameState.Instance.PurchaseItem(itemId);

        if (flowchart != null && flowchart.HasVariable(purchaseResultVar))
            flowchart.SetBooleanVariable(purchaseResultVar, success);

        // Update affordability/counts so the menu reflects the new balance.
        RefreshShop();
    }

    // ── Called by Fungus NO/Exit branch ───────────────────────────────────────
    public void CloseShop()
    {
        interactTrigger?.OnDialogClosed();
    }

    private void SyncCurrencyToFungus()
    {
        if (flowchart == null || GameState.Instance == null) return;
        if (flowchart.HasVariable(currencyVar))
            flowchart.SetIntegerVariable(currencyVar, Mathf.RoundToInt(GameState.Instance.Currency));
    }
}
