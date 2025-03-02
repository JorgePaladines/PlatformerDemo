using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class WallJumpAbility : MonoBehaviour {
    private PlayerMovement player;
    private Rigidbody2D rigidBody;
    private JumpAbility jumpAbility;
    private DashAbility dashAbility;
    private StompAbility stompAbility;

    [SerializeField] private float wallJumpHorizontalForce = 18f; // Horizontal force away from wall
    [SerializeField] private float wallJumpVerticalForce = 12f;   // Vertical force for wall jump
    [SerializeField] private float wallSlideSpeed = 2f;          // Downward speed while sliding

    [SerializeField] public bool canWallJump = true;
    [SerializeField] public bool isWallSliding;
    [SerializeField] public bool isWallJumping;
    [SerializeField] public int wallDirection; // 1 for right wall, -1 for left wall
    private float wallJumpLockoutTimer;

    public event EventHandler EnterWallSliding;
    public event EventHandler ExitWallSliding;

    void Awake() {
        player = GetComponent<PlayerMovement>();
        rigidBody = GetComponent<Rigidbody2D>();
        jumpAbility = GetComponent<JumpAbility>();
        dashAbility = GetComponent<DashAbility>();
        stompAbility = GetComponent<StompAbility>();
    }

    void Start() {
        player.Landed += ResetWallJump;
        stompAbility.OnStartStomp += DisableWallJump;
    }

    void Update() {
        CheckWallSliding();
    }

    private void EnableWallJump() {
        canWallJump = true;
    }

    public void DisableWallJump(object sender = null, EventArgs e = null) {
        canWallJump = false;
    }

    private void ResetWallJump(object sender = null, EventArgs e = null) {
        isWallSliding = false;
        isWallJumping = false;
        wallJumpLockoutTimer = 0f;
        canWallJump = true;
    }

    public void OnJump(InputAction.CallbackContext context) {
        if (!canWallJump || player == null || !context.started) return;

        bool pressingRightWall = player.right && player.moveInput.x > 0;
        bool pressingLeftWall = player.left && player.moveInput.x < 0;

        if ((pressingRightWall || pressingLeftWall) && !player.isGrounded) {
            WallJump();
        }
    }

    private void CheckWallSliding() {
        if (player.isGrounded || jumpAbility == null || !jumpAbility.canJump) {
            isWallSliding = false;
            return;
        }

        wallDirection = 0;
        if (player.right && player.moveInput.x > 0) {
            wallDirection = 1; // Right wall
        }
        else if (player.left && player.moveInput.x < 0) {
            wallDirection = -1; // Left wall
        }

        isWallSliding = wallDirection != 0 && rigidBody.velocity.y <= 0;
        if (isWallSliding) {
            EnterWallSliding?.Invoke(this, EventArgs.Empty);
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, Mathf.Max(-wallSlideSpeed, rigidBody.velocity.y));
            player.SetExternalSpeed(0f); // Prevent horizontal movement overriding slide
            player.SetHorizontalVelocity(0f); // Prevent horizontal movement overriding slide
            player.facingDirection = -wallDirection; // Face away from wall
            if(dashAbility != null) {
                dashAbility.DisableDash(); // Disable dash while wall sliding
            }
        }
        else {
            ExitWallSliding?.Invoke(this, EventArgs.Empty);
        }
    }

    private void WallJump() {
        isWallJumping = true;
        // wallJumpLockoutTimer = postWallJumpLockoutTime;
        player.ForceTurning(false); // Reset turning state

        // Apply wall jump force (opposite direction of wall)
        Vector2 jumpForce = new Vector2(-wallDirection * wallJumpHorizontalForce, wallJumpVerticalForce);
        rigidBody.velocity = jumpForce;

        if (jumpAbility != null) {
            jumpAbility.DisableDoubleJump();
        }
        if(dashAbility != null) {
            dashAbility.DisableDash();
        }

        EnableWallJump();
        StartCoroutine(UpdateWallJumpLockout());
    }

    IEnumerator UpdateWallJumpLockout() {
        // Restrict movement toward the wall
        float inputDirection = player.moveInput.x;
        if ((wallDirection == 1 && inputDirection > 0) || (wallDirection == -1 && inputDirection < 0)) {
            player.SetHorizontalVelocity(rigidBody.velocity.x); // Maintain current velocity, ignore input toward wall
        }
        
        yield return new WaitForSeconds(wallJumpLockoutTimer);

        isWallJumping = false;
        if (jumpAbility != null) {
            jumpAbility.EnableDoubleJump();
        }
        if(dashAbility != null) {
            dashAbility.EnableDash();
            dashAbility.SetSprintingState(true); // Enable sprinting after wall jump
            player.SetExternalSpeed(dashAbility.dashSpeed); // Maintain dash speed
        }
    }
}