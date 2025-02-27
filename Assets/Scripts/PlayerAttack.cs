using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour {
    private bool attackEnabled;
    public Transform attackPoint; // The position where the hitbox will be spawned
    private PlayerMovement player;

    [SerializeField] GameObject attackBox;
    [SerializeField] float duration = 0.3f;
    
    private void Start(){
        player = FindObjectOfType<PlayerMovement>();
        attackBox.SetActive(false);
        attackEnabled = true;
    }

    public bool canAttack(){
        return attackEnabled;
    }

    public void OnAttack(InputAction.CallbackContext context) { // Call this when the attack button is pressed
        if (context.started && attackEnabled) {
            StartCoroutine(nameof(Attack));
        }
    }

    IEnumerator Attack(){
        attackBox.SetActive(true);
        RhythmManager.Instance.RegisterAction(true);
        yield return new WaitForSeconds(duration);
        attackBox.SetActive(false);
    }

    public void CancelAttack(){
        attackBox.SetActive(false);
    }

    public void EnableAttack(){
        attackEnabled = true;
    }

    public void DisableAttack(){
        attackEnabled = false;
    }
}
