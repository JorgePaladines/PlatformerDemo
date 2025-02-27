using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerMovement : MonoBehaviour {
    [SerializeField] float runSpeed = 5f;
    [SerializeField] float crouchSpeed = 2f;
    public float externalSpeed { get; private set; }
    [SerializeField] float customGravity  = 4f;

    public Vector2 moveInput { get; private set; }
    Rigidbody2D rigidBody;
    public CapsuleCollider2D bodyCollider { get; private set; }
    private Vector2 _originalColliderSize;
    PlayerAttack playerAttack;
    [SerializeField] float raycastDistance = 0.1f;
    [SerializeField] LayerMask layerMask;
    // [SerializeField] float slopeAngleLimit = 45f;
    // [SerializeField] float downForceAdjustment = 1.2f;

    #region validation properties
    public float horizontalMovementValue;
    public float verticalMovementValue;
    public int facingDirection; // Direction the playable character faces
    public int playerDirection; // Direction the player is trying to go to
    public bool tryingToTurn;
    public bool isGrounded;
    public bool isDucking;
    public bool keepDucking;
    public bool isStomping = false;
    private bool _inAirLastFrame;
    #endregion

    #region restrictive properties
    public bool canMove;
    public bool canDuck;
    public bool canStomp = true;
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
    [SerializeField] public float minTurnThreshold = 2f; // Minimum speed below which turning is allowed
    private float _currentHorizontalVelocity;  // Track current velocity separately
    #endregion

    public event EventHandler Landed;
    public event EventHandler OnStartStomp;
    public event EventHandler OnEndStomp;


    void Start()  {
        rigidBody = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        playerAttack = GetComponent<PlayerAttack>();

        rigidBody.gravityScale = customGravity;
        _originalColliderSize = bodyCollider.size;

        isGrounded = false;
        isDucking = false;
        keepDucking = false;
        externalSpeed = 0f;
        facingDirection = 1;
    }

    void Update() {
        Run();
        CheckDirection();
        CheckGrounded();
        Ducking();

        horizontalMovementValue = rigidBody.velocity.x;
        verticalMovementValue = rigidBody.velocity.y;
        _inAirLastFrame = !isGrounded;
    }

    public void OnMove(InputAction.CallbackContext context) {
        moveInput = context.ReadValue<Vector2>();
    }

    public float GetHorizontalVelocity(){
        return _currentHorizontalVelocity;
    }

    public void SetHorizontalVelocity(float velocity){
        _currentHorizontalVelocity = velocity;
    }

    public void DisableMove(){
        canMove = false;
    }

    public void EnableMove(){
        canMove = true;
    }

    public void DisableDuck(){
        canDuck = false;
    }

    public void EnableDuck(){
        canDuck = true;
    }

    public void SetExternalSpeed(float speed){
        externalSpeed = speed;
    }

    public void DisableGravity(){
        rigidBody.gravityScale = 0;
    }

    public void EnableGravity(){
        rigidBody.gravityScale = customGravity;
    }

    private void Run() {
        if (!canMove) return;

        if (isStomping) {
            Vector2 stompVelocity = new Vector2(moveInput.x * stompHorizontalSpeed, -stompSpeed);
            rigidBody.velocity = stompVelocity;
        }
        else {
            // Determine target velocity
            float targetVelocity;
            if (Math.Abs(externalSpeed) > 0) {
                targetVelocity = moveInput.x * externalSpeed; // Maintain external speed
            }
            else if (isDucking) {
                targetVelocity = moveInput.x * crouchSpeed;
            }
            else {
                targetVelocity = moveInput.x * runSpeed;
            }

            // Calculate deceleration rate
            tryingToTurn = moveInput.x != 0 && Mathf.Sign(moveInput.x) != Mathf.Sign(_currentHorizontalVelocity);
            float decelerationRate = tryingToTurn ? sharpDeceleration : normalDeceleration;

            if(Math.Abs(moveInput.x) != 0){
                _currentHorizontalVelocity = Mathf.MoveTowards(_currentHorizontalVelocity, targetVelocity, decelerationRate * Time.deltaTime);
            }
            else {
                _currentHorizontalVelocity = Mathf.MoveTowards(_currentHorizontalVelocity, 0f, decelerationRate * Time.deltaTime);
            }

            // Snap to zero when very close and no input
            if (Mathf.Abs(_currentHorizontalVelocity) < minVelocityThreshold && targetVelocity == 0) {
                _currentHorizontalVelocity = 0f;
            }

            // Apply the velocity
            rigidBody.velocity = new Vector2(_currentHorizontalVelocity, rigidBody.velocity.y);
        }
    }

    private void CheckDirection() {
        if (rigidBody.velocity.x < 0) {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            facingDirection = -1;
        }
        else if (rigidBody.velocity.x > 0) {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            facingDirection = 1;
        }

        playerDirection = Math.Sign(moveInput.x) >= 0 ? 1 : -1;
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
            Landed?.Invoke(this, EventArgs.Empty);
            isGrounded = true;

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
        if (!canDuck) return;

        if (!isGrounded) {
            isDucking = false;
            if (bodyCollider.size != _originalColliderSize) {
                bodyCollider.size = _originalColliderSize;
                bodyCollider.offset = new Vector2(0f, 0f);
            }
        }
        else if (moveInput.y < 0 && Math.Abs(_currentHorizontalVelocity) <= crouchSpeed && isGrounded && !isDucking) {
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

            if (!hitCeilingHigh.collider && !hitCeilingLow.collider && !keepDucking) {
                bodyCollider.size = _originalColliderSize;
                bodyCollider.offset = new Vector2(0f, 0f);
                isDucking = false;
            }
        }
    }

    public void ForceKeepDucking(bool value) {
        keepDucking = value;
    }

    public void ForceTurning(bool value) {
        tryingToTurn = value;
    }

    public void StartStomp() {
        OnStartStomp?.Invoke(this, EventArgs.Empty);

        isStomping = true;
        playerAttack.DisableAttack();
        // canDash = false;
        rigidBody.velocity = Vector2.zero; // Reset velocity before stomp
    }

    private void EndStomp() {
        OnEndStomp?.Invoke(this, EventArgs.Empty);

        isStomping = false;
        canMove = true;
        playerAttack.EnableAttack();
        // canDash = true;
        // keepSprintingState = true; // Enable sprinting after stomp

        // Instantly set velocity to dashSpeed if moving
        if (Math.Abs(moveInput.x) >= 1) {
            _currentHorizontalVelocity = moveInput.x * externalSpeed;
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
