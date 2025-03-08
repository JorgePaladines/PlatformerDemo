using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeMovement : MonoBehaviour {
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float horizontalJumpForce = 3f;
    
    public enum EnemyState {
        Idle,
        JumpStart,
        Jumping,
        ToFall,
        Falling,
        Landing
    }
    
    public EnemyState currentState = EnemyState.Idle;

    void Start() {
        
    }

    void Update() {
        
    }
}
