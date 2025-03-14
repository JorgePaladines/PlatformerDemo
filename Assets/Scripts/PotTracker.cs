using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    using TMPro;

public class PotTracker : MonoBehaviour {
    [SerializeField] private List<GameObject> Pots;
    [SerializeField] private GameObject door;
    private Collider2D doorCollider;
    private SpriteBreaker spriteBreaker;
    private TextMeshPro potCounterText;

    // Start is called before the first frame update
    void Start() {
        potCounterText = GetComponentInChildren<TextMeshPro>();
        doorCollider = door.GetComponent<Collider2D>();
        spriteBreaker = door.GetComponent<SpriteBreaker>();
    }

    // Update is called once per frame
    void Update(){
        Pots.RemoveAll(pot => pot == null);
        potCounterText.text = Pots.Count + " pots left";
        if (Pots.Count <= 0) {
            doorCollider.enabled = false;
            spriteBreaker.Break();
        }
    }
}
