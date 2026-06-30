using UnityEngine;

// Put this on a child object of a WorldBranch prefab that has a trigger collider.
// When the player walks into it, it asks the WorldStreamer to advance to a freshly
// generated next branch. The "label" is purely informational — every exit leads to
// the next branch — but you can read it later to bias what comes next (e.g. a
// "left" exit could trend toward a different biome or difficulty).
[RequireComponent(typeof(Collider))]
public class BranchExit : MonoBehaviour
{
    [Tooltip("Optional flavor label for this exit: \"Forward\", \"Left\", \"Right\", etc.")]
    public string label = "Forward";

    // Set by WorldBranch.Awake.
    [HideInInspector] public WorldBranch owner;

    void Reset()
    {
        // Make life easy in the editor: ensure the collider is a trigger.
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (WorldStreamer.Instance == null) return;

        WorldStreamer.Instance.Advance(this);
    }
}
