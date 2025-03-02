using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour {
    
    [Header("Raycast Detection")]
    [SerializeField] private float groundRayLength = 0.1f;
    [SerializeField] private float wallRayLength = 0.1f;
    private LayerMask groundLayer;
    private LayerMask playerLayer;
    [SerializeField] private Vector2 groundRayOffset = new Vector2(0f, -0.5f);
    [SerializeField] private Vector2 wallRayOffset = new Vector2(0.5f, 0f);
    
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Movement Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float horizontalJumpForce = 3f;
    [SerializeField] private float idleInterval = 2f;
    
    // Animation parameter hashes for better performance
    private readonly int idleHash = Animator.StringToHash("Idle");
    private readonly int jumpStartHash = Animator.StringToHash("JumpStart");
    private readonly int jumpingHash = Animator.StringToHash("Jumping");
    private readonly int toFallHash = Animator.StringToHash("ToFall");
    private readonly int fallingHash = Animator.StringToHash("Falling");
    private readonly int landingHash = Animator.StringToHash("Landing");

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    public PolygonCollider2D polygonCollider;
    public CircleCollider2D damageCollider;
    
    public bool facingRight = true;
    public bool isGrounded = false;
    public bool wasGrounded = false;
    public bool isJumping = false;

    public float verticalMovementValue;
    public float horizontalMovementValue;
    
    public enum EnemyState {
        Idle,
        JumpStart,
        Jumping,
        ToFall,
        Falling,
        Landing
    }
    
    public EnemyState currentState = EnemyState.Idle;
    
    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        polygonCollider = GetComponent<PolygonCollider2D>();
        damageCollider = GetComponent<CircleCollider2D>();

        groundLayer = LayerMask.GetMask("LevelGeometry");
        playerLayer = LayerMask.GetMask("Player");

        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Start() {
        polygonCollider.enabled = true;
        damageCollider.enabled = false;
        startIdleCycle();
    }
    
    private void Update() {
        verticalMovementValue = rb.velocity.y;
        horizontalMovementValue = rb.velocity.x;

        wasGrounded = isGrounded;
        isGrounded = IsGrounded();
        
        HandleStateTransitions();
    }
    
    // Handle state transitions based on physics and grounding
    private void HandleStateTransitions() {
        // Reaching highest point of jump
        if (isJumping && !isGrounded && rb.velocity.y <= 0 && currentState == EnemyState.Jumping) {
            SetState(EnemyState.ToFall);
            animator.SetTrigger(toFallHash);
        }

        // Landing detection
        if (!wasGrounded && isGrounded) {
            LandingSequence();
        }
    }

    private void SetState(EnemyState newState) {
        currentState = newState;
        polygonCollider.enabled = (newState == EnemyState.Idle || newState == EnemyState.JumpStart || newState == EnemyState.Landing);
        damageCollider.enabled = (newState != EnemyState.Idle && newState != EnemyState.JumpStart && newState != EnemyState.Landing);
    }

    public void Idle(){
        SetState(EnemyState.Idle);
        animator.SetTrigger(idleHash);
        startIdleCycle();
    }

    public void startIdleCycle(){
        StartCoroutine(IdleCycle());
    }

    private IEnumerator IdleCycle() {
        yield return new WaitForSeconds(idleInterval);
        jumpPrepSequence();
    }
    
    private void jumpPrepSequence() {
        SetState(EnemyState.JumpStart);
        animator.SetTrigger(jumpStartHash);
    }
    
    public void PerformJump() {
        SetState(EnemyState.Jumping);
        animator.SetTrigger(jumpingHash);
        
        // Apply jump force
        int direction = facingRight ? 1 : -1;
        rb.velocity = new Vector2(horizontalJumpForce * direction, jumpForce);
        isJumping = true;
    }

    public void StayFalling(){
        SetState(EnemyState.Falling);
        animator.SetTrigger(fallingHash);
    }

    private void LandingSequence() {
        isJumping = false;
        SetState(EnemyState.Landing);
        animator.SetTrigger(landingHash);
        
        // Stop horizontal movement when landing
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    private bool IsGrounded() {
        // Adjust the origin position based on the offset
        Vector2 rayOrigin = (Vector2)transform.position + new Vector2(groundRayOffset.x * (facingRight ? 1 : -1), groundRayOffset.y);
        
        // Cast a ray downward to detect ground
        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            Vector2.down,
            groundRayLength,
            groundLayer
        );
        
        // Visualize the ray in scene view
        Debug.DrawRay(rayOrigin, Vector2.down * groundRayLength, hit.collider != null ? Color.green : Color.red);
        
        return hit.collider != null;
    }
    
    private bool isObstacleAhead() {
        // Determine the direction to check based on facing direction
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        
        // Adjust the origin position based on the offset
        Vector2 rayOrigin = (Vector2)transform.position + new Vector2(
            wallRayOffset.x * (facingRight ? 1 : -1),
            wallRayOffset.y
        );
        
        // Cast a ray to detect obstacles ahead
        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            direction,
            wallRayLength,
            groundLayer | playerLayer
        );
        
        // Visualize the ray in scene view
        Debug.DrawRay(rayOrigin, direction * wallRayLength, hit.collider != null ? Color.yellow : Color.red);
        
        return hit.collider != null;
    }
    
    private void Flip() {
        // Flip the facing direction
        facingRight = !facingRight;
        
        // Flip the sprite
        spriteRenderer.flipX = !facingRight;
    }
    
    private void OnCollisionEnter2D(Collision2D collision) {
        int collidedLayer = collision.gameObject.layer;
        LayerMask targetLayers = LayerMask.GetMask("Player", "LevelGeometry");

        // Check if we collided with the player
        if (targetLayers == (targetLayers | (1 << collidedLayer)) && isObstacleAhead()) {
            // If jumping and hit player from the side, change direction
            if (!isGrounded && isJumping) {
                Flip();
                
                // Apply small backwards impulse to prevent sticking
                int direction = facingRight ? 1 : -1;
                rb.velocity = new Vector2(horizontalJumpForce * direction * 0.5f, rb.velocity.y);
            }
        }
    }
    
    private void OnDrawGizmos() {
        bool faceRight = facingRight;
        if (!Application.isPlaying)
            faceRight = true; // Default facing in editor
            
        // Visualize ground check ray
        Gizmos.color = Color.green;
        Vector2 groundOrigin = (Vector2)transform.position + new Vector2(
            groundRayOffset.x * (faceRight ? 1 : -1),
            groundRayOffset.y
        );
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector2.down * groundRayLength);
        
        // Visualize wall check ray
        Gizmos.color = Color.yellow;
        Vector2 wallOrigin = (Vector2)transform.position + new Vector2(
            wallRayOffset.x * (faceRight ? 1 : -1),
            wallRayOffset.y
        );
        Vector2 direction = faceRight ? Vector2.right : Vector2.left;
        Gizmos.DrawLine(wallOrigin, wallOrigin + direction * wallRayLength);
    }
}