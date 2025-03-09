using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDamageUIScript : MonoBehaviour {
    public float duration = 1f;

    void Start() {
        Destroy(gameObject, duration);
    }
}
