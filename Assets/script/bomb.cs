using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Serialization;

public class Bomb : NetworkBehaviour
{
    [SerializeField] private AudioSource severAudioSource;
    [SerializeField] private AudioSource bombAudioSource;
    [SerializeField] public AudioClip bombPlantingAudioClip;
    [SerializeField] public AudioClip bombPlantedAudioClip;
    [SerializeField] public AudioClip bombBeepAudioClip;
    [SerializeField] public AudioClip explosionAudioClip;
    [SerializeField] public GameObject explosionGameObject;
    [SerializeField] private float explosionRadius = 15f; // 爆炸半徑
    [SerializeField] private float maxDamage = 100f; // 最大傷害

    private List<Transform> _playersInRange = new List<Transform>(); // 儲存進入範圍的玩家

    [SerializeField] private float _timeLeft = 45f; // 炸彈計時器總時間
    private float _beepInterval = 1f; // 初始beep間隔時間
    [SerializeField] private float beepIntervalDefault = 1f; // 初始beep間隔時間
    
    [HideInInspector] public bool canBeDefused;

    [HideInInspector] public bool defused;
    
    [Server]
    public void BombStart()
    {
        RpcCanBeDefused(true);
        _beepInterval = beepIntervalDefault;
        StartCoroutine(BombBeepCoroutine());
    }
    
    [ObserversRpc]
    private void RpcBombBeepAudioClip()
    {
        bombAudioSource.PlayOneShot(bombBeepAudioClip);
    }
    
    private IEnumerator BombBeepCoroutine()
    {
        while (_timeLeft > 0 && !defused)
        {
            RpcBombBeepAudioClip();
            yield return new WaitForSeconds(_beepInterval);
            _timeLeft -= _beepInterval;

            // 隨著時間推移加快beep的頻率
            if (_timeLeft <= 5f)
            {
                _beepInterval = beepIntervalDefault / 8;
            }
            else if (_timeLeft <= 10f)
            {
                _beepInterval = beepIntervalDefault / 4;
            }
            else if (_timeLeft <= 30f)
            {
                _beepInterval = beepIntervalDefault / 2;
            }
        }

        if (defused)
        {
            StopBomb();
        }
        else
        {
            RpcCanBeDefused(false);
            GameManager.Instance.isBombExplosionOrDefused = true;
            RpcAlreadyExplosion();
            Invoke(nameof(ServerExplosion), 1.4f);
            Invoke(nameof(RpcExplosion), 1.5f);
            Invoke(nameof(DealDamage), 1.5f);
        }
    }

    [ObserversRpc]
    private void RpcCanBeDefused(bool value)
    {
        canBeDefused = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void Defused()
    {
        defused = true;
    }
    
    [Server]
    private void ServerExplosion()
    {
        GameManager.Instance.CmdBombExplosion();
    }
    
    [ObserversRpc]
    private void RpcExplosion()
    {
        Debug.Log("RpcExplosion");
        explosionGameObject.SetActive(true);
    }
    
    [ObserversRpc]
    private void RpcAlreadyExplosion()
    {
        severAudioSource.PlayOneShot(explosionAudioClip);
    }
    
    
    [Server]
    private void StopBomb()
    {
        // 停止計時並取消爆炸的邏輯
        RpcCanBeDefused(false);
        bombAudioSource.Stop();
        severAudioSource.Stop();
        GameManager.Instance.BombDefused();
    }


    
    [Server]
    private void DealDamage()
    {
        foreach (Transform player in _playersInRange)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= explosionRadius)
            {
                Debug.Log("DealDamage");
                float damage = CalculateDamage(distance);
                GameManager.Instance.ServerDealDamage(player, damage);
            }
        }
    }
    
    private float CalculateDamage(float distance)
    {
        // 根據距離計算傷害，距離越遠傷害越低
        float damage = maxDamage * (1 - distance / explosionRadius);
        damage = Mathf.Round(damage); // 將傷害值四捨五入為整數
        return Mathf.Max(0, damage); // 確保傷害不為負
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<NetWorkPlayerControl>())
            CmdOnTrigger(other.transform,true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<NetWorkPlayerControl>())
            CmdOnTrigger(other.transform,true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<NetWorkPlayerControl>()) 
            CmdOnTrigger(other.transform,false);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void CmdOnTrigger(Transform player, bool enter)
    {
        if (_playersInRange.Contains(player))return;
        if (enter) _playersInRange.Add(player);
        else _playersInRange.Remove(player);
    }
}
