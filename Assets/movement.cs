using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class movement : MonoBehaviour
{
    [Header("Battle Settings")]
    public bool canInitiateBattles = true;
    public float battleCooldown = 0.5f;
    private float lastBattleTime = -10f;
    public float speed = 5.0f;
    public float jumpForce;
    public float maxHoldTime = 1.0f;
    public float timeToHit = 0.4f;

    private Rigidbody rb;
    private bool isJumping = false;
    private float jumpTime;
    public float jumpStartTime;
    private bool isGrounded = true;
    private bool isKnockedBack = false;

    public CinemachineVirtualCamera virtualCamera;
    public float cameraOffset = 5.0f;
    public float smoothTime = 0.3f;

    private CinemachineTransposer transposer;
    private Vector3 currentVelocity;

    // Attack Variables
    public float attackRange = 1.5f;
    public int attackDamage = 10;
    public Transform attackPoint;
    public LayerMask enemyLayers;
    public float attackCooldown = 0.5f;
    private float nextAttackTime = 0f;
    private bool isFacingRight = true;
    // Add this with your other private variables
    private bool isAttacking = false;
    public Animator swordAnim;

    // Slash Visual Effect
    [Header("Attack VFX")]
    public float slashAnimDuration = 0.5f; // how long isSlash stays true before resetting
    public GameObject hitFlashPrefab;      // spark spawned on enemies actually in range
    public float hitFlashLifetime = 0.4f;

    // Knockback Variables
    public float knockbackForce = 5.0f;
    public float knockbackDuration = 1f;

    public Animator playerAnimator;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (virtualCamera != null)
        {
            transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        }
    }

    void OnEnable()
    {
        // Battles are now loaded additively, so the player object is suspended and
        // re-enabled rather than recreated. The attack that starts a battle leaves
        // isAttacking == true (it yield-breaks before clearing it), which would block
        // FixedUpdate forever. Clear any leftover action state whenever we come back.
        isAttacking = false;
        isKnockedBack = false;
        if (swordAnim != null)
        {
            swordAnim.SetBool("isSlash", false);
            swordAnim.gameObject.SetActive(false);
        }
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isRunning", false);
            playerAnimator.ResetTrigger("slash");
        }
    }

    private float moveHorizontal;
    private float moveVertical;

    void Update()
    {
        // Get input in Update for responsive controls
        moveHorizontal = Input.GetAxisRaw("Horizontal");
        moveVertical = Input.GetAxisRaw("Vertical");
        
        // Handle non-physics inputs here
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextAttackTime && !InteractTrigger.PlayerInInteractZone)
        {
            Debug.Log("Attack initiated");
            Attack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void FixedUpdate()
    {
        if (isAttacking || isKnockedBack || InteractTrigger.IsDialogOpen)
        {
            if (InteractTrigger.IsDialogOpen)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
                if (playerAnimator != null) playerAnimator.SetBool("isRunning", false);
            }
            return;
        }
        if (!isKnockedBack)
        {
            // Apply stored input values to physics
            if (moveHorizontal != 0 || moveVertical != 0)
            {
                Move(moveHorizontal, moveVertical);
            }
            else
            {
                // Stop movement immediately when no input
                rb.velocity = new Vector3(0, rb.velocity.y, 0);

                // Add this to update animation when stopping
                if (playerAnimator != null)
                {
                    playerAnimator.SetBool("isRunning", false);
                }
            }
        }

        if (isGrounded && !isJumping)
        {
            // Optional: Reset Y velocity when grounded
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        }
    }


    void Move(float horizontal, float vertical)
    {
        Vector3 movement = new Vector3(horizontal, 0, vertical).normalized;

        // Always set horizontal velocity based on input
        rb.velocity = new Vector3(movement.x * speed, rb.velocity.y, movement.z * speed);

        // Check if moving to set animation
        bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;

        // Set animation based on movement
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isRunning", isMoving);
        }

        // Flip the character based on movement direction
        if (horizontal > 0 && !isFacingRight)
            Flip();
        else if (horizontal < 0 && isFacingRight)
            Flip();

        // Adjust camera offset
        if (transposer != null)
        {
            Vector3 targetOffset = transposer.m_FollowOffset;
            targetOffset.x = isFacingRight ? cameraOffset : -cameraOffset;
            transposer.m_FollowOffset = Vector3.SmoothDamp(
                transposer.m_FollowOffset,
                targetOffset,
                ref currentVelocity,
                smoothTime
            );
        }
    }


    void Flip()
    {
        isFacingRight = !isFacingRight;
        
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    void Attack()
    {
        Debug.Log("Attacking!");

        // Detect enemies within attack range
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);
        isAttacking = true;
        playerAnimator.SetBool("isRunning", false); 
        
        // Play attack animation
        playerAnimator.SetTrigger("slash");

        // Play the sword's slash animation (drives the SlashVFX trail)
        PlaySlashAnimation();

        // Check for battle initiation
        bool battleStarted = false;

        // Start a single coroutine that handles both battle initiation and attack cooldown
        StartCoroutine(AttackSequence(hitEnemies));
    }

    IEnumerator AttackSequence(Collider[] enemies)
    {
        // Wait until the mid-point of the animation (the actual "hit")
        yield return new WaitForSeconds(timeToHit);
        
        // At this point the attack "connects" - process battle initiation
        bool battleStarted = false;
        foreach (Collider enemy in enemies)
        {
            if (enemy == null) continue;
            Debug.Log("Processing hit on: " + enemy.name + " at animation mid-point");

            // This enemy was in range when the attack connected - flash it
            SpawnHitFlash(enemy.bounds.center);

            if (canInitiateBattles && Time.time > lastBattleTime + battleCooldown && !battleStarted)
            {
                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                if (enemyAI != null && enemyAI.enableBattleTransition)
                {
                    lastBattleTime = Time.time;
                    battleStarted = true;
                    GameState.Instance.StartBattle(enemy.gameObject);
                    // If battle started, we can return early as the scene will change
                    yield break;
                }
            }
        }
        
        // If no battle was initiated, wait for the rest of the animation
        float remainingAnimTime = 0.25f; // Remaining animation time
        yield return new WaitForSeconds(remainingAnimTime);
        
        // Reset attack state to allow movement
        isAttacking = false;
    }

    void PlaySlashAnimation()
    {
        if (swordAnim == null) return;
        // Enable the sword only for the attack window
        swordAnim.gameObject.SetActive(true);
        swordAnim.SetBool("isSlash", true);
        StartCoroutine(ResetSlashBool());
    }

    IEnumerator ResetSlashBool()
    {
        yield return new WaitForSeconds(slashAnimDuration);
        if (swordAnim != null)
        {
            swordAnim.SetBool("isSlash", false);
            swordAnim.gameObject.SetActive(false);
        }
    }

    void SpawnHitFlash(Vector3 position)
    {
        if (hitFlashPrefab == null) return;
        GameObject flash = Instantiate(hitFlashPrefab, position, Quaternion.identity);
        Destroy(flash, hitFlashLifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            Debug.Log("Grounded");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Vector3 knockbackDirection = (transform.position - other.transform.position).normalized;
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
            StartCoroutine(KnockbackCoroutine());
            Debug.Log("Knocked back by enemy");
        }
    }

    IEnumerator KnockbackCoroutine()
    {
        isKnockedBack = true;
        yield return new WaitForSeconds(knockbackDuration);
        isKnockedBack = false;
    }

    // Debugging attack range in Unity
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
