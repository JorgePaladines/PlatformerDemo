using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeMovement : MonoBehaviour {
    private Enemy enemy;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float horizontalJumpForce = 3f;
    private float startTime;
    private float idleInterval = 2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    public enum EnemyState {
        Idle,
        JumpStart,
        Jumping,
        ToFall,
        Falling,
        Landing
    }
    
    public EnemyState currentState = EnemyState.Idle;

    void Start() {
        enemy = GetComponent<Enemy>();
        animator = GetComponent<Animator>();
        StartMovementCycle();
    }

    void Update() {
        HandleStateTransitions();
    }

    private void StartMovementCycle() {
        startTime = Random.Range(0.5f, 3f);
        enemy.facingRight = Random.Range(0, 2) * 2 - 1 == 1;
        if(!enemy.facingRight){
            enemy.spriteRenderer.flipX = !enemy.facingRight;
        }
        
        Invoke("startIdleCycle", startTime);
    }

    // Handle state transitions based on physics and grounding
    private void HandleStateTransitions() {
        // Reaching highest point of jump
        if (enemy.isJumping && !enemy.isGrounded && enemy.rb.velocity.y <= 0.5 && currentState == EnemyState.Jumping) {
            SetState(EnemyState.ToFall);
        }

        // Landing detection
        if (!enemy.wasGrounded && enemy.isGrounded && enemy.isJumping) {
            LandingSequence();
        }
    }

    private void SetState(EnemyState newState) {
        currentState = newState;
        animator.SetTrigger(newState.ToString());
        enemy.bodyCollider.enabled = newState == EnemyState.Idle || newState == EnemyState.JumpStart || newState == EnemyState.Landing;
        enemy.damageCollider.enabled = !enemy.bodyCollider.enabled;
    }

    public void Idle(){
        SetState(EnemyState.Idle);
        startIdleCycle();
    }

    public void startIdleCycle(){
        StartCoroutine(IdleCycle());
    }

    private IEnumerator IdleCycle() {
        yield return new WaitForSeconds(idleInterval);
        SetState(EnemyState.JumpStart);
    }

    public void PerformJump() {
        SetState(EnemyState.Jumping);
        
        // Apply jump force
        int direction = enemy.facingRight ? 1 : -1;
        enemy.rb.velocity = new Vector2(horizontalJumpForce * direction, jumpForce);
        enemy.isJumping = true;
    }

    public void StayFalling(){
        SetState(EnemyState.Falling);
    }

    private void LandingSequence() {
        enemy.isJumping = false;
        SetState(EnemyState.Landing);
        
        // Stop horizontal movement when landing
        enemy.rb.velocity = new Vector2(0, enemy.rb.velocity.y);
    }

    private void Flip() {
        enemy.facingRight = !enemy.facingRight;
        enemy.spriteRenderer.flipX = !enemy.facingRight;
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        int collidedLayer = collision.gameObject.layer;
        LayerMask targetLayers = LayerMask.GetMask("Player", "LevelGeometry");

        // Check if we collided with the player
        if (targetLayers == (targetLayers | (1 << collidedLayer)) && enemy.isObstacleAhead()) {
            // If jumping and hit player from the side, change direction
            if (!enemy.isGrounded && enemy.isJumping) {
                Flip();
                
                // Apply small backwards impulse to prevent sticking
                int direction = enemy.facingRight ? 1 : -1;
                enemy.rb.velocity = new Vector2(horizontalJumpForce * direction * 0.5f, enemy.rb.velocity.y);
            }
        }
    }
}
