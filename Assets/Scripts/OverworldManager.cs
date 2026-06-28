using UnityEngine;
using System.Collections;
using Fungus;

public class OverworldManager : MonoBehaviour
{
    public GameObject playerPrefab; // Reference to your player prefab if needed

    [Header("Scene Transition")]
    public float fadeInDuration = 1f;

    void Start()
    {
        // Reveal the scene from the black that the cutscene's Fungus "Fade Screen" left behind.
        // CameraManager persists across the scene load, so we fade its alpha back to 0 here.
        FungusManager.Instance.CameraManager.Fade(0f, fadeInDuration, null);

        Debug.Log("GameInstance : " + GameState.Instance);
        Debug.Log("Returning From Battle : " + GameState.Instance.returningFromBattle);
        // Check if we're returning from battle
        if (GameState.Instance != null && GameState.Instance.returningFromBattle)
        {
            StartCoroutine(HandleReturnFromBattle());
        }
    }
    
    IEnumerator HandleReturnFromBattle()
    {
        // Give scene a moment to load
        yield return new WaitForSeconds(0.1f);
        
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        // Restore player position
        if (player != null)
        {
            player.transform.position = GameState.Instance.playerPositionBeforeBattle;
            Debug.Log($"Restored player to position: {player.transform.position}");
        }
        
        // Find and remove defeated enemy
        string enemyId = GameState.Instance.lastBattleEnemyId;
        if (!string.IsNullOrEmpty(enemyId))
        {
            string[] parts = enemyId.Split('_');
            if (parts.Length > 0)
            {
                string enemyName = parts[0];
                
                // Find all enemies
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                foreach (var enemy in enemies)
                {
                    // If this is the enemy that initiated battle
                    if (enemy.name == enemyName || enemy.name.StartsWith(enemyName + "("))
                    {
                        Debug.Log($"Removing defeated enemy: {enemy.name}");
                        Destroy(enemy);
                        break;
                    }
                }
            }
        }
        
        // Reset the returning flag
        GameState.Instance.returningFromBattle = false;
    }
}