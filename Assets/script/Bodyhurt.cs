using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class Bodyhurt : MonoBehaviour
{
    // Start is called before the first frame update
    private NetWorkPlayerControl _netWorkPlayerControl ;
    
    void Start()
    {
        _netWorkPlayerControl = GetComponentInParent<NetWorkPlayerControl>();
    }

    private void OnCollisionEnter(Collision other)
    {
        // if (transform.root.GetComponent<NetworkObject>().IsOwner) return;
        //
        // Bullet bullet = other.gameObject.GetComponent<Bullet>();
        //
        // NetworkObject networkObject = transform.root.GetComponent<NetworkObject>();
        //
        // Debug.Log("GotHurt");
        //
        // if (bullet)
        // {
        //     _netWorkPlayerControl.CmdGotDamage(bullet.damage);
        // }
    }
}
