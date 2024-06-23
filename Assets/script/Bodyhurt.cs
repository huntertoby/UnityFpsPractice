using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class Bodyhurt : MonoBehaviour
{
    // Start is called before the first frame update
    private Health _health;
    
    void Start()
    {
        _health = GetComponentInParent<Health>();
    }

    private void OnCollisionEnter(Collision other)
    {
        Bullet bullet = other.gameObject.GetComponent<Bullet>();
        
        NetworkObject networkObject = transform.root.GetComponent<NetworkObject>();
        
        Debug.Log("GotHurt");
        
        if (bullet)
        {
            _health.GotShot(bullet);
        }
    }
}
