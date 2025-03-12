using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class DashAbility : MonoBehaviour {
    PlayerMovement player;
    PlayerAttack playerAttack;
    Rigidbody2D rigidBody;
    private int enemyLayer;

    public bool canDash { get; private set; }
    public bool isDashing { get; private set; }
    public bool isSprinting { get; private set; }
    public bool keepSprintingState { get; private set; }
    private float _dashTimer;

    public float dashSpeed = 15f;
    private float dashTime = 0.15f;
    private float dashCooldownTime = 0.25f;
    
    public event EventHandler OnStartDash;
    public event EventHandler OnEndDash;

    void Awake() {
        player = GetComponent<PlayerMovement>();
        playerAttack = GetComponent<PlayerAttack>();
        rigidBody = GetComponent<Rigidbody2D>();
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }
    
    void Start() {
        canDash = true;
        isDashing = false;
        player.Landed += EnableDash;
    }

    void Update() {
        ConsumeDashTime();
        UpdateSprintingState();
    }

    public void EnableDash(object sender = null, EventArgs e = null) {
        canDash = true;
    }

    public void DisableDash(object sender = null, EventArgs e = null) {
        canDash = false;
    }

    public void OnDash (InputAction.CallbackContext context) {
        if(!canDash) return;
        if (context.started) {
            TriggerDash();
        }
    }

    public void TriggerDash() {
        if (player == null) return;

        bool airDash = canDash && !player.isGrounded && !player.isDucking && player.moveInput.y >= 0;
        bool groundDash = canDash && player.isGrounded && !player.isDucking && player.moveInput.y >= 0;
        bool crouchDash = canDash && player.isGrounded && player.isDucking;
        bool ableToDash = airDash || (groundDash && !crouchDash) || (!groundDash && crouchDash);

        if (ableToDash && _dashTimer <= 0) {
            StartCoroutine(Dash());
        }
    }

    private void ConsumeDashTime() {
        if (_dashTimer > 0) _dashTimer -= Time.deltaTime;
    }
    
    IEnumerator Dash() {
        // Avoid multiple air dashes
        if (!player.isGrounded) canDash = false;

        // Keep ducking during dash
        if(player.isGrounded && player.isDucking) player.ForceKeepDucking(true);

        OnStartDash?.Invoke(this, EventArgs.Empty);
        player.DisableGravity();
        isDashing = true;
        Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, true); // Enable enemy evasion
        _dashTimer = dashCooldownTime + dashTime; // Set cooldown immediately

        if(playerAttack != null) playerAttack.DisableAttack();
        player.DisableMove();
        player.DisableDuck();
        // jumpAbility?.DisableJump();
        
        // Enable sprinting after dash ends
        keepSprintingState = true;
        player.ForceTurning(false);
        player.SetExternalSpeed(dashSpeed);

        // Apply dash velocity
        int directionToDash = player.playerDirection == 0 ? player.facingDirection : player.playerDirection;
        player.LockFacingDirection(directionToDash);
        float newVelocity = dashSpeed * directionToDash;
        player.SetHorizontalVelocity(newVelocity);
        rigidBody.velocity = new Vector2(newVelocity, 0f);

        yield return new WaitForSeconds(dashTime);

        OnEndDash?.Invoke(this, EventArgs.Empty);
        player.EnableGravity();
        isDashing = false;
        player.UnlockFacingDirection();
        Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, false); // Disable enemy evasion
        player.ForceKeepDucking(false);

        // Check if ducking or not moving when dash ends and stop immediately
        if (player.isDucking || player.moveInput.x == 0 || player.facingDirection == player.moveInput.x * -1) {
            player.SetHorizontalVelocity(0f); // Stop immediately
            rigidBody.velocity = new Vector2(0f, rigidBody.velocity.y);
            keepSprintingState = false; // Prevent sprinting from resuming
            player.SetExternalSpeed(0f);
        }

        player.EnableMove();
        player.EnableDuck();
        if(playerAttack != null) playerAttack.EnableAttack();
        // jumpAbility?.EnableJump();

        yield return new WaitForSeconds(dashCooldownTime); // Wait for cooldown
    }

    private void UpdateSprintingState() {
        isSprinting = keepSprintingState && !player.isDucking && Math.Abs(player.moveInput.x) >= 1;

        if (keepSprintingState && !isDashing && player.tryingToTurn) {
            keepSprintingState = false;
            player.SetExternalSpeed(0f); // Reset sprinting when trying to turn
        }
        else if (keepSprintingState && player.moveInput.x == 0) {
            keepSprintingState = false;
            player.SetExternalSpeed(0f); // Reset sprinting when input stops
        }
    }

    public void SetSprintingState(bool state) {
        keepSprintingState = state;
    }
}
