using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class StompAbility : MonoBehaviour {
    public bool canStomp = true;
    public bool isStomping = false;
    [SerializeField] float stompSpeed = 23f; // Speed of downward stomp
    [SerializeField] float stompHorizontalSpeed = 2f; // Limited horizontal movement during stomp
    
    [SerializeField] CapsuleCollider2D[] damageHitboxes;
    [SerializeField] LayerMask enemyLayerMask; // Layer for enemies to detect

    [SerializeField] ParticleSystem fallingEffectPrefab;
    ParticleSystem fallingEffect;
    [SerializeField] ParticleSystem powerStompEffectPrefab;
    ParticleSystem powerStompEffect;

    PlayerMovement player;
    Rigidbody2D rigidBody;
    JumpAbility jumpAbility;
    DashAbility dashAbility;
    PlayerAttack playerAttack;

    private AudioSource audioSource;
    [SerializeField] AudioClip stompSound;
    [SerializeField] AudioClip powerStompSound;

    public event EventHandler OnStartStomp;
    public event EventHandler OnEndStomp;

    void Awake() {
        player = GetComponent<PlayerMovement>();
        rigidBody = GetComponent<Rigidbody2D>();
        jumpAbility = GetComponent<JumpAbility>();
        dashAbility = GetComponent<DashAbility>();
        playerAttack = GetComponent<PlayerAttack>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = stompSound;
    }

    void Start() {
        if (fallingEffectPrefab != null) {
            fallingEffect = Instantiate(fallingEffectPrefab, player.transform);
            fallingEffect.Stop();
        }
        if(powerStompEffectPrefab != null) {
            powerStompEffect = Instantiate(powerStompEffectPrefab, player.transform);
            powerStompEffect.Stop();
        }
        player.Landed += EndStomp;
        dashAbility.OnStartDash += DisableStomp;
        dashAbility.OnEndDash += EnableStomp;

        DisableHitBoxes();
    }

    public void EnableStomp(object sender = null, EventArgs e = null) {
        canStomp = true;
    }

    public void DisableStomp(object sender = null, EventArgs e = null) {
        canStomp = false;
    }

    public void EnableHitBoxes() {
        foreach (CapsuleCollider2D damageBox in damageHitboxes) {
            damageBox.gameObject.SetActive(true);
        }
    }

    public void DisableHitBoxes() {
        foreach (CapsuleCollider2D damageBox in damageHitboxes) {
            damageBox.gameObject.SetActive(false);
        }
    }

    public void OnStomp (InputAction.CallbackContext context) {
        if(!canStomp || player == null) return;
        if (context.started) {
            bool isDashing = dashAbility?.isDashing ?? false;
            if(!isDashing && canStomp && !player.isGrounded && !player.isDucking && player.moveInput.y < 0 && !isStomping){
                StartStomp();
            }
        }
    }

    public void StartStomp() {
        OnStartStomp?.Invoke(this, EventArgs.Empty);
        isStomping = true;
        player.SetExternalSpeed(stompHorizontalSpeed);
        if(jumpAbility != null) jumpAbility.DisableJump();
        if(dashAbility != null) dashAbility.DisableDash();
        if(playerAttack != null) playerAttack.DisableAttack();
        EnableHitBoxes();
        fallingEffect?.Play();
        rigidBody.velocity = Vector2.zero; // Reset velocity before stomp
        Vector2 stompVelocity = new Vector2(rigidBody.velocity.x, -stompSpeed);
        rigidBody.velocity = stompVelocity;
    }

    private void EndStomp(object sender = null, EventArgs e = null) {
        if(!isStomping) return;

        OnEndStomp?.Invoke(this, EventArgs.Empty);
        playerAttack.onBeat = RhythmManager.Instance.RegisterAction(true);
        isStomping = false;
        player.EnableMove();
        player.SetExternalSpeed(0f);
        DisableHitBoxes();

        if(jumpAbility != null) jumpAbility.EnableJump();
        if(playerAttack != null) playerAttack.EnableAttack();
        if(dashAbility != null){
            dashAbility.EnableDash();

            // Instantly trigger a dash if moving
            if (Math.Abs(player.moveInput.x) >= 1) {
                dashAbility.TriggerDash();
            } else {
                // Reset if no input
                player.SetExternalSpeed(0f);
                player.SetHorizontalVelocity(0f);
            }
        }

        fallingEffect?.Stop();
        bool powerAttack = RhythmManager.Instance.usePowerAttack;
        if(powerAttack){
            audioSource.clip = powerStompSound;
            powerStompEffect?.Play();
        }
        else {
            audioSource.clip = stompSound;
        }
        
        audioSource.Play();

        // Apply horizontal velocity immediately
        rigidBody.velocity = new Vector2(player.GetHorizontalVelocity(), 0f);
    }
}
