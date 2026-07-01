using System.Collections.Generic;
using UnityEngine;

public class WorldBranch : MonoBehaviour
{
    [Header("Layout (authored in the prefab)")]
    [Tooltip("Where the player is placed when they enter this branch.")]
    public Transform playerSpawn;

    [Tooltip("Optional spots where enemies can spawn. Leave empty for a safe/rest branch.")]
    public Transform[] enemySlots;

    [Header("Exits")]
    [Tooltip("The exits the player can walk into to advance. If left empty, they are " +
             "auto-collected from BranchExit components in the children at Awake.")]
    public List<BranchExit> exits = new List<BranchExit>();

    void Awake()
    {
        // Auto-wire exits so designers don't have to drag every one into the list.
        if (exits == null || exits.Count == 0)
            exits = new List<BranchExit>(GetComponentsInChildren<BranchExit>(true));

        foreach (var exit in exits)
            if (exit != null) exit.owner = this;
    }

    // Spawns enemies into this branch's slots. Each slot rolls independently against
    // spawnChance. Called by the WorldStreamer right after the branch is created.
    public void PopulateEnemies(GameObject[] enemyPrefabs, float spawnChance)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || enemySlots == null)
            return;

        foreach (var slot in enemySlots)
        {
            if (slot == null) continue;
            if (Random.value > spawnChance) continue;

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            GameObject enemy = Instantiate(prefab, slot.position, slot.rotation, transform);
            enemy.tag = "Enemy";
        }
    }

    public Vector3 PlayerSpawnPosition =>
        playerSpawn != null ? playerSpawn.position : transform.position;

    public Quaternion PlayerSpawnRotation =>
        playerSpawn != null ? playerSpawn.rotation : transform.rotation;
}
