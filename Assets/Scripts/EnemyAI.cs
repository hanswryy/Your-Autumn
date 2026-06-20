using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Battle Settings")]
    public bool enableBattleTransition = true;
    public string battleSceneName = "BattleScene";
    public float collisionCooldown = 1.0f;
    private float lastCollisionTime = -10f;

    [Header("Movement Settings")]
    public float idleTime = 2f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float patrolDistance = 5f; // How far to walk in each direction

    [Header("Detection Settings")]
    public float detectionRange = 8f;
    public float losePlayerRange = 10f;
    public Transform eyePosition; // Enemy's line of sight origin
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("References")]
    public Animator animator; // Optional

    // State management
    private enum AIState { Idle, Patrol, Chase }
    private AIState currentState;

    // Movement tracking
    private Vector3 startPosition;
    private Vector3 leftPatrolPoint;
    private Vector3 rightPatrolPoint;
    private bool movingRight = true;
    private float stateTimer;
    private Transform playerTransform;

    private Rigidbody rb;
    private bool isFacingRight = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        leftPatrolPoint = startPosition - new Vector3(patrolDistance, 0, 0);
        rightPatrolPoint = startPosition + new Vector3(patrolDistance, 0, 0);

        // If no eye position is set, use the transform position
        if (eyePosition == null)
            eyePosition = transform;

        // Initialize state
        ChangeState(AIState.Idle);

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        if (animator != null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdleState();
                break;
            case AIState.Patrol:
                UpdatePatrolState();
                break;
            case AIState.Chase:
                UpdateChaseState();
                break;
        }

        // Always check for player detection
        CheckForPlayerDetection();
    }

    private void UpdateIdleState()
    {
        // Stay still and wait
        if (animator != null)
            animator.SetBool("isWalking", false);

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0)
            ChangeState(AIState.Patrol);
    }

    private void UpdatePatrolState()
    {
        // Walk back and forth
        if (animator != null)
            animator.SetBool("isWalking", true);

        Vector3 targetPoint = movingRight ? rightPatrolPoint : leftPatrolPoint;
        Vector3 moveDirection = (targetPoint - transform.position).normalized;

        // Move towards target
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPoint,
            patrolSpeed * Time.deltaTime
        );

        // Check if we need to flip direction
        if (Vector3.Distance(transform.position, targetPoint) < 0.1f)
        {
            movingRight = !movingRight;
            Flip();
        }

        // Update facing direction based on movement
        if (moveDirection.x > 0 && !isFacingRight)
            Flip();
        else if (moveDirection.x < 0 && isFacingRight)
            Flip();
    }

    private void UpdateChaseState()
    {
        if (playerTransform == null)
        {
            ChangeState(AIState.Idle);
            return;
        }

        // Move towards player
        if (animator != null)
            animator.SetBool("isWalking", true);

        Vector3 targetPosition = new Vector3(
            playerTransform.position.x,
            transform.position.y,
            transform.position.z
        );

        Vector3 moveDirection = (targetPosition - transform.position).normalized;

        // Move towards player
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            chaseSpeed * Time.deltaTime
        );

        // Update facing direction based on player position
        if (moveDirection.x > 0 && !isFacingRight)
            Flip();
        else if (moveDirection.x < 0 && isFacingRight)
            Flip();

        // Check if we lost the player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > losePlayerRange || !CanSeePlayer())
        {
            ChangeState(AIState.Idle);
        }
    }

    private void CheckForPlayerDetection()
    {
        // Skip if already chasing or player not found
        if (currentState == AIState.Chase || playerTransform == null)
            return;

        if (CanSeePlayer())
        {
            ChangeState(AIState.Chase);
        }
    }

    private bool CanSeePlayer()
    {
        if (playerTransform == null)
            return false;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRange)
        {
            // Check line of sight
            Vector3 directionToPlayer = (playerTransform.position - eyePosition.position).normalized;
            RaycastHit hit;

            if (Physics.Raycast(eyePosition.position, directionToPlayer, out hit, detectionRange, playerLayer | obstacleLayer))
            {
                // If the first thing we hit is the player, we can see them
                if (hit.transform == playerTransform)
                {
                    Debug.DrawLine(eyePosition.position, hit.point, Color.red);
                    return true;
                }
            }
        }

        return false;
    }

    private void ChangeState(AIState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case AIState.Idle:
                stateTimer = idleTime;
                break;

            case AIState.Patrol:
                // No special initialization needed
                break;

            case AIState.Chase:
                // No special initialization needed
                break;
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Visualize patrol points if set
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(leftPatrolPoint, 0.2f);
            Gizmos.DrawSphere(rightPatrolPoint, 0.2f);
            Gizmos.DrawLine(leftPatrolPoint, rightPatrolPoint);
        }
    }
    
    // Add this method to EnemyAI.cs
    private void OnCollisionEnter(Collision collision)
    {
        // Check if it's the player and we're not on cooldown
        if (collision.gameObject.CompareTag("Player") && 
            Time.time > lastCollisionTime + collisionCooldown)
        {
            if (enableBattleTransition && currentState == AIState.Chase)
            {
                lastCollisionTime = Time.time;
                if (GameState.Instance != null)
                {
                    Debug.Log("Battle initiated by " + gameObject.name + " against: " + collision.gameObject.name);
                    GameState.Instance.StartBattle(this.gameObject);
                }
            }
        }
    }
}