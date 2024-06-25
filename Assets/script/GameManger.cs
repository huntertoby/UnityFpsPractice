using System.Collections.Generic;
using Demo.Scripts.Runtime.Character;
using FishNet;
using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    


    [SerializeField] public GameObject teamUi;

     public Transform[] team1PlayerTransforms = new Transform[5];
     public Transform[] team2PlayerTransforms = new Transform[5];
    
     public Transform[] _attackerSpawnTransforms ;
     public Transform[] _defenderSpawnTransforms ;

    

    

    [SerializeField] private AudioListener audioListener;
    
    public enum TeamSide
    {
        Defender, Attacker
    }

    private int _round;
    public int _team1Win;
    private TeamSide _team1Side;
    public int _team1Person;
    public int _team2Win;
    private TeamSide _team2Side; 
    public int _team2Person;
    
    [HideInInspector] public State _gameState;
    
    public enum State
    {
        BuyIng, Fighting, Wait
    }

    public float buyingTime;
    
    public float roundTime;

    public float waitTime;
    
    [HideInInspector] public float time;

    private bool _timeOut;

     private GameObject _bomb;

    [SerializeField] private Canvas canvas;
    
    private HashSet<int> _readyClients = new HashSet<int>();

    private bool _gameStarted = false;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        _attackerSpawnTransforms = GameObject.Find("Map").GetComponent<Map>().attackerSpawnTransforms;
        _defenderSpawnTransforms = GameObject.Find("Map").GetComponent<Map>().defenderSpawnTransforms;
    }
    
    
    [ServerRpc(RequireOwnership = false)]
    public void CmdGameStart()
    {
        ServerGameStart();
    }
    [Server]
    public void ServerGameStart()
    {
        time = buyingTime;
        SetTimeSide(TeamSide.Defender,TeamSide.Attacker);
        time = buyingTime;
        _gameState = State.BuyIng;
        RpcGameStart();
    }
    
    [Server]
    private void SetTimeSide(TeamSide team1Side,TeamSide team2Side)
    {
        _team1Side = team1Side;
        _team2Side = team2Side;
        RpcGetTimeSide(_team1Side, _team2Side);
    }
    
    [ObserversRpc]
    private void RpcGetTimeSide(TeamSide team1Side,TeamSide team2Side)
    {
        _team1Side = team1Side;
        _team2Side = team2Side;
    }
    
    [ObserversRpc]
    private void RpcGameStart()
    {
        var player = GameObject.Find("MyPlayer").transform;
        var netWorkPlayerControl = player.GetComponent<NetWorkPlayerControl>();
        CmdInitialaizeOtherPlayer(player, true);
        netWorkPlayerControl.InitializedPlayer(true);
        netWorkPlayerControl.SetMovement(false);
        var ui = player.GetComponent<Ui>();
        ui.SetGameUiActive(true);
        ui.GetTimeUI();
        canvas.enabled = false;
        teamUi.SetActive(false);
        AddServerPerson(netWorkPlayerControl.team,player);
    }
    
    [Server]
    private void ResetRound()
    {
        SetTimeSide(_team2Side,_team1Side);
        
        _gameState = State.BuyIng;
        
        time = buyingTime ;
        
        _team1Person = 0;
        _team2Person = 0;
        
        _readyClients = new HashSet<int>();
        
        RpcInitializePlayersForNewRound();
    }

    [ObserversRpc]
    private void RpcInitializePlayersForNewRound()
    {
        var player = GameObject.Find("MyPlayer").transform;
        var netWorkPlayerControl = player.GetComponent<NetWorkPlayerControl>();
        var fpsController = player.GetComponent<FPSController>();
        var health = player.GetComponent<Health>();
        CmdInitialaizeOtherPlayer(player,true);
        ResetBombStatus();
        netWorkPlayerControl.SetMovement(false);
        netWorkPlayerControl.canPlant = false;
        netWorkPlayerControl.haveBomb = false;
        var ui = player.GetComponent<Ui>();
        ui.SetGameUiActive(true);
        fpsController.ChangeWeapon(0);
        fpsController.ResetWeaponAmmo();
        CmdSetMaxHealth(player);
        health.isDied = false;
        SetPlayerSide(netWorkPlayerControl);
        AddServerPerson(netWorkPlayerControl.team,player);
    }

    private void SetPlayerSide(NetWorkPlayerControl netWorkPlayerControl)
    {
        if (netWorkPlayerControl.team == 1) netWorkPlayerControl.playerSide = _team1Side;
        else netWorkPlayerControl.playerSide = _team2Side;
    }       
    
    [ServerRpc(RequireOwnership = false)]
    private void CmdInitialaizeOtherPlayer(Transform player,bool value)
    {
        RpcInitialaizeOtherPlayer(player, value);
    }

    [ObserversRpc]
    private void RpcInitialaizeOtherPlayer(Transform player,bool value)
    {
        player.GetComponent<NetWorkPlayerControl>().InitializedPlayer(value);
    }
    
    

    [ServerRpc(RequireOwnership = false)]
    public void AddServerPerson(int team,Transform player)
    {
        if (team == 1) _team1Person++;
        if (team == 2) _team2Person++;
        CmdClientReady(player.GetComponent<NetworkObject>().OwnerId,player);
    }
    
    [Server]
    public void CmdClientReady(int clientId,Transform player)
    {
        Debug.Log("CmdClientReady");
        _readyClients.Add(clientId);
        var netWorkPlayerControl = player.GetComponent<NetWorkPlayerControl>();
        CheckAllClientsReady();
    }
    
    [Server]
    private void CheckAllClientsReady()
    {
        if (_readyClients.Count == ServerManager.Clients.Count && _gameStarted == false)
        {
            GiveBomb();
            Game();
            _gameStarted = true;
            Debug.Log("Game");
            CmdSetSpawnPlace();
            
        }else if(_readyClients.Count == ServerManager.Clients.Count)
        {
            GiveBomb();
            _timeOut = false;
            isBombExplosionOrDefused = false;
            Debug.Log("NewRound");
            CmdSetSpawnPlace();
        }
    }

    [ObserversRpc]
    private void CmdSetSpawnPlace()
    {
        var player = GameObject.Find("MyPlayer").GetComponent<Transform>();
        var team = player.GetComponent<NetWorkPlayerControl>().team;
        var teamIndex = player.GetComponent<NetWorkPlayerControl>().teamIndex;
        
        if (team == 1)
        {
            if (_team1Side == TeamSide.Attacker) RpcSetSpawnPlace(_attackerSpawnTransforms[teamIndex].position,player);
            else RpcSetSpawnPlace(_defenderSpawnTransforms[teamIndex].position,player);
        }
        else
        {
            if (_team2Side == TeamSide.Attacker) RpcSetSpawnPlace(_attackerSpawnTransforms[teamIndex].position,player);
            else RpcSetSpawnPlace(_defenderSpawnTransforms[teamIndex].position,player);
        }
    }
    
    private void RpcSetSpawnPlace(Vector3 spawnPosition,Transform player)
    {
        if (player.GetComponent<NetworkObject>().IsOwner) player.position = spawnPosition;
    }
    
    
    
    [ServerRpc(RequireOwnership = false)]
    public void GetTime(Transform player)
    {
        ServerGetTime(player);
    }
    [Server]
    public void ServerGetTime(Transform player)
    {
        float minutes = Mathf.FloorToInt(time / 60);
        float seconds = Mathf.FloorToInt(time % 60);
        RpcGetTime(player,string.Format("{0:00}:{1:00}", minutes, seconds));
    }
    [ObserversRpc] 
    void RpcGetTime(Transform player,string timeText)
    {
        player.GetComponent<Ui>().SetTimeUI(timeText);
    }
    
    
    

    [ServerRpc(RequireOwnership = false)]
    public void CmdDealDamage(Transform toWho, float damage)
    {
        Debug.Log(damage);
        toWho.GetComponent<Health>().GotDamage(damage);
        
    }
    [Server]
    public void ServerDealDamage(Transform toWho, float damage)
    {
        toWho.GetComponent<Health>().GotDamage(damage);
    }
    
    
    [ServerRpc(RequireOwnership = false)]
    public void CmdSetMaxHealth(Transform toWho)
    {
        toWho.GetComponent<Health>().ServerSetMaxHealth();
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void CmdBombPlant(Transform whoPlant,GameObject bombPrefab)
    {
        ServerBombPlant(whoPlant, bombPrefab);
    }

    [Server] void ServerBombPlant(Transform whoPlant,GameObject bombPrefab)
    {
        _bomb = Instantiate(bombPrefab,whoPlant.position,Quaternion.LookRotation(Vector3.up));
        Spawn(_bomb);
        _bomb.GetComponent<Bomb>().BombStart();
        RpcSetPlayersTimeOrBombUi("bomb");
        RpcGetBomb(_bomb.transform);
    }
    
    [ObserversRpc]
    private void RpcGetBomb(Transform bombTransform)
    {
        
    }
    
    [ObserversRpc]
    private void RpcSetPlayersTimeOrBombUi(string state)
    {
        var player = GameObject.Find("MyPlayer");
        var ui = player.GetComponent<Ui>();
        if (state == "bomb") ui.bombImage.enabled = true;
        else ui.bombImage.enabled = false;
        ui.time.enabled = !ui.bombImage.enabled;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PlayerDie(int team)
    {
        SeverPlayerDie(team);
    }
    
    [Server]
    public void SeverPlayerDie(int team)
    {
        if (team == 1) _team1Person--;
        if (team == 2) _team2Person--;
        CheckWin();
    }
    
    
    [Server]
    private void CheckWin()
    {
        if (isBombExplosionOrDefused)return;
        if ((_team1Person != 0 && _team2Person != 0) && !_timeOut) return;
        if (_bomb && _team1Side == TeamSide.Defender && _team1Person != 0)return;
        if (_bomb && _team2Side == TeamSide.Defender && _team2Person != 0)return;
        Invoke(nameof(DespawnDestroyBomb),9f);
        _bomb = null;
        if (_team1Person == 0) _team2Win++;
        else _team1Win++;
        _gameState = State.Wait;
        time = waitTime;
        RpcSetPlayerWin(_team1Win,_team2Win);
        RpcSetPlayersTimeOrBombUi("Time");
    }
    
    [ObserversRpc]
    private void RpcSetPlayerWin(int team1Win, int team2Win)
    {
        var player = GameObject.Find("MyPlayer").GetComponent<NetWorkPlayerControl>();
        var ui = player.GetComponent<Ui>();
        if(player.team == 1)ui.SetTeamWin(team1Win.ToString(),team2Win.ToString()); 
        if(player.team == 2)ui.SetTeamWin(team2Win.ToString(),team1Win.ToString());
    }    
    

    [ObserversRpc]
    private void CloseBuyMenu()
    {
        GameObject.Find("MyPlayer").GetComponent<Ui>().CloseBuyMenu();
    }
    
    [ObserversRpc]
    private void SetPlayerMove(bool value)
    {
        GameObject.Find("MyPlayer").GetComponent<FPSMovement>().canMove = value;
    }
    
    public bool isBombExplosionOrDefused;
    

    
    [Server]
    private void GiveBomb()
    {
        Debug.Log(_team1Side);
        // 確認攻擊方
        Transform[] attackerTransforms = _team1Side == TeamSide.Attacker ? team1PlayerTransforms : team2PlayerTransforms;
        
        Debug.Log(_team1Side == TeamSide.Attacker ? "team1PlayerTransforms" : "team2PlayerTransforms");
        
        // 攻擊方玩家數量
        int attackerCount = _team1Side == TeamSide.Attacker ? _team1Person : _team2Person;

        Debug.Log(attackerCount);
        
        // 隨機選擇一名攻擊方玩家
        int randomIndex = Random.Range(0, attackerCount);
        
        Debug.Log(randomIndex);
        
        // 給予炸彈
        int currentCount = 0;
        
        Debug.Log(currentCount);
        
        for (int i = 0; i < attackerTransforms.Length; i++)
        {
            Debug.Log(i);
            if (attackerTransforms[i])
            {
                Debug.Log(i);
                
                if (currentCount == randomIndex)
                {
                    Debug.Log(i);
                    
                    RpcGiveBomb(attackerTransforms[i]);
                    
                    break;
                }
                currentCount++;
            }
        }
    }

    [ObserversRpc]
    private void RpcGiveBomb(Transform player)
    {
        player.GetComponent<NetWorkPlayerControl>().haveBomb = true;
    }
    
    
    [Server]
    public void CmdBombExplosion()
    {
        if (_team1Side == TeamSide.Attacker) _team1Win++;
        else _team2Win++;
        RpcSetPlayerWin(_team1Win,_team2Win);
        _gameState = State.Wait;
        time = waitTime ;
        Invoke(nameof(DespawnDestroyBomb),9f);
        RpcSetPlayersTimeOrBombUi("Time");
    }
    
    [Server]
    public void BombDefused()
    {
        if (_team1Side == TeamSide.Defender) _team1Win++;
        else _team2Win++;
        RpcSetPlayerWin(_team1Win,_team2Win);
        _gameState = State.Wait;
        time = waitTime ;
        isBombExplosionOrDefused = true;
        Invoke(nameof(DespawnDestroyBomb),9f);
        RpcSetPlayersTimeOrBombUi("Time");
    }

    [Server]
    private void DespawnDestroyBomb()
    {
        Despawn(_bomb);
        Destroy(_bomb);
    }
    
    
    [ObserversRpc]
    private void ResetBombStatus()
    {
        GameObject.Find("MyPlayer").GetComponent<NetWorkPlayerControl>().haveBomb = false;
    }
    
    
    

    


    [Server]
    private void Game()
    {
        if (_gameState == State.Wait && time == 0)
        {
            ResetRound();
        }
        else if (_gameState == State.BuyIng && time == 0)
        {
            _gameState = State.Fighting;
            SetPlayerMove(true);
            CloseBuyMenu();
            time = roundTime ;
        }
        else if (time == 0 && _gameState == State.Fighting)
        {
            if (_team1Side == TeamSide.Defender) _team1Win++;
            else _team2Win++;
            _timeOut = true;
            CheckWin();
        }
        
        if((!_bomb || isBombExplosionOrDefused) && _readyClients.Count == ServerManager.Clients.Count) time -= 1;
        
        Invoke(nameof(Game), 1f);
        
        RpcGame(_gameState);
    }

    [ObserversRpc]
    private void RpcGame(State gameState)
    {
        _gameState = gameState;
    }


}
