using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructibleObject : MonoBehaviour {
    private SpriteBreaker spriteBreaker;
    private Collider2D mainCollider;
    private RhythmManager rhythmManager;
    private PlayerAttack playerAttack;
    private AudioSource audioSource;
    public AudioClip destructionSound;

    [Header("Health Settings")]
    public int maxHealth = 100;
    [SerializeField] private float currentHealth;
    public float invulnerabilityDuration = 0.3f;
    private bool isInvulnerable = false;
    private bool canTouch = true;

    void Start() {
        spriteBreaker = GetComponent<SpriteBreaker>();
        mainCollider = GetComponent<Collider2D>();
        rhythmManager = FindAnyObjectByType<RhythmManager>();
        playerAttack = FindAnyObjectByType<PlayerAttack>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = destructionSound;
        audioSource.playOnAwake = false;
    }

    public void TakeDamage(float damage) {
        if (isInvulnerable) return;
        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth <= 0) {
            canTouch = false;
            DestroyObject();
        } else {
            StartCoroutine(InvulnerabilityCoroutine());
        }
    }

    private IEnumerator InvulnerabilityCoroutine() {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }

    private void DestroyObject() {
        mainCollider.enabled = false;
        audioSource.Play();
        spriteBreaker.Break();
        // foreach (Transform child in transform) {
        //     Collider2D childCollider = child.GetComponent<Collider2D>();
        //     if (childCollider != null) {
        //         childCollider.enabled = false;
        //     }
        // }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        int collidedLayer = other.gameObject.layer;
        LayerMask targetLayers = LayerMask.GetMask("PlayerAttack");

        if (targetLayers == (targetLayers | (1 << collidedLayer)) && other.isTrigger) {
            HitBoxScript hitBox = other.gameObject.GetComponent<HitBoxScript>();
            if (hitBox != null) {
                float damageAmount;
                if(rhythmManager.usePowerAttack){
                    damageAmount = hitBox.GetDamage() * hitBox.GetMultiplier();
                }
                else {
                    if(!playerAttack.onBeat) {
                        damageAmount = hitBox.GetDamage() / hitBox.GetDivider();
                    }
                    else{
                        damageAmount = hitBox.GetDamage();
                    }
                }
                if(canTouch){
                    TakeDamage(damageAmount);
                }
            }
        }
    }
}
