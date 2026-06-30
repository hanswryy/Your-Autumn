using UnityEngine;

// Data-driven catalogue of branch prefabs and how likely each is to appear.
// Designers tune weights and enemy settings here without touching code. Create one
// via Assets > Create > World > Branch Table and assign it to the WorldStreamer.
[CreateAssetMenu(menuName = "World/Branch Table", fileName = "BranchTable")]
public class BranchTable : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public WorldBranch prefab;
        [Tooltip("Relative likelihood of being picked. 0 = never.")]
        [Min(0f)] public float weight = 1f;
    }

    [Header("Branches")]
    public Entry[] entries;

    [Header("Enemies")]
    public GameObject[] enemyPrefabs;
    [Range(0f, 1f)] public float enemySpawnChance = 0.5f;

    // Weighted random pick. `previous` lets us avoid spawning the same branch twice
    // in a row when there's an alternative; pass null to allow anything.
    public WorldBranch Pick(System.Random rng, WorldBranch previousPrefab = null)
    {
        if (entries == null || entries.Length == 0)
        {
            Debug.LogError("[BranchTable] No entries configured.");
            return null;
        }

        float total = 0f;
        foreach (var e in entries)
            if (e != null && e.prefab != null && e.prefab != previousPrefab)
                total += Mathf.Max(0f, e.weight);

        // If filtering out the previous prefab left nothing, fall back to all of them.
        bool avoidPrevious = total > 0f;
        if (!avoidPrevious)
        {
            foreach (var e in entries)
                if (e != null && e.prefab != null)
                    total += Mathf.Max(0f, e.weight);
        }

        if (total <= 0f)
        {
            Debug.LogError("[BranchTable] All weights are zero.");
            return null;
        }

        double roll = rng.NextDouble() * total;
        foreach (var e in entries)
        {
            if (e == null || e.prefab == null) continue;
            if (avoidPrevious && e.prefab == previousPrefab) continue;

            roll -= Mathf.Max(0f, e.weight);
            if (roll <= 0d) return e.prefab;
        }

        // Floating-point fallback.
        foreach (var e in entries)
            if (e != null && e.prefab != null)
                return e.prefab;

        return null;
    }
}
