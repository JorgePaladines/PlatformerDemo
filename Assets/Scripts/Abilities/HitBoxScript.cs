using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBoxScript : MonoBehaviour {
    [SerializeField] float damage = 10f;
    [SerializeField] float damageMultiplier = 2f;

    private void Start(){
        gameObject.SetActive(false);
    }

    public float GetDamage(){
        return damage;
    }

    public float GetMultiplier(){
        return damageMultiplier;
    }
}
