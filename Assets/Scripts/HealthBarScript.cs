using System.Collections;
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
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 1.2f, 0);
    [SerializeField] private float healthBarDisplayTime = 3f;
    // [SerializeField] private Vector2 healthBarSize = new Vector2(80f, 20f);
    
    private Enemy enemy;
    private GameObject healthBarInstance;
    private Slider healthSlider;
    private Coroutine hideHealthBarCoroutine;
    
    private void Awake() {
        rhythmManager = FindAnyObjectByType<RhythmManager>();
        playerAttack = FindAnyObjectByType<PlayerAttack>();
        enemy = GetComponent<Enemy>();
        currentHealth = maxHealth;
        InstantiateHealthBar();
    }
    
    private void InstantiateHealthBar() {
        // Instantiate the slider prefab as a child of this GameObject
        healthBarInstance = Instantiate(healthBarPrefab, transform);
        // healthBarInstance = healthBarPrefab;
        
        // Position it above the enemy (the RectTransform will be positioned relative to the parent)
        RectTransform rectTransform = healthBarInstance.GetComponentInChildren<RectTransform>();
        if (rectTransform != null) {
            rectTransform.localPosition = healthBarOffset;
            // rectTransform.sizeDelta = healthBarSize;
        }
        
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
        Debug.Log("Enemy took " + damage + " damage!");
        if (isInvulnerable) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        healthSlider.value = currentHealth;

        ShowHealthBar();

        if (currentHealth <= 0) {
            enemy.Die();
        } else {
            enemy.TakeDamage();
            StartCoroutine(InvulnerabilityCoroutine());
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
    
    // Public method to heal the enemy if needed
    // public void Heal(int amount)
    // {
    //     currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
    //     if (healthSlider != null)
    //     {
    //         healthSlider.value = currentHealth;
    //     }
        
    //     ShowHealthBar();
    // }

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
                else if(!rhythmManager.usePowerAttack && !playerAttack.onBeat) {
                    damageAmount = hitBox.GetDamage() / hitBox.GetDivider();
                }
                else{
                    damageAmount = hitBox.GetDamage();
                }
                TakeDamage(damageAmount);
            }
        }
    }
}