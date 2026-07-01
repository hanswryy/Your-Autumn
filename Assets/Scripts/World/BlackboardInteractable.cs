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

    public void EnableCutsceneMode()
    {
        GetComponent<BlackboardDoorTrigger>()?.LockOpen();
        InteractTrigger.LockInteractions();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        movement mov = player.GetComponent<movement>();
        if (mov) mov.enabled = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb) { rb.velocity = Vector3.zero; rb.isKinematic = true; }

        SetPlayerRunning(false);
    }

    public void PlayerStartWalk() => SetPlayerRunning(true);
    public void PlayerStopWalk() => SetPlayerRunning(false);

    private void SetPlayerRunning(bool running)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Animator anim = player != null ? player.GetComponent<Animator>() : null;
        if (anim) anim.SetBool("isRunning", running);
    }

    public void GoToOverworld()
    {
        InteractTrigger.UnlockInteractions();
        SceneManager.LoadScene(overworldSceneName);
    }

    public void CancelInteract()
    {
        interactTrigger?.OnDialogClosed();
    }
}
