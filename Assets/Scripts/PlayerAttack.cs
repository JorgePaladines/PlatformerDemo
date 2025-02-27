using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerAttack : MonoBehaviour {
    private bool attackEnabled;
    public Transform attackPoint; // The position where the hitbox will be spawned
    private PlayerMovement player;
    private JumpAbility jumpAbility;

    [SerializeField] GameObject attackBox;
    [SerializeField] float duration = 0.3f;
    
    private void Start(){
        player = GetComponent<PlayerMovement>();
        jumpAbility = GetComponent<JumpAbility>();

        attackBox.SetActive(false);
        attackEnabled = true;

        player.Landed += CancelAttack;
        jumpAbility.Jumped += CancelAttack;
    }

    public void OnAttack(InputAction.CallbackContext context) {
        if (player == null || attackBox == null) return;
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

    public bool canAttack(){
        return attackEnabled;
    }

    public bool EnableAttack(){
        return attackEnabled = true;
    }

    public bool DisableAttack(){
        return attackEnabled = false;
    }

    // Cancel attack if the player hit the ground or just jumped
    public void CancelAttack(object sender = null, EventArgs e = null){
        attackBox.SetActive(false);
    }
}
