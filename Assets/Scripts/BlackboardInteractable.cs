using System.Collections;
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

    [Header("Cutscene")]
    [Tooltip("Empty GameObject placed just outside the door — player walks here")]
    public Transform walkTarget;
    public float walkDuration  = 1.2f;
    public float fadeDuration  = 1.0f;

    private InteractTrigger interactTrigger;

    void Awake()
    {
        interactTrigger = GetComponent<InteractTrigger>();
    }

    public void OnInteract()
    {
        if (flowchart == null) { Debug.LogWarning("[BlackboardInteractable] No Flowchart assigned.", this); return; }
        flowchart.ExecuteBlock(blockName);
    }

    // ── Fungus YES branch ───────────────────────────────────────────────────

    // Call Method step 1: disable control, start walk animation, lock door open.
    public void EnableCutsceneMode()
    {
        GetComponent<BlackboardDoorTrigger>()?.LockOpen();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        movement mov = player.GetComponent<movement>();
        if (mov) mov.enabled = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb) { rb.velocity = Vector3.zero; rb.isKinematic = true; }

        Animator anim = player.GetComponent<Animator>();
        if (anim) anim.SetBool("isRunning", true);
    }

    // Call Method step 2 (after Fade Screen): load the overworld.
    public void GoToOverworld()
    {
        SceneManager.LoadScene(overworldSceneName);
    }

    // Single Call Method alternative — does everything in one shot.
    public void StartCutscene()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Freeze player control
        movement mov = player.GetComponent<movement>();
        if (mov) mov.enabled = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb) { rb.velocity = Vector3.zero; rb.isKinematic = true; }

        Animator anim = player.GetComponent<Animator>();
        if (anim) anim.SetBool("isRunning", true);
    }

    IEnumerator FadeAndLoad()
    {
        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeToBlack(fadeDuration));

        SceneManager.LoadScene(overworldSceneName);
    }

    // ── Fungus NO branch ────────────────────────────────────────────────────
    public void CancelInteract()
    {
        interactTrigger?.OnDialogClosed();
    }
}
