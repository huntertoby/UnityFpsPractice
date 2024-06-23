using System;
using System.Collections;
using System.Collections.Generic;
using Demo.Scripts.Runtime.Item;
using FishNet.Connection;
using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    
    
    private Rigidbody _bulletRigidbody;
    [SerializeField] private GameObject bulletHoleImpactPrefab;
    [SerializeField] private GameObject bloodImpactPrefab;
    [SerializeField] private float bulletSpeed = 10f;

    [SerializeField] public float damage;
    
    [HideInInspector]public Weapon weapon;
    private void Awake()
    {
        _bulletRigidbody = GetComponent<Rigidbody>();
    }
    void Start()
    {
        _bulletRigidbody.velocity = transform.forward * bulletSpeed;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        float offset = 0.01f; // 微小偏移量
        GameObject prefab;

        if (collision.transform.root.gameObject.CompareTag("Player"))
        {
            prefab = bloodImpactPrefab;
            if (weapon) { weapon.ShotPeople(); }
        }
        else
        {
            prefab = bulletHoleImpactPrefab;
        }
        Instantiate(prefab, contact.point + contact.normal * offset, Quaternion.LookRotation(contact.normal));
        Destroy(gameObject);
    }
}