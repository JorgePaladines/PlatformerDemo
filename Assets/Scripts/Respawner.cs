using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawner : MonoBehaviour {
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Transform respawnPoint;
    
    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            player.transform.position = respawnPoint.position;
        }
    }
}
