using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance { get; private set; }
    
    [Header("Transition Settings")]
    public float fadeTime = 1.0f;
    public CanvasGroup fadePanel;
    
    [Header("Player")]
    public GameObject player;
    
    private PathNode currentNode;
    private bool isTransitioning = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Modify your SetStartNode and PerformTransition methods to use the coroutine
    public void SetStartNode(PathNode startNode)
    {
        currentNode = startNode;
        StartCoroutine(PlacePlayerAtCurrentNodeCoroutine());
    }

    private IEnumerator PerformTransition(PathNode targetNode)
    {
        isTransitioning = true;

        // Fade out
        yield return FadeScreen(0, 1);

        // Hide current node
        currentNode.gameObject.SetActive(false);

        // Show target node
        targetNode.gameObject.SetActive(true);

        // Wait for node to fully initialize
        yield return null;

        // Update current node reference
        currentNode = targetNode;

        // Place player at spawn point
        yield return PlacePlayerAtCurrentNodeCoroutine();

        // Fade in
        yield return FadeScreen(1, 0);

        isTransitioning = false;
    }
    
    public void TransitionToNode(PathNode targetNode)
    {
        if (isTransitioning) return;
        
        StartCoroutine(PerformTransition(targetNode));
    }
    
    // Replace your PlacePlayerAtCurrentNode method with this coroutine version
    private IEnumerator PlacePlayerAtCurrentNodeCoroutine()
    {
        if (player != null && currentNode != null && currentNode.spawnPoint != null)
        {
            // Make sure node is active
            if (!currentNode.gameObject.activeInHierarchy)
            {
                Debug.LogError("Node must be active before accessing spawn point position!");
                yield break;
            }
            
            // Get spawn position
            Vector3 spawnPos = currentNode.spawnPoint.position;
            spawnPos.y += 0.1f; // Small offset to prevent floor clipping
            
            Debug.Log("Attempting to place player at: " + spawnPos);
            
            // Disable any character controller if present
            CharacterController charController = player.GetComponent<CharacterController>();
            if (charController != null)
                charController.enabled = false;
                
            // Disable any player movement scripts
            MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
            List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
            
            foreach (var script in scripts)
            {
                if (script.GetType().Name.ToLower().Contains("move") || 
                    script.GetType().Name.ToLower().Contains("control"))
                {
                    if (script.enabled)
                    {
                        script.enabled = false;
                        disabledScripts.Add(script);
                    }
                }
            }
            
            // Reset rigidbody
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Force position in several frames to overcome any other scripts
            for (int i = 0; i < 5; i++)
            {
                player.transform.position = spawnPos;
                yield return null;
            }
            
            // Re-enable components
            if (charController != null)
                charController.enabled = true;
                
            if (rb != null)
                rb.isKinematic = false;
                
            foreach (var script in disabledScripts)
            {
                script.enabled = true;
            }
            
            // One final position set after everything is re-enabled
            player.transform.position = spawnPos;
            
            Debug.Log("Final player position: " + player.transform.position);
        }
        else
        {
            Debug.LogWarning("Cannot place player - missing required reference");
        }
    }

    
    private IEnumerator FadeScreen(float from, float to)
    {
        if (fadePanel != null)
        {
            float elapsedTime = 0;
            fadePanel.alpha = from;
            
            while (elapsedTime < fadeTime)
            {
                fadePanel.alpha = Mathf.Lerp(from, to, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            fadePanel.alpha = to;
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }
    }
}