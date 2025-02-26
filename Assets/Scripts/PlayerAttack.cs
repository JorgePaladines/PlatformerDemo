using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour {
    public Transform attackPoint; // The position where the hitbox will be spawned
    private GameObject currentHitbox; // Store reference to active hitbox
    private PlayerMovement playerMovement;

    [SerializeField] GameObject attackBox;
    [SerializeField] float duration = 0.3f;
    
    private void Start(){
        playerMovement = FindObjectOfType<PlayerMovement>();
        attackBox.SetActive(false);
    }

    public void OnAttack(InputAction.CallbackContext context) { // Call this when the attack button is pressed
        if (context.started && playerMovement.canAttack) {
            StartCoroutine(nameof(Attack));

            // if (currentHitbox == null) { // Prevent multiple hitboxes at once
            //     currentHitbox = Instantiate(attackBox, attackPoint.position, Quaternion.identity);
            //     RhythmManager.Instance.RegisterAction(true);
            //     currentHitbox.transform.SetParent(transform); // Parent to the player
            //     Destroy(currentHitbox, duration);
            // }
        }
    }

    IEnumerator Attack(){
        attackBox.SetActive(true);
        RhythmManager.Instance.RegisterAction(true);
        yield return new WaitForSeconds(duration);
        attackBox.SetActive(false);
    }

    public void CancelAttack(){
        // Destroy(currentHitbox);
        attackBox.SetActive(false);
    }
}
