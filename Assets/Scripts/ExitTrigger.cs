using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    public PathNode ownerNode;
    public string exitDirection; // "Forward", "Left", "Right"
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PathNode targetNode = null;
            
            // Get the target node based on exit direction
            switch (exitDirection)
            {
                case "Forward":
                    targetNode = ownerNode.forwardConnection;
                    break;
                case "Left":
                    targetNode = ownerNode.leftConnection;
                    break;
                case "Right":
                    targetNode = ownerNode.rightConnection;
                    break;
            }

            if (targetNode != null)
            {
                PathGenerator generator = FindObjectOfType<PathGenerator>();
                if (generator != null && targetNode.depthLevel == generator.pathDepth-1)
                {
                    // Hide the end panel if it exists
                    generator.endPanel.SetActive(true);
                }

                // Tell PathManager to handle the transition
                PathManager.Instance.TransitionToNode(targetNode);

            }
        }
    }
}