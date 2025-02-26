using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerMovementNormal : MonoBehaviour {
    public Vector2 moveInput { get; private set; }

    [SerializeField] float runSpeed = 5f;
    [SerializeField] float jumpSpeed = 12f;
    [SerializeField] float doubleJumpMultiplier = 0.75f;
    [SerializeField] float customGravity  = 2.5f;

    Rigidbody2D rigidBody;
    CapsuleCollider2D bodyCollider;
    private Vector2 _originalColliderSize;
    PlayerAttack playerAttack;

    [SerializeField] float raycastDistance = 0.1f;
    
    [SerializeField] LayerMask layerMask;
    [SerializeField] float slopeAngleLimit = 45f;
    [SerializeField] float downForceAdjustment = 1.2f;

    public float horizontalMovementValue = 0f;
    public float verticalMovementValue = 0f;
    private int _facingDirection = 1;

    #region validation properties
    public bool isGrounded = false;
    public bool isDucking = false;
    public bool canDoubleJump = true;
    public bool isDashing = false;
    public bool isStomping = false;
    private bool _inAirLastFrame;
    #endregion

    #region restrictive properties
    private bool canMove = true;
    private bool canJump = true;
    public bool canAttack = true;
    private bool canDuck = true;
    private bool canDash = true;
    private bool canStomp = true;
    #endregion

    // Dash variables
    [SerializeField] float dashSpeed = 15f;
    [SerializeField] float dashTime = 0.1f;
    [SerializeField] float dashCooldownTime = 0.25f;
    private float _dashTimer;
    [SerializeField] private bool dashed;

    // Stomp variables
    [SerializeField] float stompSpeed = 25f; // Speed of downward stomp
    [SerializeField] float stompHorizontalSpeed = 2f; // Limited horizontal movement during stomp
    [SerializeField] Vector2 stompHitboxSize = new Vector2(0.5f, 0.5f); // Size of stomp hitbox
    [SerializeField] LayerMask enemyLayerMask; // Layer for enemies to detect


    public event EventHandler OnDoubleJump;
    public event EventHandler OnStartDash;
    public event EventHandler OnStartStomp;


    void Start()  {
        rigidBody = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        playerAttack = GetComponent<PlayerAttack>();

        rigidBody.gravityScale = customGravity;
        _originalColliderSize = bodyCollider.size;
    }

    void Update() {
        Run();
        CheckDirection();
        CheckGrounded();
        Ducking();
        ConsumeDashTime();

        horizontalMovementValue = rigidBody.velocity.x;
        verticalMovementValue = rigidBody.velocity.y;
        _inAirLastFrame = !isGrounded;
    }

    public void OnMove(InputAction.CallbackContext context) {
        moveInput = context.ReadValue<Vector2>();
    }

    private void Run() {
        if (!canMove) return;

        if(isStomping){
            Vector2 stompVelocity = new Vector2(moveInput.x * stompHorizontalSpeed, -stompSpeed);
            rigidBody.velocity = stompVelocity;
        }
        else{
            bool isSprinting = Math.Abs(moveInput.x) >= 1 && Math.Abs(rigidBody.velocity.x) >= dashSpeed;
            if((dashed || isSprinting) && !isDucking){
                Vector2 runVelocity = new Vector2(moveInput.x * dashSpeed, rigidBody.velocity.y);
                rigidBody.velocity = runVelocity;
            }
            else{
                Vector2 runVelocity = new Vector2(moveInput.x * runSpeed, rigidBody.velocity.y);
                rigidBody.velocity = runVelocity;
            }
        }
    }

    private void CheckDirection() {
        if (rigidBody.velocity.x < 0) {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            _facingDirection = -1;
        }
        else if (rigidBody.velocity.x > 0) {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            _facingDirection = 1;
        }
    }

    public void OnJump (InputAction.CallbackContext context) {
        if(!canJump) return;
        
        if (context.started) {
            if(isGrounded){
                rigidBody.velocity += new Vector2(0f, jumpSpeed);
            }
            else if(canDoubleJump){
                canDoubleJump = false;
                OnDoubleJump?.Invoke(this, EventArgs.Empty);
                rigidBody.velocity = new Vector2(rigidBody.velocity.x, 0f); // Reset Y velocity to avoid stacking force
                rigidBody.velocity += new Vector2(0f, jumpSpeed * doubleJumpMultiplier);
            }

            // RhythmManager.Instance.RegisterAction(false);
        }
        else if (context.canceled) {
            if (rigidBody.velocity.y > 0) {
                rigidBody.velocity *= new Vector2(1f, 0.5f);
            }
        }
    }

    private void CheckGrounded(){
        RaycastHit2D groundRayCast = Physics2D.CapsuleCast(
            bodyCollider.bounds.center, 
            bodyCollider.size, 
            CapsuleDirection2D.Vertical, 
            0f, 
            Vector2.down, 
            raycastDistance, 
            layerMask
        );

        if(groundRayCast.collider){
            isGrounded = true;
            canDoubleJump = true;
            canDash = true;

            if (isStomping) {
                EndStomp();
                CheckStompHit();
            }
        }
        else{
            isGrounded = false;
        }

        // Cancel attack if the player hit the ground or just jumped
        if ((isGrounded && _inAirLastFrame) || (!isGrounded && !_inAirLastFrame)) {
            playerAttack.CancelAttack();
        }
    }

    private void Ducking(){
        if(!canDuck) return;

        if(!isGrounded){
            isDucking = false;
            if(bodyCollider.size != _originalColliderSize){
                bodyCollider.size = _originalColliderSize;
                bodyCollider.offset = new Vector2(0f, 0f);
            }
        }
        else if(moveInput.y < 0 && isGrounded && !isDucking){
            bodyCollider.size = new Vector2(bodyCollider.size.x, bodyCollider.size.y / 2);
            bodyCollider.offset = new Vector2(0f, -bodyCollider.size.y / 2);
            isDucking = true;
        }
        else if(moveInput.y >= 0 && isGrounded && isDucking){
            RaycastHit2D hitCeilingHigh = Physics2D.CapsuleCast(
                new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.center.y + (_originalColliderSize.y / 2)), 
                _originalColliderSize, 
                CapsuleDirection2D.Vertical, 
                0f, 
                Vector2.up, 
                raycastDistance, 
                layerMask
            );

            RaycastHit2D hitCeilingLow = Physics2D.CapsuleCast(
                bodyCollider.bounds.center, 
                bodyCollider.size, 
                CapsuleDirection2D.Vertical, 
                0f, 
                Vector2.up, 
                raycastDistance, 
                layerMask
            );

            // If the player is not touching a ceiling
            if (!hitCeilingHigh.collider && !hitCeilingLow.collider) { 
                bodyCollider.size = _originalColliderSize;
                bodyCollider.offset = new Vector2(0f, 0f);
                isDucking = false;
            }
        }
    }

    public void OnDash (InputAction.CallbackContext context) {
        if(!canDash && !canStomp) return;

        if (context.started) {
            if(canStomp && !isGrounded && !isStomping && !isDashing && moveInput.y < 0){
                StartStomp();
            }
            else{
                if(canDash && !isStomping && !isDashing && _dashTimer <= 0){
                    StartCoroutine(nameof(Dash));
                    if(Math.Abs(rigidBody.velocity.x) < dashSpeed){
                        rigidBody.velocity = new Vector2(rigidBody.velocity.x + (dashSpeed * _facingDirection), 0f);
                    }
                }
            }
        }
    }

    private void ConsumeDashTime() {
        if (_dashTimer > 0) _dashTimer -= Time.deltaTime;
    }

    IEnumerator Dash() {
        // Avoid multiple air dashes
        if(!isGrounded) canDash = false;

        rigidBody.gravityScale = 0;
        canMove = false;
        canAttack = false;
        canDuck = false;
        canStomp = false;
        isDashing = true;
        dashed = true;

        OnStartDash?.Invoke(this, EventArgs.Empty);
        yield return new WaitForSeconds(dashTime);

        rigidBody.gravityScale = customGravity;
        _dashTimer = dashCooldownTime;
        isDashing = false;

        canMove = true;
        canAttack = true;
        canDuck = true;
        canStomp = true;

        yield return new WaitForSeconds(1f);
        dashed = false;
    }

    private void StartStomp() {
        isStomping = true;
        canJump = false;
        canAttack = false;
        canDash = false;
        rigidBody.velocity = Vector2.zero; // Reset velocity before stomp
        OnStartStomp?.Invoke(this, EventArgs.Empty);
    }

    private void EndStomp() {
        isStomping = false;
        canMove = true;
        canJump = true;
        canAttack = true;
        canDash = true;
        dashed = true;
        StartCoroutine(nameof(KeepDashingStateForAMoment));
        rigidBody.velocity = new Vector2(rigidBody.velocity.x, 0f); // Stop vertical movement
    }

    IEnumerator KeepDashingStateForAMoment(){
        yield return new WaitForSeconds(1f);
        dashed = false;
    }

    // Check for enemy hits
    private void CheckStompHit() {
        Vector2 hitboxCenter = (Vector2)transform.position + Vector2.down * bodyCollider.size.y / 2;
        Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter, stompHitboxSize, 0f, enemyLayerMask);
        
        foreach (Collider2D hit in hits) {
            // Here you can add damage logic
            // For example: hit.GetComponent<Enemy>().TakeDamage(stompDamage);
            Debug.Log($"Stomped on: {hit.gameObject.name}");
        }
    }
}
