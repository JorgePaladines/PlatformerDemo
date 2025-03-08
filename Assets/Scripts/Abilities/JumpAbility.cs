using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class JumpAbility : MonoBehaviour {
    [Header("Player Components")]
    PlayerMovement player;
    Rigidbody2D rigidBody;

    [Header("Jump Settings")]
    [SerializeField] float jumpSpeed = 18f;
    [SerializeField] float doubleJumpMultiplier = 0.75f;
    public bool canJump = true;
    public bool canDoubleJump = true;

    public event EventHandler Jumped;
    public event EventHandler OnDoubleJump;

    void Awake() {
        player = GetComponent<PlayerMovement>();
        rigidBody = GetComponent<Rigidbody2D>();
    }

    void Start() {
        player.Landed += EnableDoubleJump;
    }

    public void OnJump(InputAction.CallbackContext context) {
        if(!canJump || player == null) return;
        
        if (context.started) {
            if(player.isGrounded && !player.above) {
                Jumped?.Invoke(this, EventArgs.Empty);
                rigidBody.velocity = new Vector2(rigidBody.velocity.x, 0f);
                rigidBody.velocity += new Vector2(0f, jumpSpeed);
                if(rigidBody.gravityScale == 0){
                    player.EnableGravity();
                }
                player.ForceKeepDucking(false);
            }
            else if(canDoubleJump && !player.isGrounded && !player.isDucking){
                OnDoubleJump?.Invoke(this, EventArgs.Empty);
                canDoubleJump = false;
                rigidBody.velocity = new Vector2(rigidBody.velocity.x, 0f); // Reset Y velocity to avoid stacking force
                rigidBody.velocity += new Vector2(0f, jumpSpeed * doubleJumpMultiplier);
            }
        }
        else if (context.canceled) {
            if (rigidBody.velocity.y > 0) {
                rigidBody.velocity *= new Vector2(1f, 0.5f);
            }
        }
    }

    public void EnableJump(object sender = null, EventArgs e = null) {
        canJump = true;
    }

    public void EnableDoubleJump(object sender = null, EventArgs e = null) {
        canDoubleJump = true;
    }

    public void DisableJump(object sender = null, EventArgs e = null) {
        canJump = false;
    }

    public void DisableDoubleJump(object sender = null, EventArgs e = null) {
        canDoubleJump = false;
    }
}
