using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour {
    
    [Header("Raycast Detection")]
    [SerializeField] GameObject checkGroundPoint;
    [SerializeField] private float groundRayLength = 0.1f;
    [SerializeField] private float wallRayLength = 0.2f;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] LayerMask playerLayer;

    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;
    public PolygonCollider2D bodyCollider;
    public CircleCollider2D damageCollider;
    private float minX;
    private float maxX;
    private float minY;
    private float maxY;
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
        bodyCollider = GetComponent<PolygonCollider2D>();
        damageCollider = GetComponent<CircleCollider2D>();
        spriteBreaker = GetComponent<SpriteBreaker>();

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
        IsGrounded();
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

    public void IsGrounded() {
        RaycastHit2D hit = Physics2D.Raycast(
            checkGroundPoint.transform.position,
            Vector2.down,
            groundRayLength,
            groundLayer
        );

        Debug.DrawRay(checkGroundPoint.transform.position, Vector2.down * groundRayLength, hit.collider != null ? Color.green : Color.red);
        isGrounded = hit.collider != null;
    }
    
    public bool isObstacleAhead() {
        // Determine the direction to check based on facing direction
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        
        // Cast a ray to detect obstacles ahead
        RaycastHit2D hit = Physics2D.Raycast(
            damageCollider.bounds.center,
            direction,
            damageCollider.radius + wallRayLength,
            groundLayer | playerLayer
        );
        
        // Visualize the ray in scene view
        Debug.DrawRay(damageCollider.bounds.center, direction * (damageCollider.radius + wallRayLength), hit.collider != null ? Color.red : Color.yellow);

        return hit.collider != null;
    }
}