using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AnimationController : MonoBehaviour
{
    PlayerMovement playerController;
    JumpAbility jumpAbility;
    DashAbility dashAbility;
    StompAbility stompAbility;
    WallJumpAbility wallJumpAbility;
    Animator animator;

    // Start is called before the first frame update
    void Start() {
        playerController = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
        jumpAbility = GetComponent<JumpAbility>();
        dashAbility = GetComponent<DashAbility>();
        stompAbility = GetComponent<StompAbility>();
        wallJumpAbility = GetComponent<WallJumpAbility>();

        jumpAbility.OnDoubleJump += PlayDoubleJump;
        dashAbility.OnStartDash += PlayDash;
        stompAbility.OnStartStomp += PlayStomp;
    }

    // Update is called once per frame
    void Update() {
        animator.SetFloat("horizontalMovement", Mathf.Abs(playerController.horizontalMovementValue));
        animator.SetFloat("verticalMovement", playerController.verticalMovementValue);
        animator.SetBool("isGrounded", playerController.isGrounded);
        animator.SetBool("isCrouching", playerController.isDucking);

        if(wallJumpAbility.isWallSliding) {
            animator.SetBool("onWall", true);
        } else {
            animator.SetBool("onWall", false);
        }
    }

    void PlayDoubleJump(object sender, EventArgs e) {
        animator.SetTrigger("doubleJump");
    }

    void PlayDash (object sender, EventArgs e) {
        animator.Play("slide");
    }

    void PlayStomp (object sender, EventArgs e) {
        animator.Play("stomp");
    }
}
