using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AnimationController : MonoBehaviour
{
    PlayerMovement playerController;
    JumpAbility jumpAbility;
    Animator animator;

    // Start is called before the first frame update
    void Start() {
        playerController = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
        jumpAbility = GetComponent<JumpAbility>();

        jumpAbility.OnDoubleJump += PlayDoubleJump;
        playerController.OnStartDash += PlayDash;
        playerController.OnStartStomp += PlayStomp;
    }

    // Update is called once per frame
    void Update() {
        animator.SetFloat("horizontalMovement", Mathf.Abs(playerController.horizontalMovementValue));
        animator.SetFloat("verticalMovement", playerController.verticalMovementValue);
        animator.SetBool("isGrounded", playerController.isGrounded);
        animator.SetBool("isCrouching", playerController.isDucking);
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
