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

    public Vector2 moveInput;
    Rigidbody2D rigidBody;
    public CapsuleCollider2D bodyCollider { get; private set; }
    private Vector2 standingColliderSize;
    private Vector3 standingColliderCenter;
    private Vector2 crouchColliderSize;
    private Vector3 crouchColliderCenter;
    // private Vector2 crouchColliderOffset;
    private float raycastDistance = 0.1f;
    [SerializeField] LayerMask levelGeometryLayer;
    [SerializeField] LayerMask enemyLayer;

    #region validation properties
    public float horizontalMovementValue;
    public float verticalMovementValue;
    public int facingDirection; // Direction the playable character faces
    public int playerDirection; // Direction the player is trying to go to
    public bool tryingToTurn;
    public bool isFacingLocked;
    public bool isGrounded;
    public bool isDucking;
    public bool keepDucking;
    public bool _inAirLastFrame;
    public bool left;
    public bool right;
    public bool above;
    #endregion

    #region restrictive properties
    public bool canMove;
    public bool canDuck;
    #endregion

    #region deceleration variables
    [SerializeField] float normalDeceleration = 15f;     // Rate of normal slowing
    [SerializeField] float sharpDeceleration = 40f;      // Rate when changing directions
    [SerializeField] float minVelocityThreshold = 0.1f;  // When to snap to zero
    [SerializeField] public float minTurnThreshold = 2f; // Minimum speed below which turning is allowed
    private float _currentHorizontalVelocity;  // Track current velocity separately
    #endregion

    public event EventHandler Landed;
    public event EventHandler EnterCrouch;
    public event EventHandler ExitCrouch;

    void Start()  {
        rigidBody = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();

        rigidBody.gravityScale = customGravity;

        standingColliderSize = new Vector2(bodyCollider.size.x, bodyCollider.size.y);
        standingColliderCenter = new Vector3(bodyCollider.bounds.center.x, bodyCollider.bounds.center.y, bodyCollider.bounds.center.z);
        crouchColliderSize = new Vector2(standingColliderSize.x, standingColliderSize.y / 2);
        crouchColliderCenter = new Vector3(standingColliderCenter.x, standingColliderCenter.y - bodyCollider.size.y / 4);
        // crouchColliderOffset = new Vector2(0f, -standingColliderSize.y / 2);

        canMove = true;
        isGrounded = false;
        isDucking = false;
        keepDucking = false;
        externalSpeed = 0f;
        facingDirection = 1;
    }

    void Update() {
        Run();
        CheckGrounded();
        Ducking();
        CheckDirection();
        CheckAbove();
        CheckSides();

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

    public void ForceKeepDucking(bool value) {
        keepDucking = value;
    }

    public void ForceTurning(bool value) {
        tryingToTurn = value;
    }

    public void LockFacingDirection(int direction) {
        isFacingLocked = true;
        facingDirection = direction;
        transform.rotation = Quaternion.Euler(0f, direction < 0 ? 180f : 0f, 0f);
    }

    public void UnlockFacingDirection() {
        isFacingLocked = false;
    }

    private void Run() {
        if (!canMove) return;

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

    private void CheckDirection() {
        if (isFacingLocked) return;
        
        if (rigidBody.velocity.x < 0) {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            facingDirection = -1;
        }
        else if (rigidBody.velocity.x > 0) {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            facingDirection = 1;
        }

        playerDirection = Math.Sign(moveInput.x);
    }

    private void CheckGrounded(){
        RaycastHit2D groundRayCast = Physics2D.CapsuleCast(
            bodyCollider.bounds.center, 
            bodyCollider.size, 
            CapsuleDirection2D.Vertical, 
            0f, 
            Vector2.down, 
            raycastDistance/2, 
            levelGeometryLayer | enemyLayer
        );

        Vector2 colliderCenter = bodyCollider.bounds.center;
        Vector2 colliderSize = bodyCollider.size;
        Debug.DrawRay(new Vector2(colliderCenter.x, colliderCenter.y - colliderSize.y / 2), Vector2.down * raycastDistance/2, Color.green);

        if(groundRayCast.collider){
            isGrounded = true;

            if(isGrounded && _inAirLastFrame){
                Landed?.Invoke(this, EventArgs.Empty);
            }
        }
        else{
            isGrounded = false;
            isDucking = false;
        }
    }

    private void CheckAbove() {
        RaycastHit2D hitCeilingStanding;
        if (bodyCollider.size == standingColliderSize && !isDucking) {
            hitCeilingStanding = Physics2D.CapsuleCast(
                bodyCollider.bounds.center,
                bodyCollider.size,
                CapsuleDirection2D.Vertical,
                0f,
                Vector2.up,
                raycastDistance,
                levelGeometryLayer
            );
        }
        else{
            hitCeilingStanding = Physics2D.CapsuleCast(
                new Vector3(bodyCollider.bounds.center.x, bodyCollider.bounds.center.y + bodyCollider.size.y / 2, bodyCollider.bounds.center.z),
                standingColliderSize,
                CapsuleDirection2D.Vertical,
                0f,
                Vector2.up,
                raycastDistance,
                levelGeometryLayer
            );
        }

        above = hitCeilingStanding.collider ? true : false;
    }
    
    private void Ducking() {
        if (!canDuck) return;

        if (!isGrounded) {
            if(!_inAirLastFrame){
                ExitCrouch?.Invoke(this, EventArgs.Empty);
            }
            isDucking = false;
            if (bodyCollider.size != standingColliderSize) {
                bodyCollider.size = standingColliderSize;
                bodyCollider.offset = new Vector2(0f, 0f);
            }
        }
        else if (moveInput.y < 0 && Math.Abs(_currentHorizontalVelocity) <= crouchSpeed && isGrounded && !isDucking) {
            EnterCrouch?.Invoke(this, EventArgs.Empty);
            bodyCollider.size = new Vector2(bodyCollider.size.x, bodyCollider.size.y / 2);
            bodyCollider.offset = new Vector2(0f, -bodyCollider.size.y / 2);
            isDucking = true;
        }
        else if (moveInput.y >= 0 && isGrounded && isDucking) {
            if(!above && !keepDucking){
                ExitCrouch?.Invoke(this, EventArgs.Empty);
                bodyCollider.size = standingColliderSize;
                bodyCollider.offset = new Vector2(0f, 0f);
                isDucking = false;
            }
        }
    }

    private void CheckSides(){
        Vector2 colliderCenter = bodyCollider.bounds.center;
        Vector2 colliderSize = bodyCollider.size;

        RaycastHit2D rightSide = Physics2D.Raycast(
            new Vector2(colliderCenter.x + colliderSize.x / 2, colliderCenter.y),
            Vector2.right,
            raycastDistance,
            levelGeometryLayer
        );
        RaycastHit2D leftSide = Physics2D.Raycast(
            new Vector2(colliderCenter.x - colliderSize.x / 2, colliderCenter.y),
            Vector2.left,
            raycastDistance,
            levelGeometryLayer
        );

        Debug.DrawRay(new Vector2(colliderCenter.x + colliderSize.x / 2, colliderCenter.y), Vector2.right * raycastDistance, Color.green);
        Debug.DrawRay(new Vector2(colliderCenter.x - colliderSize.x / 2, colliderCenter.y), Vector2.left * raycastDistance, Color.red);

        left = leftSide.collider != null;
        right = rightSide.collider != null;
    }
}
