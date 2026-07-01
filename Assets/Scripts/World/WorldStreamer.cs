using System.Collections;
using UnityEngine;

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

        WorldBranch existing = startingBranch != null ? startingBranch : FindObjectOfType<WorldBranch>();
        if (existing != null)
        {
            current = existing;
            currentPrefab = null;
            if (table != null)
                current.PopulateEnemies(table.enemyPrefabs, table.enemySpawnChance);
        }
        else
        {
            SpawnNextBranch();
            PlacePlayerAtCurrentBranch();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

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

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

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
