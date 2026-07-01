using UnityEngine;

// Drop on any GameObject that has a trigger Collider.
// Drag any component implementing IInteractable into the interactable slot.
// Works for the blackboard, NPCs, chests — anything that needs a "Press Space" interaction.
public class InteractTrigger : MonoBehaviour
{
    [Tooltip("Component on any GameObject that implements IInteractable")]
    public MonoBehaviour interactable;

    [Tooltip("Optional UI shown when player is in range (e.g. a 'Press Space' canvas element)")]
    public GameObject promptUI;

    // movement.cs checks these to suppress attack and movement during dialog.
    public static bool PlayerInInteractZone { get; private set; }
    public static bool IsDialogOpen         { get; private set; }

    // When true, ALL interact triggers ignore Space (e.g. during a cutscene).
    public static bool InteractionsLocked   { get; private set; }

    public static void LockInteractions()   => InteractionsLocked = true;
    public static void UnlockInteractions()  => InteractionsLocked = false;

    private IInteractable target;
    private bool playerInZone;
    private bool dialogOpen;

    void Awake()
    {
        target = interactable as IInteractable;
        if (interactable != null && target == null)
            Debug.LogError($"[InteractTrigger] '{interactable.GetType().Name}' does not implement IInteractable.", this);
    }

    void Start()
    {
        if (promptUI) promptUI.SetActive(false);
    }

    void Update()
    {
        if (!playerInZone || dialogOpen || InteractionsLocked) return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            dialogOpen   = true;
            IsDialogOpen = true;
            target?.OnInteract();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInZone = true;
        PlayerInInteractZone = true;
        if (promptUI) promptUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInZone         = false;
        dialogOpen           = false;
        PlayerInInteractZone = false;
        IsDialogOpen         = false;
        if (promptUI) promptUI.SetActive(false);
    }

    // Call this from Fungus (or anywhere) when the dialog closes,
    // so the player can interact again without re-entering the zone.
    public void OnDialogClosed()
    {
        dialogOpen   = false;
        IsDialogOpen = false;
    }
}
