using UnityEngine;

public class PathNode : MonoBehaviour
{
    public enum NodeType { OneWay, TwoWayLeft, TwoWayRight, ThreeWay }

    [Header("Node Settings")]
    public NodeType nodeType;
    public Transform spawnPoint; // Your existing spawn point

    [Header("Connections")]
    // References to connected nodes (set during generation)
    public PathNode forwardConnection;
    public PathNode leftConnection;
    public PathNode rightConnection;

    [Header("Exit Triggers")]
    public Transform forwardExitPoint;
    public Transform leftExitPoint;
    public Transform rightExitPoint;

    [Header("Enemy Settings")]
    public Transform enemySpawnPoint; // Specific spot for enemies
    public bool canSpawnEnemy = true; // Toggle for this specific node
    public GameObject assignedEnemyPrefab; // Optional: specific enemy for this node


    // Track node's position in the network
    [HideInInspector] public int depthLevel;
    
    // Add this to your PathNode class
    public Vector3 absoluteSpawnPosition;
    
    public GameObject SpawnEnemy(GameObject[] enemyPrefabs, float spawnChance)
    {
        // Don't spawn if this node has spawning disabled
        if (!canSpawnEnemy)
            return null;
            
        // Random chance check
        if (Random.value > spawnChance)
            return null;
            
        // Determine which enemy to spawn
        GameObject prefab = null;
        
        // If this node has a specific enemy assigned, use that
        if (assignedEnemyPrefab != null)
        {
            prefab = assignedEnemyPrefab;
        }
        // Otherwise choose from the provided array
        else if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        }
        else
        {
            Debug.LogWarning("No enemy prefabs available to spawn");
            return null;
        }
        
        // Get spawn position
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        
        if (enemySpawnPoint != null)
        {
            spawnPosition = enemySpawnPoint.position;
            spawnRotation = enemySpawnPoint.rotation;
        }
        else
        {
            // Fall back to the player spawn point but offset it
            spawnPosition = spawnPoint != null ? 
                spawnPoint.position + new Vector3(1f, 0, 0) : 
                transform.position + new Vector3(0, 1f, 0);
            spawnRotation = Quaternion.identity;
        }
        
        // Spawn the enemy
        GameObject enemy = Instantiate(prefab, spawnPosition, spawnRotation);
        enemy.transform.parent = transform;
        
        // Ensure it has the Enemy tag
        enemy.tag = "Enemy";
        
        Debug.Log($"Spawned enemy on node {name} at depth {depthLevel}");
        
        return enemy;
    }


    private void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);

            // Show text in scene view
#if UNITY_EDITOR
            UnityEditor.Handles.Label(spawnPoint.position, "Spawn: " + spawnPoint.position);
#endif
        }
    }
}