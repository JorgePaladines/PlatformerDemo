using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerAttack : MonoBehaviour {
    private bool attackEnabled;
    private PlayerMovement player;
    private JumpAbility jumpAbility;
    private DashAbility dashAbility;

    [SerializeField] GameObject attackBox;
    [SerializeField] GameObject crouchAttackBox;
    [SerializeField] GameObject sprintAttackBox;
    [SerializeField] float duration = 0.3f;
    
    private void Start(){
        player = GetComponent<PlayerMovement>();
        jumpAbility = GetComponent<JumpAbility>();
        dashAbility = GetComponent<DashAbility>();

        attackBox.SetActive(false);
        crouchAttackBox.SetActive(false);
        sprintAttackBox.SetActive(false);
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
        GameObject currentAttackBox;
        if(player.isDucking){
            currentAttackBox = crouchAttackBox;
        } else if(dashAbility.isSprinting){
            currentAttackBox = sprintAttackBox;
        } else {
            currentAttackBox = attackBox;
        }

        currentAttackBox.SetActive(true);
        RhythmManager.Instance.RegisterAction(true);
        yield return new WaitForSeconds(duration);
        currentAttackBox.SetActive(false);
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

    public void CancelAttack(object sender = null, EventArgs e = null){
        attackBox.SetActive(false);
    }
}
