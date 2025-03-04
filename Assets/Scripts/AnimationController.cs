using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AnimationController : MonoBehaviour {
    PlayerMovement playerController;
    PlayerAttack playerAttack;
    JumpAbility jumpAbility;
    DashAbility dashAbility;
    StompAbility stompAbility;
    WallJumpAbility wallJumpAbility;
    Animator animator;

    // Start is called before the first frame update
    void Start() {
        playerController = GetComponent<PlayerMovement>();
        playerAttack = GetComponent<PlayerAttack>();
        animator = GetComponentInChildren<Animator>();
        jumpAbility = GetComponent<JumpAbility>();
        dashAbility = GetComponent<DashAbility>();
        stompAbility = GetComponent<StompAbility>();
        wallJumpAbility = GetComponent<WallJumpAbility>();

        jumpAbility.OnDoubleJump += PlayDoubleJump;
        playerAttack.OnAttackStart += StartAttack;
        playerAttack.OnAttackEnd += EndAttack;
    }

    // Update is called once per frame
    void Update() {
        animator.SetFloat("horizontalMovement", Math.Abs(playerController.horizontalMovementValue));
        animator.SetFloat("verticalMovement", playerController.verticalMovementValue);
        animator.SetBool("isGrounded", playerController.isGrounded);
        animator.SetBool("isDucking", playerController.isDucking);
        animator.SetBool("isCrouching", playerController.isDucking);
        animator.SetBool("isDashing", dashAbility.isDashing);
        animator.SetBool("isStomping", stompAbility.isStomping);

        if(wallJumpAbility.isWallSliding) {
            animator.SetBool("onWall", true);
        } else {
            animator.SetBool("onWall", false);
        }
    }

    void PlayDoubleJump(object sender, EventArgs e) {
        animator.SetTrigger("doubleJump");
    }

    void StartAttack(object sender, EventArgs e) {
        animator.SetTrigger("attack");
    }

    void EndAttack(object sender, EventArgs e) {
    }
}
