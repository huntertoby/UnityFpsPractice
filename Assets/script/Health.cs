using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class Health : NetworkBehaviour
{
    // Start is called before the first frame update
    private Ui _ui;

    [SerializeField] private float health;
    [SerializeField] private float maxHealth;
    [SerializeField] private float shield;
    [SerializeField] private float maxShield;

    private Bullet _preBullet;

    public bool isDied;
    
    void Start()
    {
        _ui = GetComponent<Ui>();

    }
    
    [Server]
    public void ServerSetMaxHealth()
    {
        health = maxHealth; 
        shield = maxShield;
        RpcUpdateHealthAndShield(shield, health);
    }
    
    [Server]
    public void GotDamage(float damage)
    {
        Debug.Log(damage);
        if (shield > 0)
        {
            if (shield >= damage)
            {
                shield -= damage;
                damage = 0;
            }
            else
            {
                damage -= shield;
                shield = 0;
            }
        }

        if (damage > 0)
        {
            health -= damage;
        }

        if (health<0)
        {
            health = 0;
        }
        RpcUpdateHealthAndShield(shield, health);
        Debug.Log(shield + " " + health);
    }
    
    [ObserversRpc]
    public void RpcUpdateHealthAndShield(float newShield, float newHealth)
    {
        Debug.Log(newShield +" "+newHealth);
        shield = newShield;
        health = newHealth;
        UpdateTwoBar();
    }

    public void UpdateTwoBar()
    {
        if (!IsOwner)return;
        _ui.SethHealthAndShield(shield/maxShield,health/maxHealth);
        if (health == 0 && !isDied)
        {
            GetComponent<NetWorkPlayerControl>().SetDie();
            isDied = true;
        }
    }
    

    


    
    
}

