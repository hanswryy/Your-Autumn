using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject straightPathPrefab;    // One-branch
    public GameObject leftBranchPrefab;      // Two-branch left and straight
    public GameObject rightBranchPrefab;     // Two-branch right and straight
    public GameObject threeBranchPrefab;     // Three-branch

    public GameObject endPanel;
    
    [Header("Generation Settings")]
    public Transform startPoint;
    public Transform endPoint;
    public int pathDepth = 3;
    public float nodeSpacing = 10f;
    
    [Header("References")]
    public PathManager pathManager;
    
    // Lists to keep track of created nodes
    private List<GameObject> nodeObjects = new List<GameObject>();
    private List<PathNode> pathNodes = new List<PathNode>();
    private Dictionary<int, List<PathNode>> nodesByDepth = new Dictionary<int, List<PathNode>>();

    [Header("Enemy Settings")]
    public GameObject[] enemyPrefabs;
    public float enemySpawnChance = 0.5f;
    public bool spawnEnemyAtStart = false;
    public bool spawnEnemyAtEnd = false;
    
    void Start()
    {
        GeneratePath();
    }
    
    public void GeneratePath()
    {
        // // Find and destroy any existing PathNodes in the scene
        // PathNode[] existingNodes = GameObject.FindObjectsOfType<PathNode>();
        // foreach (var node in existingNodes)
        // {
        //     if (!nodeObjects.Contains(node.gameObject))
        //         Destroy(node.gameObject);
        // }

        ClearExistingPath();
        InitializeNodesByDepth();

        FindExistingPathNodes();
        
        // Step 1: Create start node (always one-way)
        GameObject startNodeObj = Instantiate(straightPathPrefab, startPoint.position, Quaternion.identity);
        PathNode startNode = startNodeObj.GetComponent<PathNode>();
        if (startNode == null) startNode = startNodeObj.AddComponent<PathNode>();
        
        startNode.nodeType = PathNode.NodeType.OneWay;
        startNode.depthLevel = 0;
        
        nodeObjects.Add(startNodeObj);
        pathNodes.Add(startNode);
        nodesByDepth[0].Add(startNode);
        
        // Step 2: Generate intermediate nodes (depth 1 to pathDepth-1)
        for (int depth = 1; depth < pathDepth; depth++)
        {
            GenerateNodesAtDepth(depth);
        }
        
        // Step 3: Create end node
        GameObject endNodeObj = Instantiate(straightPathPrefab, endPoint.position, Quaternion.identity);
        PathNode endNode = endNodeObj.GetComponent<PathNode>();
        if (endNode == null) endNode = endNodeObj.AddComponent<PathNode>();
        
        endNode.nodeType = PathNode.NodeType.OneWay;
        endNode.depthLevel = pathDepth;
        
        nodeObjects.Add(endNodeObj);
        pathNodes.Add(endNode);
        
        // Step 4: Connect nodes
        ConnectNodes();
        
        SpawnEnemiesAtNodes();
        
        // Step 5: Hide all nodes except the start node
        foreach (var node in pathNodes)
        {
            if (node != startNode)
                node.gameObject.SetActive(false);
        }
        
        // Tell PathManager which node to start with
        if (pathManager != null)
            pathManager.SetStartNode(startNode);
    }

    void FindExistingPathNodes()
    {
        // Find all PathNode components in the scene
        PathNode[] existingNodes = GameObject.FindObjectsOfType<PathNode>();
        
        foreach (var node in existingNodes)
        {
            // Skip nodes we already track (shouldn't happen at this point, but just in case)
            if (pathNodes.Contains(node))
                continue;
                
            // Add to tracking lists
            nodeObjects.Add(node.gameObject);
            pathNodes.Add(node);
            
            // Check if node has a depth level assigned, if not, assign depth 0
            if (node.depthLevel == 0)
            {
                // You might want to assign depths based on position or other criteria
                nodesByDepth[0].Add(node);
            }
            else if (node.depthLevel <= pathDepth)
            {
                // Add to appropriate depth list
                nodesByDepth[node.depthLevel].Add(node);
            }
            
            Debug.Log("Found existing path node: " + node.name + " with type " + node.nodeType);
        }
    }

    private void SpawnEnemiesAtNodes()
    {
        foreach (var node in pathNodes)
        {
            // Skip start node if configured
            if (node.depthLevel == 0 && !spawnEnemyAtStart)
                continue;
                
            // Skip end node if configured
            if (node.depthLevel == pathDepth && !spawnEnemyAtEnd)
                continue;
            
            // Let the node handle its own enemy spawning
            node.SpawnEnemy(enemyPrefabs, enemySpawnChance);
        }
    }
    
    void ClearExistingPath()
    {
        foreach (var nodeObj in nodeObjects)
        {
            if (nodeObj != null)
                Destroy(nodeObj);
        }
        
        nodeObjects.Clear();
        pathNodes.Clear();
        nodesByDepth.Clear();
    }
    
    void InitializeNodesByDepth()
    {
        for (int i = 0; i <= pathDepth; i++)
        {
            nodesByDepth[i] = new List<PathNode>();
        }
    }
    
    void GenerateNodesAtDepth(int depth)
    {
        // How many nodes to create at this depth
        int nodesToCreate = GetNodesCountForDepth(depth);
        
        for (int i = 0; i < nodesToCreate; i++)
        {
            // Calculate position based on depth and node index
            float xOffset = (i - (nodesToCreate - 1) / 2.0f) * nodeSpacing;
            Vector3 nodePos = Vector3.Lerp(startPoint.position, endPoint.position, (float)depth / pathDepth) + 
                              new Vector3(xOffset, 0, 0);
            
            // Select node type (except for depth 1 which needs at least one branching node)
            GameObject prefab = GetRandomNodePrefab(depth);
            
            // Create node
            GameObject nodeObj = Instantiate(prefab, nodePos, Quaternion.identity);
            PathNode node = nodeObj.GetComponent<PathNode>();
            if (node == null) node = nodeObj.AddComponent<PathNode>();
            
            // Set node properties based on prefab type
            SetNodeTypeBasedOnPrefab(node, prefab);
            node.depthLevel = depth;
            
            nodeObjects.Add(nodeObj);
            pathNodes.Add(node);
            nodesByDepth[depth].Add(node);
        }
    }
    
    int GetNodesCountForDepth(int depth)
    {
        // First depth has 3 nodes (to ensure branching)
        // Middle depths have 2-4 nodes
        // Final depth converges back to 2-3 nodes
        if (depth == 1) return 3;
        if (depth == pathDepth - 1) return Random.Range(2, 4);
        return Random.Range(2, 5);
    }
    
    GameObject GetRandomNodePrefab(int depth)
    {
        // First level after start should have at least one branching node
        if (depth == 1)
        {
            float choice = Random.value;
            if (choice < 0.4f)
                return leftBranchPrefab;
            else if (choice < 0.8f)
                return rightBranchPrefab;
            else
                return threeBranchPrefab;
        }
        
        // For other levels, random selection with weights
        float random = Random.value;
        
        // Last level before end should be mostly straight
        if (depth == pathDepth - 1)
        {
            if (random < 0.6f)
                return straightPathPrefab;
            else if (random < 0.8f)
                return leftBranchPrefab;
            else
                return rightBranchPrefab;
        }
        
        // Middle levels can have any type
        if (random < 0.25f)
            return straightPathPrefab;
        else if (random < 0.5f)
            return leftBranchPrefab;
        else if (random < 0.75f)
            return rightBranchPrefab;
        else
            return threeBranchPrefab;
    }
    
    void SetNodeTypeBasedOnPrefab(PathNode node, GameObject prefab)
    {
        if (prefab == straightPathPrefab)
            node.nodeType = PathNode.NodeType.OneWay;
        else if (prefab == leftBranchPrefab)
            node.nodeType = PathNode.NodeType.TwoWayLeft;
        else if (prefab == rightBranchPrefab)
            node.nodeType = PathNode.NodeType.TwoWayRight;
        else if (prefab == threeBranchPrefab)
            node.nodeType = PathNode.NodeType.ThreeWay;
    }
    
    void ConnectNodes()
    {
        // For each depth level except the last
        for (int depth = 0; depth < pathDepth; depth++)
        {
            List<PathNode> currentDepthNodes = nodesByDepth[depth];
            List<PathNode> nextDepthNodes = nodesByDepth[depth + 1];
            
            foreach (var currentNode in currentDepthNodes)
            {
                // Always create a forward connection if possible
                if (nextDepthNodes.Count > 0)
                {
                    int randomIndex = Random.Range(0, nextDepthNodes.Count);
                    PathNode targetNode = nextDepthNodes[randomIndex];

                    currentNode.forwardConnection = targetNode;

                    // Create exit trigger for forward direction
                    CreateExitTrigger(currentNode, "Forward");
                }
                
                // Create left/right connections based on node type
                if (currentNode.nodeType == PathNode.NodeType.TwoWayLeft || 
                    currentNode.nodeType == PathNode.NodeType.ThreeWay)
                {
                    // Left connection goes to a different node at the same depth level
                    // or to the next depth level if there's no other node at current depth
                    if (currentDepthNodes.Count > 1)
                    {
                        PathNode targetNode = GetRandomNodeExcept(currentDepthNodes, currentNode);
                        currentNode.leftConnection = targetNode;
                    }
                    else if (nextDepthNodes.Count > 1)
                    {
                        PathNode targetNode = GetRandomNodeExcept(nextDepthNodes, currentNode.forwardConnection);
                        currentNode.leftConnection = targetNode;
                    }
                    
                    // Create exit trigger for left direction
                    CreateExitTrigger(currentNode, "Left");
                }
                
                if (currentNode.nodeType == PathNode.NodeType.TwoWayRight || 
                    currentNode.nodeType == PathNode.NodeType.ThreeWay)
                {
                    // Right connection similar to left
                    if (currentDepthNodes.Count > 1)
                    {
                        PathNode targetNode = GetRandomNodeExcept(currentDepthNodes, currentNode);
                        if (currentNode.nodeType == PathNode.NodeType.ThreeWay)
                            targetNode = GetRandomNodeExcept(currentDepthNodes, currentNode, currentNode.leftConnection);
                            
                        currentNode.rightConnection = targetNode;
                    }
                    else if (nextDepthNodes.Count > 1)
                    {
                        List<PathNode> excludedNodes = new List<PathNode> { currentNode.forwardConnection };
                        if (currentNode.leftConnection != null)
                            excludedNodes.Add(currentNode.leftConnection);
                            
                        PathNode targetNode = GetRandomNodeExcept(nextDepthNodes, excludedNodes.ToArray());
                        currentNode.rightConnection = targetNode;
                    }
                    
                    // Create exit trigger for right direction
                    CreateExitTrigger(currentNode, "Right");
                }
            }
        }
        
        // Connect final depth level to end node
        PathNode endNode = pathNodes.Find(n => n.depthLevel == pathDepth);
        if (endNode != null)
        {
            List<PathNode> lastDepthNodes = nodesByDepth[pathDepth - 1];
            foreach (var node in lastDepthNodes)
            {
                // Make sure all final nodes connect to the end node
                if (node.forwardConnection == null)
                    node.forwardConnection = endNode;
            }
        }
    }
    
    PathNode GetRandomNodeExcept(List<PathNode> nodes, params PathNode[] except)
    {
        List<PathNode> availableNodes = new List<PathNode>(nodes);
        
        foreach (var node in except)
        {
            if (node != null)
                availableNodes.Remove(node);
        }
        
        if (availableNodes.Count == 0)
            return null;
            
        return availableNodes[Random.Range(0, availableNodes.Count)];
    }
    
    void CreateExitTrigger(PathNode node, string direction)
    {
        GameObject triggerObj = new GameObject("ExitTrigger_" + direction);
        triggerObj.transform.parent = node.transform;
        
        BoxCollider collider = triggerObj.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(5,5,5);
        
        ExitTrigger trigger = triggerObj.AddComponent<ExitTrigger>();
        trigger.ownerNode = node;
        trigger.exitDirection = direction;
        
        // Position the trigger based on direction
        Vector3 position = Vector3.zero;
        if (direction == "Forward")
        {
            position = Vector3.forward * 2;
            triggerObj.transform.localPosition = position;
            if (node.forwardExitPoint != null)
                triggerObj.transform.position = node.forwardExitPoint.position;
        }
        else if (direction == "Left")
        {
            position = Vector3.left * 2;
            triggerObj.transform.localPosition = position;
            if (node.leftExitPoint != null)
                triggerObj.transform.position = node.leftExitPoint.position;
        }
        else if (direction == "Right")
        {
            position = Vector3.right * 2;
            triggerObj.transform.localPosition = position;
            if (node.rightExitPoint != null)
                triggerObj.transform.position = node.rightExitPoint.position;
        }
    }
}