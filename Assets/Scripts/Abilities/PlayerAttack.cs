using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using static UnityEngine.ParticleSystem;

public class PlayerAttack : MonoBehaviour {
    RhythmManager rhythmManager;
    public Material shaderMaterial;

    public bool attackEnabled = true;
    public bool onBeat;
    private PlayerMovement player;
    private SpriteRenderer spriteRenderer;
    private JumpAbility jumpAbility;
    private DashAbility dashAbility;
    private StompAbility stompAbility;
    private WallJumpAbility wallJumpAbility;

    private Collider2D currentHitBox;
    [SerializeField] Collider2D attackHitBox;
    [SerializeField] Collider2D airHitBox;
    [SerializeField] Collider2D crouchHitBox;
    [SerializeField] Collider2D sprintHitBox;
    [SerializeField] float duration = 0.1f;

    [Header("Power Attack Settings")]
    [SerializeField] ParticleSystem powerUpEffectPrefab;
    ParticleSystem powerUpEffect;
    EmissionModule emission;
    [SerializeField] AudioClip attackSound;
    [SerializeField] AudioClip powerAttackSound;
    [SerializeField] AudioClip extraPowerSound;
    private AudioSource audioSource;

    public event EventHandler OnAttackStart;
    public event EventHandler OnPowerAttack;
    public event EventHandler OnAttackEnd;

    private void Start(){
        player = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        jumpAbility = GetComponent<JumpAbility>();
        dashAbility = GetComponent<DashAbility>();
        stompAbility = GetComponent<StompAbility>();
        wallJumpAbility = GetComponent<WallJumpAbility>();
        rhythmManager = FindAnyObjectByType<RhythmManager>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = attackSound;

        attackHitBox?.gameObject.SetActive(false);
        airHitBox?.gameObject.SetActive(false);
        crouchHitBox?.gameObject.SetActive(false);
        sprintHitBox?.gameObject.SetActive(false);
        currentHitBox = attackHitBox;

        if (powerUpEffectPrefab != null) {
            powerUpEffect = Instantiate(powerUpEffectPrefab, player.transform);
            powerUpEffect.Pause();
            emission = powerUpEffect.emission;
            emission.rateOverTime = 0;
        }

        shaderMaterial.SetFloat("_Amount", 0f);

        player.Landed += CancelAttack;
        player.EnterCrouch += CancelAttack;
        player.ExitCrouch += CancelAttack;
        jumpAbility.Jumped += CancelAttack;
        dashAbility.OnStartDash += CancelAttack;
        dashAbility.OnStartDash += DisableAttack;
        dashAbility.OnEndDash += EnableAttack;
        stompAbility.OnStartStomp += DisableAttack;
        stompAbility.OnEndStomp += EnableAttack;
        wallJumpAbility.EnterWallSliding += DisableAttack;
        wallJumpAbility.ExitWallSliding += EnableAttack;
        rhythmManager.OnPowerAttack += VisualFeedback;
    }

    private void Update() {
        if(powerUpEffect != null && powerUpEffectPrefab != null && powerUpEffect.transform.localPosition.z != player.facingDirection){
            powerUpEffect.transform.localPosition = new Vector3(
                powerUpEffect.transform.localPosition.x,
                powerUpEffect.transform.localPosition.y,
                player.facingDirection
            );
        }

        HandlePowerEffectEmission();
    }

    public void OnAttack(InputAction.CallbackContext context) {
        if (player == null || !attackEnabled) return;
        if (context.started) {
            StartCoroutine(nameof(Attack));
        }
    }

    IEnumerator Attack(){
        DisableAttack();
        onBeat = RhythmManager.Instance.RegisterAction(true);

        if(rhythmManager.usePowerAttack){
            OnPowerAttack?.Invoke(this, EventArgs.Empty);
            audioSource.clip = powerAttackSound;
        }
        else{
            OnAttackStart?.Invoke(this, EventArgs.Empty);
            audioSource.clip = attackSound;
        }
        audioSource.Play();

        if(!player.isGrounded){
            currentHitBox = airHitBox;
        }
        else{
            if(player.isDucking){
                currentHitBox = crouchHitBox;
            }
            else if(dashAbility.isSprinting){
                currentHitBox = sprintHitBox;
            }
            else {
                currentHitBox = attackHitBox;
            }
        }
        currentHitBox.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        OnAttackEnd?.Invoke(this, EventArgs.Empty);
        onBeat = false;
        currentHitBox.gameObject.SetActive(false);
        EnableAttack();
    }

    private void HandlePowerEffectEmission() {
        if(rhythmManager.usePowerAttack){
            emission.rateOverTime = 100;
            shaderMaterial.SetFloat("_Amount", 0.000002f);
        }
        else if (rhythmManager.streak > 0 && rhythmManager.PlayerStillHasChance()) {
            if(!powerUpEffect.isPlaying){
                powerUpEffect.Play();
            }
            switch (rhythmManager.streak) {
                case 0:
                    emission.rateOverTime = 0;
                    shaderMaterial.SetFloat("_Amount", 0f);
                    break;
                case 1:
                    emission.rateOverTime = 10;
                    shaderMaterial.SetFloat("_Amount", 0.000001f);
                    break;
                case 2:
                    emission.rateOverTime = 50;
                    shaderMaterial.SetFloat("_Amount", 0.00001f);
                    break;
                default:
                    emission.rateOverTime = 100;
                    shaderMaterial.SetFloat("_Amount", 0.00005f);
                    break;
            }
        }
        else {
            emission.rateOverTime = 0;
            shaderMaterial.SetFloat("_Amount", 0f);
            powerUpEffect.Stop();
        }
    }

    private void VisualFeedback(object sender, EventArgs e) {
        StartCoroutine(ResetPowerAttack());
    }

    IEnumerator ResetPowerAttack(){
        if(spriteRenderer != null){
            yield return new WaitForSeconds(duration);
            rhythmManager.ResetPowerAttack();
        }
    }


    public bool canAttack(){
        return attackEnabled;
    }

    public void EnableAttack(object sender = null, EventArgs e = null){
        attackEnabled = true;
    }

    public void DisableAttack(object sender = null, EventArgs e = null){
        attackEnabled = false;
    }

    public void CancelAttack(object sender = null, EventArgs e = null){
        currentHitBox?.gameObject.SetActive(false);
    }
}
