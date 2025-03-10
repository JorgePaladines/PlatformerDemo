using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarScript : MonoBehaviour {
    RhythmManager rhythmManager;
    PlayerAttack playerAttack;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float currentHealth;
    private bool isInvulnerable = false;
    
    [Header("Health Bar UI")]
    [SerializeField] private GameObject healthBarPrefab; // This should be a prefab with a Slider component
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float healthBarDisplayTime = 3f;
    [SerializeField] private GameObject damagePointsPrefab;
    
    private Enemy enemy;
    private GameObject healthBarInstance;
    private Slider healthSlider;
    private Coroutine hideHealthBarCoroutine;
    private GameObject enemyDamagePoints;
    
    private void Awake() {
        rhythmManager = FindAnyObjectByType<RhythmManager>();
        playerAttack = FindAnyObjectByType<PlayerAttack>();
        enemy = GetComponent<Enemy>();
        currentHealth = maxHealth;
        InstantiateHealthBar();
    }
    
    private void InstantiateHealthBar() {
        healthBarInstance = Instantiate(healthBarPrefab, transform);
        
        // Position it above the enemy (the RectTransform will be positioned relative to the parent)
        RectTransform rectTransform = healthBarInstance.GetComponentInChildren<RectTransform>();
        rectTransform.localPosition = healthBarOffset;
        
        // Get reference to the Slider component
        healthSlider = healthBarInstance.GetComponentInChildren<Slider>();
        if (healthSlider != null) {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        else {
            Debug.LogError("Health bar prefab does not contain a Slider component!");
        }
        
        // Initially hide the health bar
        healthBarInstance.SetActive(false);
    }
    
    public void TakeDamage(float damage) {
        if (isInvulnerable) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        healthSlider.value = currentHealth;

        ShowHealthBar();
        ShowDamagePoints(damage);

        if (currentHealth <= 0) {
            enemy.Die();
        } else {
            enemy.TakeDamage();
            StartCoroutine(InvulnerabilityCoroutine());
        }
    }

    private void ShowDamagePoints(float damage) {
        if(currentHealth > 0){
            if(enemyDamagePoints != null){
                Destroy(enemyDamagePoints);
            }
            enemyDamagePoints = Instantiate(damagePointsPrefab, transform);
            enemyDamagePoints.GetComponentInChildren<TextMeshPro>().text = damage.ToString();
            RectTransform rectTransform = enemyDamagePoints.GetComponent<RectTransform>();
            rectTransform.localPosition = healthBarOffset;
        }
    }

    private void ShowHealthBar() {
        healthBarInstance.SetActive(true);
        if(hideHealthBarCoroutine != null) StopCoroutine(hideHealthBarCoroutine);
        hideHealthBarCoroutine = StartCoroutine(HideHealthBarAfterDelay());
    }
    
    private IEnumerator HideHealthBarAfterDelay() {
        yield return new WaitForSeconds(healthBarDisplayTime);
        healthBarInstance.SetActive(false);
        hideHealthBarCoroutine = null;
    }

    private IEnumerator InvulnerabilityCoroutine() {
        isInvulnerable = true;
        yield return new WaitForSeconds(enemy.invulnerabilityDuration);
        isInvulnerable = false;
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
                TakeDamage(damageAmount);
            }
        }
    }
}