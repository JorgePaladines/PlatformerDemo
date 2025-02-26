using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerMovement : MonoBehaviour {
    [SerializeField] float runSpeed = 5f;
    [SerializeField] float crouchSpeed = 2f;
    [SerializeField] float jumpSpeed = 18f;
    [SerializeField] float doubleJumpMultiplier = 0.75f;
    [SerializeField] float customGravity  = 4f;

    public Vector2 moveInput { get; private set; }
    Rigidbody2D rigidBody;
    CapsuleCollider2D bodyCollider;
    private Vector2 _originalColliderSize;
    PlayerAttack playerAttack;
    [SerializeField] float raycastDistance = 0.1f;
    [SerializeField] LayerMask layerMask;
    // [SerializeField] float slopeAngleLimit = 45f;
    // [SerializeField] float downForceAdjustment = 1.2f;

    public float horizontalMovementValue;
    public float verticalMovementValue;
    private int _facingDirection = 1; // Direction the playable character faces
    private int _playerDirection; // Direction the player is trying to go to

    #region validation properties
    public bool isGrounded = false;
    public bool isDucking = false;
    public bool canDoubleJump = true;
    public bool isDashing = false;
    public bool isSprinting = false;
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

    #region Dash Variables
    [SerializeField] float dashSpeed = 15f;
    [SerializeField] float dashTime = 0.1f;
    [SerializeField] float dashCooldownTime = 0.25f;
    private float _dashTimer;
    private bool keepSprintingState;
    #endregion

    #region Stomp Variables
    [SerializeField] float stompSpeed = 23f; // Speed of downward stomp
    [SerializeField] float stompHorizontalSpeed = 2f; // Limited horizontal movement during stomp
    // [SerializeField] Vector2 stompHitboxSize = new Vector2(0.5f, 0.5f); // Size of stomp hitbox
    [SerializeField] LayerMask enemyLayerMask; // Layer for enemies to detect
    #endregion

    #region deceleration variables
    [SerializeField] float normalDeceleration = 15f;     // Rate of normal slowing
    [SerializeField] float sharpDeceleration = 40f;      // Rate when changing directions
    [SerializeField] float minVelocityThreshold = 0.1f;  // When to snap to zero
    [SerializeField] float minTurnThreshold = 2f; // Minimum speed below which turning is allowed
    private float _currentHorizontalVelocity;  // Track current velocity separately
    #endregion

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

        if (isStomping) {
            Vector2 stompVelocity = new Vector2(moveInput.x * stompHorizontalSpeed, -stompSpeed);
            rigidBody.velocity = stompVelocity;
        }
        else {
            // Check if player should be sprinting (post-dash or post-stomp)
            isSprinting = keepSprintingState && !isDucking && Math.Abs(moveInput.x) >= 1;
            float targetVelocity;

            // Determine target velocity
            if (isSprinting) {
                targetVelocity = moveInput.x * dashSpeed; // Maintain dash speed during sprint
            }
            else if (isDucking) {
                targetVelocity = moveInput.x * crouchSpeed;
                if (keepSprintingState && isDucking) keepSprintingState = false; // Reset sprint only if ducking during sprint
            } 
            else {
                targetVelocity = moveInput.x * runSpeed; // Normal run speed otherwise
                keepSprintingState = false; // Reset sprint state if not sprinting and moving normally
            }

            // Calculate deceleration rate
            float decelerationRate = normalDeceleration;
            if (moveInput.x != 0 && Mathf.Sign(moveInput.x) != Mathf.Sign(_currentHorizontalVelocity)) {
                decelerationRate = sharpDeceleration; // Sharper deceleration on direction change
            }

            // Prevent instant direction change when moving at high speed (unless dashing)
            bool isMovingFast = Mathf.Abs(_currentHorizontalVelocity) > minTurnThreshold;
            bool tryingToTurn = moveInput.x != 0 && Mathf.Sign(moveInput.x) != Mathf.Sign(_currentHorizontalVelocity);

            if (isMovingFast && tryingToTurn && !isDashing) { // Dash can bypass this
                // Decelerate to zero instead of flipping direction
                _currentHorizontalVelocity = Mathf.MoveTowards(_currentHorizontalVelocity, 0f, sharpDeceleration * Time.deltaTime);
                keepSprintingState = false; // End sprint state during turn attempt
            }
            else if (!isSprinting || moveInput.x == 0) {
                // Smoothly interpolate velocity unless sprinting and moving in same direction
                _currentHorizontalVelocity = Mathf.MoveTowards(_currentHorizontalVelocity, targetVelocity, decelerationRate * Time.deltaTime);
            }
            else {
                // If sprinting and moving, maintain dashSpeed instantly in direction
                _currentHorizontalVelocity = targetVelocity;
            }

            // Interrupt sprinting if input stops
            if (isSprinting && moveInput.x == 0) {
                keepSprintingState = false; // End sprint state on stop
            }

            // Snap to zero when very close and no input
            if (Mathf.Abs(_currentHorizontalVelocity) < minVelocityThreshold && targetVelocity == 0) {
                _currentHorizontalVelocity = 0f;
            }

            // Apply the velocity (can be overridden by dash or ducking stop)
            rigidBody.velocity = new Vector2(_currentHorizontalVelocity, rigidBody.velocity.y);
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

        _playerDirection = Math.Sign(moveInput.x) >= 0 ? 1 : -1;
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
                // CheckStompHit();
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
    
    private void Ducking() {
        if (!canDuck && !isDashing && !isSprinting) return;

        if (!isGrounded) {
            isDucking = false;
            if (bodyCollider.size != _originalColliderSize) {
                bodyCollider.size = _originalColliderSize;
                bodyCollider.offset = new Vector2(0f, 0f);
            }
        }
        else if (moveInput.y < 0 && Math.Abs(_currentHorizontalVelocity) <= crouchSpeed && isGrounded && !isDucking && !isDashing && !isSprinting) {
            bodyCollider.size = new Vector2(bodyCollider.size.x, bodyCollider.size.y / 2);
            bodyCollider.offset = new Vector2(0f, -bodyCollider.size.y / 2);
            isDucking = true;
        }
        else if (moveInput.y >= 0 && isGrounded && isDucking) {
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

            if (!hitCeilingHigh.collider && !hitCeilingLow.collider && !isDashing) {
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
                        int directionToDash = moveInput.x == 0 ? _facingDirection : _playerDirection;
                        _currentHorizontalVelocity = dashSpeed * directionToDash;
                        rigidBody.velocity = new Vector2(_currentHorizontalVelocity, 0f);
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
        if (!isGrounded) canDash = false;

        rigidBody.gravityScale = 0;
        _dashTimer = dashCooldownTime + dashTime; // Set cooldown immediately
        canMove = false;
        canAttack = false;
        canDuck = false;
        canStomp = false;
        isDashing = true;
        keepSprintingState = true; // Enable sprinting after dash ends

        OnStartDash?.Invoke(this, EventArgs.Empty);
        yield return new WaitForSeconds(dashTime);

        rigidBody.gravityScale = customGravity;
        isDashing = false;

        // Check if ducking when dash ends and stop immediately
        if (isDucking) {
            _currentHorizontalVelocity = 0f; // Stop immediately
            rigidBody.velocity = new Vector2(0f, rigidBody.velocity.y);
            keepSprintingState = false; // Prevent sprinting from resuming
        }

        canMove = true;
        canAttack = true;
        canDuck = true;
        canStomp = true;

        yield return new WaitForSeconds(dashCooldownTime); // Wait for cooldown
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
        keepSprintingState = true; // Enable sprinting after stomp

        // Instantly set velocity to dashSpeed if moving
        if (Math.Abs(moveInput.x) >= 1) {
            _currentHorizontalVelocity = moveInput.x * dashSpeed;
        } else {
            _currentHorizontalVelocity = 0f; // Reset if no input
        }

        rigidBody.velocity = new Vector2(_currentHorizontalVelocity, 0f); // Apply immediately
    }

    // Check for enemy hits
    // private void CheckStompHit() {
    //     Vector2 hitboxCenter = (Vector2)transform.position + Vector2.down * bodyCollider.size.y / 2;
    //     Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter, stompHitboxSize, 0f, enemyLayerMask);
        
    //     foreach (Collider2D hit in hits) {
    //         // Here you can add damage logic
    //         // For example: hit.GetComponent<Enemy>().TakeDamage(stompDamage);
    //         Debug.Log($"Stomped on: {hit.gameObject.name}");
    //     }
    // }
}
