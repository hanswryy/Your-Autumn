using Fungus;
using UnityEngine;

// Drop on an NPC GameObject alongside an InteractTrigger (which supplies the
// trigger collider + "Press Space" handling). Runs a Fungus block on interact.
public class NpcInteractable : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    public string npcName = "Karina";

    [Header("Fungus")]
    public Flowchart flowchart;
    public string blockName = "Greet";

    private InteractTrigger interactTrigger;

    void Awake()
    {
        interactTrigger = GetComponent<InteractTrigger>();
    }

    public void OnInteract()
    {
        if (flowchart == null) { Debug.LogWarning($"[NpcInteractable] No Flowchart assigned on '{npcName}'.", this); return; }
        flowchart.ExecuteBlock(blockName);
    }

    public void OnDialogClosed()
    {
        interactTrigger?.OnDialogClosed();
    }
}
