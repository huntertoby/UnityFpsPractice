using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Bomb : MonoBehaviour
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
    
    
    public void BombStart()
    {
        canBeDefused = true;
        _beepInterval = beepIntervalDefault;
        StartCoroutine(BombBeepCoroutine());
    }

    private IEnumerator BombBeepCoroutine()
    {
        while (_timeLeft > 0 && !defused)
        {
            bombAudioSource.PlayOneShot(bombBeepAudioClip);
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
            canBeDefused = false;
            severAudioSource.PlayOneShot(explosionAudioClip);
            Invoke(nameof(Explosion), 1.9f);
        }
    }

    private void StopBomb()
    {
        // 停止計時並取消爆炸的邏輯
        canBeDefused = false;
        bombAudioSource.Stop();
        severAudioSource.Stop();
        
        GameObject.Find("GameManager").GetComponent<GameManager>().BombDefused();
        // 你可以在這裡添加更多的邏輯來處理拆彈成功的情況
        Invoke(nameof(DestroyBomb), 5f); 
        
    }


    private void Explosion()
    {
        explosionGameObject.SetActive(true);
        GameObject.Find("GameManager").GetComponent<GameManager>().BombExplosion();
        // 處理在範圍內的玩家
        DealDamage();
        
        Invoke(nameof(DestroyBomb), 5f); 
    }
    
    private void DestroyBomb()
    {
        Destroy(gameObject);
    }
    
    

    private void DealDamage()
    {
        foreach (Transform player in _playersInRange)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= explosionRadius)
            {
                float damage = CalculateDamage(distance);
                Health playerHealth = player.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.GotDamage(damage);
                }
            }
        }
    }

    private float CalculateDamage(float distance)
    {
        // 根據距離計算傷害，距離越遠傷害越低
        float damage = maxDamage * (1 - distance / explosionRadius);
        return Mathf.Max(0, damage); // 確保傷害不為負
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<NetWorkPlayerControl>()) // 確保只有玩家進入範圍
        {
            _playersInRange.Add(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<NetWorkPlayerControl>()) // 確保只有玩家離開範圍
        {
            _playersInRange.Remove(other.transform);
        }
    }
}
