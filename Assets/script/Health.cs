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
        RpcUpdateHealthAndShield(shield, health);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SeverSetShield(float value)
    {
        shield = maxShield * value;
        RpcUpdateHealthAndShield(shield, health);
    }
    
    
    [Server]
    public void GotDamage(Transform whoDid,float damage)
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

        if (whoDid.GetComponent<Bomb>())
        {
            Debug.Log(" 炸彈對 " 
            + transform.GetComponent<NetWorkPlayerControl>().playerName+" 造成了 "+ damage+ " 點傷害");
        }
        else
        {
            Debug.Log(whoDid.GetComponent<NetWorkPlayerControl>().playerName + " 對 " 
            + transform.GetComponent<NetWorkPlayerControl>().playerName+" 造成了 "+ damage+ " 點傷害");
        }
        
        Debug.Log(transform.GetComponent<NetWorkPlayerControl>().playerName+ " 還剩下 Shield: " + shield + " health " + health);
        
        RpcUpdateHealthAndShield(shield, health);
        CheckIsDie(whoDid);
    }
    
    [ObserversRpc]
    public void RpcUpdateHealthAndShield(float newShield, float newHealth)
    {
        Debug.Log(transform.GetComponent<NetWorkPlayerControl>().playerName+ " 還剩下 Shield: " + shield + " health " + health);
        shield = newShield;
        health = newHealth;
        UpdateTwoBar();
    }

    private void UpdateTwoBar()
    {
        if (!IsOwner)return;
        _ui.SethHealthAndShield(shield/maxShield,health/maxHealth);
    }
    
    [Server]
    private void CheckIsDie(Transform whoDid)
    {
        if (health == 0 && !isDied)
        {
            ServerIsDied(true);

            if (whoDid.GetComponent<Bomb>())
            {
                GameManager.Instance.ServerPlayerDie(
                    -transform.GetComponent<NetWorkPlayerControl>().team,
                    transform,
                    whoDid);
            }
            else
            {
                GameManager.Instance.ServerPlayerDie(
                    transform.GetComponent<NetWorkPlayerControl>().team,
                    transform,
                    whoDid);
            }
            

        }
    }
    
    [Server]
    public void ServerIsDied(bool value)
    {
        isDied = value;
        RpcIsDied(value);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void CmdIsDied(bool value)
    {
        isDied = value;
        RpcIsDied(value);
    }
    
    [ObserversRpc]
    private void RpcIsDied(bool value)
    {
        isDied = value;
        if(value && IsOwner)GetComponent<NetWorkPlayerControl>().SetDie();
    }
    

    


    
    
}

