using UnityEngine;

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
