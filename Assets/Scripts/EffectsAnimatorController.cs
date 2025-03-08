using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EffectsAnimatorController : MonoBehaviour {
    PlayerMovement player;
    JumpAbility jumpAbility;
    StompAbility stompAbility;

    [SerializeField] EffectSprite effectPrefab;

    void Start() {
        player = GetComponentInParent<PlayerMovement>();
        jumpAbility = GetComponentInParent<JumpAbility>();
        stompAbility = GetComponentInParent<StompAbility>();

        jumpAbility.OnDoubleJump += DoubleJumpEffectCall;
        stompAbility.OnEndStomp += StompLandEffectCall;
    }

    void Update() {
        
    }

    void DoubleJumpEffectCall(object sender, EventArgs e) {
        SpawnEffect("doubleJump");
    }

    void StompLandEffectCall(object sender, EventArgs e) {
        SpawnEffect("endStomp");
    }

    public EffectSprite SpawnEffect(string effectName) {
        EffectSprite effect = Instantiate(effectPrefab, player.transform.position, Quaternion.identity, null);
        Animator animator = effect.GetComponent<Animator>();
        animator.SetTrigger(effectName);
        return effect;
    }
}
