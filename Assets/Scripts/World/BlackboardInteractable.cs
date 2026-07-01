using Fungus;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BlackboardInteractable : MonoBehaviour, IInteractable
{
    [Header("Fungus")]
    public Flowchart flowchart;
    public string blockName = "AskOverworld";

    [Header("Scene Transition")]
    public string overworldSceneName = "OverworldScene";

    private InteractTrigger interactTrigger;

    void Awake()
    {
        interactTrigger = GetComponent<InteractTrigger>();
    }

    public void OnInteract()
    {
        if (flowchart == null) { Debug.LogWarning("[BlackboardInteractable] No Flowchart assigned.", this); return; }
        if (flowchart.HasExecutingBlocks()) return; // already running — don't start a duplicate
        flowchart.ExecuteBlock(blockName);
    }

    // ── Fungus YES branch ───────────────────────────────────────────────────

    // Step 1: hand off control to the cutscene. Player stands idle until told to walk.
    public void EnableCutsceneMode()
    {
        GetComponent<BlackboardDoorTrigger>()?.LockOpen();
        InteractTrigger.LockInteractions(); // block Space-interacts (e.g. Karina's shop) during the cutscene

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        movement mov = player.GetComponent<movement>();
        if (mov) mov.enabled = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb) { rb.velocity = Vector3.zero; rb.isKinematic = true; }

        SetPlayerRunning(false);
    }

    // Call before a LeanTween "Move" command so the run animation plays while walking.
    public void PlayerStartWalk() => SetPlayerRunning(true);

    // Call after a "Move" command finishes so the player idles during dialog.
    public void PlayerStopWalk() => SetPlayerRunning(false);

    private void SetPlayerRunning(bool running)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Animator anim = player != null ? player.GetComponent<Animator>() : null;
        if (anim) anim.SetBool("isRunning", running);
    }

    // Final step (after Fade Screen): load the overworld.
    public void GoToOverworld()
    {
        InteractTrigger.UnlockInteractions(); // clear the lock so the next scene starts interactable
        SceneManager.LoadScene(overworldSceneName);
    }

    // ── Fungus NO branch ────────────────────────────────────────────────────
    public void CancelInteract()
    {
        interactTrigger?.OnDialogClosed();
    }
}
