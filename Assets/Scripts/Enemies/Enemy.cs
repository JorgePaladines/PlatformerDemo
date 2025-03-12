using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour {
    
    [Header("Raycast Detection")]
    [SerializeField] private float groundRayLength = 0.1f;
    [SerializeField] private float wallRayLength = 0.2f;
    private LayerMask groundLayer;
    private LayerMask playerLayer;
    [SerializeField] private Vector2 groundRayOffset = new Vector2(0f, -0.5f);
    [SerializeField] private Vector2 wallRayOffset = new Vector2(0.5f, 0f);

    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;
    public Collider2D bodyCollider;
    public CircleCollider2D damageCollider;
    private Color originalColor;
    public float invulnerabilityDuration = 0.3f;
    [SerializeField] Color flashColor;
    [SerializeField] ParticleSystem hitEffect;
    private SpriteBreaker spriteBreaker;

    public bool facingRight;
    public bool isGrounded = false;
    public bool wasGrounded = false;
    public bool isJumping = false;
    public float verticalMovementValue;
    public float horizontalMovementValue;    
    
    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        damageCollider = GetComponent<CircleCollider2D>();
        spriteBreaker = GetComponent<SpriteBreaker>();

        groundLayer = LayerMask.GetMask("LevelGeometry");
        playerLayer = LayerMask.GetMask("Player");

        originalColor = spriteRenderer.color;
    }

    private void Start() {
        bodyCollider.enabled = true;
        if(damageCollider != null){
            damageCollider.enabled = false;
        }
    }
    
    private void Update() {
        verticalMovementValue = rb.velocity.y;
        horizontalMovementValue = rb.velocity.x;

        wasGrounded = isGrounded;
        isGrounded = IsGrounded();
    }

    public void TakeDamage(){
        StartCoroutine(FlashSprite());
    }

    private IEnumerator FlashSprite() {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(invulnerabilityDuration);
        spriteRenderer.color = originalColor;
    }

    public void Die(){
        if(hitEffect != null){
            ParticleSystem instance = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(instance.gameObject, instance.main.duration + instance.main.startLifetime.constantMax);
        }
        if(spriteBreaker != null){
            bodyCollider.enabled = false;
            spriteBreaker.Break();
        }
        else {
            Destroy(gameObject);
        }
    }

    public bool IsGrounded() {
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
    
    public bool isObstacleAhead() {
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