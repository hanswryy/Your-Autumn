using System.Collections;
using UnityEngine;

// Drives the infinite overworld by streaming one branch at a time.
//
// Because moving between branches is a fade-to-black teleport, every branch can
// occupy the SAME spot in world space (the anchor). We therefore only ever keep
// one branch alive: when the player walks into an exit we fade out, destroy the
// current branch, spawn the next one at the anchor, drop the player on its spawn
// point, and fade back in. Infinite by construction, no hard-coded layout.
//
// Replaces the old PathManager + PathGenerator pair.
public class WorldStreamer : MonoBehaviour
{
    public static WorldStreamer Instance { get; private set; }

    [Header("Generation")]
    public BranchTable table;
    [Tooltip("Where every branch is spawned. If empty, this object's transform is used.")]
    public Transform anchor;
    [Tooltip("0 = random seed each run. Non-zero = deterministic generation.")]
    public int seed = 0;

    [Header("Player")]
    [Tooltip("If empty, the player is found by the \"Player\" tag.")]
    public GameObject player;

    [Header("Starting Branch")]
    [Tooltip("Optional hand-placed branch to begin on (the designed starting area). " +
             "If empty, any WorldBranch already in the scene is adopted; if there is " +
             "none, the first branch is generated from the table. The player is NOT " +
             "moved when starting on a hand-placed branch — they keep their scene position.")]
    public WorldBranch startingBranch;

    [Header("Transition")]
    [Tooltip("Seconds for the fade-out / fade-in when moving between branches.")]
    public float fadeDuration = 0.4f;

    private WorldBranch current;
    private WorldBranch currentPrefab;   // the prefab `current` was instantiated from, for repeat-avoidance
    private System.Random rng;
    private bool isTransitioning;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        rng = seed != 0 ? new System.Random(seed) : new System.Random();
    }

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        // Begin on a hand-placed branch if there is one, so the designer's starting
        // area is used as-is. We must NOT spawn a duplicate from the table here — that
        // was the bug that left a second, enemy-less branch on top of the real one.
        WorldBranch existing = startingBranch != null ? startingBranch : FindObjectOfType<WorldBranch>();
        if (existing != null)
        {
            current = existing;
            currentPrefab = null; // unknown source prefab; allow any branch next
            if (table != null)
                current.PopulateEnemies(table.enemyPrefabs, table.enemySpawnChance);
            // The player is already standing in the hand-placed starting area, so we
            // leave them where the designer put them.
        }
        else
        {
            // No starting branch in the scene: generate the first one. The surrounding
            // scene fade (cutscene / hub entry) covers the pop-in, so we don't fade.
            SpawnNextBranch();
            PlacePlayerAtCurrentBranch();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Called by a BranchExit when the player walks into it.
    public void Advance(BranchExit exit)
    {
        if (isTransitioning) return;
        StartCoroutine(AdvanceRoutine());
    }

    private IEnumerator AdvanceRoutine()
    {
        isTransitioning = true;

        yield return Fade(toBlack: true);

        if (current != null)
            Destroy(current.gameObject);

        SpawnNextBranch();
        PlacePlayerAtCurrentBranch();

        // Let the new branch's colliders/triggers settle before we reveal it, so the
        // player doesn't immediately re-trigger an exit they were standing on.
        yield return null;

        yield return Fade(toBlack: false);

        isTransitioning = false;
    }

    private void SpawnNextBranch()
    {
        WorldBranch prefab = table != null ? table.Pick(rng, currentPrefab) : null;
        if (prefab == null)
        {
            Debug.LogError("[WorldStreamer] Could not pick a branch — check the BranchTable.");
            return;
        }

        Transform a = anchor != null ? anchor : transform;
        current = Instantiate(prefab, a.position, a.rotation);
        currentPrefab = prefab;

        if (table != null)
            current.PopulateEnemies(table.enemyPrefabs, table.enemySpawnChance);
    }

    private void PlacePlayerAtCurrentBranch()
    {
        if (player == null || current == null) return;

        Vector3 pos = current.PlayerSpawnPosition;
        Quaternion rot = current.PlayerSpawnRotation;

        // A CharacterController (if present) fights manual moves; disable it around
        // the teleport. The fade hides everything here.
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // The player's Rigidbody is dynamic + interpolated. Setting only
        // transform.position leaves the physics body behind, and interpolation then
        // drags the visible transform back toward the old spot — so the player seems
        // to land in the wrong place or not move at all. Move the BODY itself and
        // clear momentum so it actually teleports.
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
            rb.rotation = rot;
        }

        player.transform.SetPositionAndRotation(pos, rot);

        if (cc != null) cc.enabled = true;
    }

    // Uses the shared ScreenFader so branch transitions match every other fade in
    // the game. Falls back to a plain wait if no fader is present.
    private IEnumerator Fade(bool toBlack)
    {
        if (ScreenFader.Instance != null)
        {
            yield return toBlack
                ? ScreenFader.Instance.FadeToBlack(fadeDuration)
                : ScreenFader.Instance.FadeFromBlack(fadeDuration);
        }
        else
        {
            yield return new WaitForSeconds(fadeDuration);
        }
    }
}
