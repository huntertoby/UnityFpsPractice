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
    
    void Start()
    {
        _ui = GetComponent<Ui>();

    }

    public void SetMaxHealth()
    {
        health = maxHealth; 
        shield = maxShield;
    }
    public void GotDamage(float damage)
    {
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

        UpdateTwoBar();
    }

    public void GotShot(Bullet bullet)
    {
        if (_preBullet == bullet) return;
        _preBullet = bullet;
        
        float damage = bullet.damage;
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

        UpdateTwoBar();
    }

    public void UpdateTwoBar()
    {
        if (!IsOwner)return;
        _ui.SethHealthAndShield(shield/maxShield,health/maxHealth);
        if (health == 0)
        {
            GetComponent<NetWorkPlayerControl>().SetDie();
        }
    }
    
    
}

