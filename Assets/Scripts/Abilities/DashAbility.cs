using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class DashAbility : MonoBehaviour {
    PlayerMovement player;
    Rigidbody2D rigidBody;
    PlayerAttack playerAttack;

    public bool canDash { get; private set; }
    public bool isDashing { get; private set; }
    public bool isSprinting { get; private set; }
    public bool keepSprintingState;
    private float _dashTimer;

    [SerializeField] float dashSpeed = 15f;
    [SerializeField] float dashTime = 0.1f;
    [SerializeField] float dashCooldownTime = 0.25f;
    
    public event EventHandler OnStartDash;
    public event EventHandler OnEndDash;

    void Awake() {
        player = GetComponent<PlayerMovement>();
        rigidBody = GetComponent<Rigidbody2D>();
        playerAttack = GetComponent<PlayerAttack>();
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
        if(!canDash && !player.canStomp) return;

        if (context.started) {
            if(!isDashing && player.canStomp && !player.isGrounded && player.moveInput.y < 0 && !player.isStomping){
                player.StartStomp();
            }
            else{
                if(canDash && !isDashing && _dashTimer <= 0 && !player.isStomping){
                    StartCoroutine(nameof(Dash));
                }
            }
        }
    }

    private void ConsumeDashTime() {
        if (_dashTimer > 0) _dashTimer -= Time.deltaTime;
    }
    
    IEnumerator Dash() {
        // Avoid multiple air dashes
        if (!player.isGrounded) canDash = false;

        // Keep ducking during dash
        if(player.isDucking) player.ForceKeepDucking(true);

        OnStartDash?.Invoke(this, EventArgs.Empty);
        player.DisableGravity();
        isDashing = true;
        _dashTimer = dashCooldownTime + dashTime; // Set cooldown immediately

        player.DisableMove();
        player.DisableDuck();
        playerAttack.DisableAttack();
        player.canStomp = false;
        
        // Enable sprinting after dash ends
        keepSprintingState = true;
        player.ForceTurning(false);
        player.SetExternalSpeed(dashSpeed);

        // Apply dash velocity
        int directionToDash = player.moveInput.x == 0 ? player.facingDirection : player.playerDirection;
        float newVelocity = dashSpeed * directionToDash;
        player.SetHorizontalVelocity(newVelocity);
        rigidBody.velocity = new Vector2(newVelocity, 0f);

        yield return new WaitForSeconds(dashTime);

        player.EnableGravity();
        OnEndDash?.Invoke(this, EventArgs.Empty);
        isDashing = false;
        player.ForceKeepDucking(false);

        // Check if ducking when dash ends and stop immediately
        if (player.isDucking) {
            player.SetHorizontalVelocity(0f); // Stop immediately
            rigidBody.velocity = new Vector2(0f, rigidBody.velocity.y);
            keepSprintingState = false; // Prevent sprinting from resuming
            player.SetExternalSpeed(0f);
        }

        player.EnableMove();
        player.EnableDuck();
        playerAttack.EnableAttack();
        player.canStomp = true;

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
