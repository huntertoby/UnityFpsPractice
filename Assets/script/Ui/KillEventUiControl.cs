using System;
using System.Collections;
using System.Collections.Generic;
using Demo.Scripts.Runtime.Character;
using Demo.Scripts.Runtime.Item;
using FishNet.Object;
using UnityEngine;

public class KillEventUiControl : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private GameObject killEventPrefab;
    
    public void CreatKillEventPrefab(Transform killer,Transform beKiller)
    {
        var myPlayer = GameObject.Find("MyPlayer").transform;
        
        var killEventGameObject = Instantiate(killEventPrefab);
        killEventGameObject.transform.parent = transform;
        
        var beKillerText = killEventGameObject.GetComponent<KillEventPrefab>().beKillerText;



        if (killer.GetComponent<Bomb>())
        {
            KillerIsBomb(killer, myPlayer, killEventGameObject);
        }
        else
        {
            KillerIsPerson(killer, myPlayer, killEventGameObject);
        }
        
        beKillerText.text = beKiller.GetComponent<NetWorkPlayerControl>().playerName;
        
        if(beKiller.GetComponent<NetWorkPlayerControl>().team == myPlayer.GetComponent<NetWorkPlayerControl>().team) beKillerText.color = Color.cyan;
        else beKillerText.color = Color.red;
        
        Debug.Log("CreatKillEventPrefab");
    }

    private void KillerIsPerson(Transform killer,Transform myPlayer,GameObject killEventGameObject)
    {
        var killerText = killEventGameObject.GetComponent<KillEventPrefab>().killerText;
        
        killerText.text = killer.GetComponent<NetWorkPlayerControl>().playerName;
        
        if(killer.GetComponent<NetWorkPlayerControl>().team == myPlayer.GetComponent<NetWorkPlayerControl>().team) 
            killerText.color = Color.cyan;
        else killerText.color = Color.red;
        
        var weapon = killer.GetComponent<FPSController>().GetActiveItem() as Weapon;
        var weaponImage = weapon.weaponImage;
        Sprite weaponSprite = Sprite.Create(
            weaponImage, 
            new Rect(0.0f, 0.0f, weaponImage.width, weaponImage.height), 
            new Vector2(0.5f, 0.5f)
        );

        if (weapon) killEventGameObject.GetComponent<KillEventPrefab>().weaponImage.sprite = weaponSprite;
    }
    
    private void KillerIsBomb(Transform killer,Transform myPlayer,GameObject killEventGameObject)
    {
        var killerText = killEventGameObject.GetComponent<KillEventPrefab>().killerText;
        
        killerText.text = "Bomb";
        
        Debug.Log(myPlayer.GetComponent<NetWorkPlayerControl>().playerSide);
        
        if(myPlayer.GetComponent<NetWorkPlayerControl>().playerSide == GameManager.TeamSide.Attacker) killerText.color = Color.cyan;
        else killerText.color = Color.red;
        
        var weaponImage = killer.GetComponent<Weapon>().weaponImage;
        
        Sprite weaponSprite = Sprite.Create(
            weaponImage, 
            new Rect(0.0f, 0.0f, weaponImage.width, weaponImage.height), 
            new Vector2(0.5f, 0.5f)
        );
        killEventGameObject.GetComponent<KillEventPrefab>().weaponImage.sprite = weaponSprite;
        
        Debug.Log("KillerIsBomb");
    }


    

}
