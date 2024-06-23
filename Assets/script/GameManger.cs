using Demo.Scripts.Runtime.Character;
using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private Button severStartButton;
    [SerializeField] private Button clientStartButton;
    
    [SerializeField] public Button[] attackerButton;
    [SerializeField] public Button[] defenderButton;
    [SerializeField] public TMP_InputField inputField;
    [SerializeField] public Button prepare;

    [SerializeField] public GameObject teamUi;
    [SerializeField] public GameObject startersUi;

    [SerializeField] public Transform[] team1PlayerTransforms = new Transform[5];
    [SerializeField] public Transform[] team2PlayerTransforms = new Transform[5];

    [SerializeField] public Button startButton;

    [HideInInspector] public bool gameStart = false;

    [SerializeField] private AudioListener audioListener;
    
    public enum TeamSide
    {
        Defender, Attacker
    }

    private int _round;
    private int _team1Win;
    private TeamSide _team1Side;
    private int _team1Person;
    private int _team2Win;
    private TeamSide _team2Side; 
    private int _team2Person;
    
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

    public Bomb bomb;
    
    void Start()
    {
        severStartButton.onClick.AddListener(ServerStart);
        clientStartButton.onClick.AddListener(ClientStart);
    }

    private void ServerStart()
    {
        GameObject.Find("SeverStarter").GetComponent<SeverStarter>().enabled = true;
    }
   
    private void ClientStart()
    {
        GameObject.Find("SeverStarter").GetComponent<ClientStarter>().enabled = true;
    }

    public void GameStart()
    {
        time = buyingTime;
        
        Transform[] attackerSpawnTransforms = GameObject.Find("Map").GetComponent<Map>().attackerSpawnTransforms;
        Transform[] defenderSpawnTransforms = GameObject.Find("Map").GetComponent<Map>().defenderSpawnTransforms;

        _team1Side = TeamSide.Attacker;
        _team2Side = TeamSide.Defender;
        
        InitializePlayers(team1PlayerTransforms, attackerSpawnTransforms, 1, ref _team1Person);
        InitializePlayers(team2PlayerTransforms, defenderSpawnTransforms, 2, ref _team2Person);

        GameObject.Find("Canvas").GetComponent<Canvas>().enabled = false;
        teamUi.SetActive(false);

        time = buyingTime;
        _gameState = State.BuyIng;
        audioListener.enabled = false;
        
        GiveBomb();

        Game();
    }

    private void InitializePlayers(Transform[] playerTransforms, Transform[] spawnTransforms, int team, ref int teamPersonCount)
    {
        for (int i = 0; i < playerTransforms.Length; i++)
        {
            if (playerTransforms[i])
            {
                playerTransforms[i].position = spawnTransforms[i].position;
                NetWorkPlayerControl netWorkPlayerControl = playerTransforms[i].GetComponent<NetWorkPlayerControl>();
                netWorkPlayerControl.InitializedPlayer(true);
                netWorkPlayerControl.team = team;
                netWorkPlayerControl.SetMovement(false);
                Ui ui = playerTransforms[i].GetComponent<Ui>();
                ui.SetGameManager();
                ui.SetGameUiActive(true);
                ui.TimeUI();
                teamPersonCount++;
            }
        }
    }

    public string GetTime()
    {
        float minutes = Mathf.FloorToInt(time / 60);
        float seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void PlayerDie(int team)
    {
        if (team == 1) _team1Person--;
        if (team == 2) _team2Person--;
        CheckWin();
    }

    private void CheckWin()
    {
        if (_isBombExplosionOrDefused)return;
        if ((_team1Person != 0 && _team2Person != 0) && !_timeOut) return;
        if (_team1Person == 0) _team2Win++;
        if (_team2Person == 0) _team1Win++;
        _gameState = State.Wait;
        time = waitTime + 1;
        SetPlayerWin();
        
        SetPlayersTimeOrBombUi("Time");
    }

    private void SetPlayerWin()
    {
        UpdatePlayersUI(team1PlayerTransforms, _team1Win, _team2Win);
        UpdatePlayersUI(team2PlayerTransforms, _team2Win, _team1Win);
    }

    private void UpdatePlayersUI(Transform[] playerTransforms, int teamWin, int otherTeamWin)
    {
        foreach (var player in playerTransforms)
        {
            if (player)
            {
                player.GetComponent<Ui>().SetTeamWin(teamWin.ToString(), otherTeamWin.ToString());
            }
        }
    }

    private void CloseBuyMenu()
    {
        SetPlayerUI(team1PlayerTransforms, false);
        SetPlayerUI(team2PlayerTransforms, false);
    }

    private void SetPlayerUI(Transform[] playerTransforms, bool active)
    {
        foreach (var player in playerTransforms)
        {
            
            if (player)
            {
                if (player.GetComponent<Ui>().buyMenu)
                {
                    player.GetComponent<Ui>().buyMenu.SetActive(active);
                }
            }
        }
    }

    private void SetPlayerMove(bool value)
    {
        UpdatePlayerMovement(team1PlayerTransforms, value);
        UpdatePlayerMovement(team2PlayerTransforms, value);
    }

    private void UpdatePlayerMovement(Transform[] playerTransforms, bool canMove)
    {
        foreach (var player in playerTransforms)
        {
            if (player)
            {
                player.GetComponent<NetWorkPlayerControl>().SetMovement(canMove);
            }
        }
    }

    private void ResetRound()
    {
        (_team1Side, _team2Side) = (_team2Side, _team1Side);
        
        // 重置攻擊方所有玩家的炸彈狀態
        ResetBombStatus(team1PlayerTransforms);
        ResetBombStatus(team2PlayerTransforms);
        
        time = buyingTime + 1;
        
        Transform[] attackerSpawnTransforms = GameObject.Find("Map").GetComponent<Map>().attackerSpawnTransforms;
        Transform[] defenderSpawnTransforms = GameObject.Find("Map").GetComponent<Map>().defenderSpawnTransforms;

        _team1Person = 0;
        _team2Person = 0;

        if (_team1Side == TeamSide.Attacker)
        {
            InitializePlayersForNewRound(team1PlayerTransforms, attackerSpawnTransforms, 1, ref _team1Person);
            InitializePlayersForNewRound(team2PlayerTransforms, defenderSpawnTransforms, 2, ref _team2Person);
        }
        else
        {
            InitializePlayersForNewRound(team1PlayerTransforms, defenderSpawnTransforms, 1, ref _team1Person);
            InitializePlayersForNewRound(team2PlayerTransforms, attackerSpawnTransforms, 2, ref _team2Person);
        }

        GiveBomb();

        _gameState = State.BuyIng;
        _timeOut = false;
        _isBombExplosionOrDefused = false;
    }
    
    private void InitializePlayersForNewRound(Transform[] playerTransforms, Transform[] spawnTransforms, int team, ref int teamPersonCount)
    {
        for (int i = 0; i < playerTransforms.Length; i++)
        {
            if (playerTransforms[i])
            {
                playerTransforms[i].position = spawnTransforms[i].position;
                NetWorkPlayerControl netWorkPlayerControl = playerTransforms[i].GetComponent<NetWorkPlayerControl>();
                netWorkPlayerControl.SetMovement(false);
                netWorkPlayerControl.InitializedPlayer(true);
                netWorkPlayerControl.canPlant = false;
                Health health = playerTransforms[i].GetComponent<Health>();
                health.SetMaxHealth();
                FPSController fpsController = playerTransforms[i].GetComponent<FPSController>();
                fpsController.ChangeWeapon(0);
                fpsController.ResetWeaponAmmo();
                Ui ui = playerTransforms[i].GetComponent<Ui>();
                ui.SetGameUiActive(true);
                health.UpdateTwoBar();
                teamPersonCount++;
            }
        }
    }

    private bool _isBombExplosionOrDefused;


    public void BombExplosion()
    {
        if (_team1Side == TeamSide.Attacker) _team1Win++;
        else _team2Win++;
        SetPlayerWin();
        _gameState = State.Wait;
        time = waitTime + 1;
        _isBombExplosionOrDefused = true;
        
        bomb = null;
        
        SetPlayersTimeOrBombUi("Time");
    }

    private void GiveBomb()
    {
        // 確認攻擊方
        Transform[] attackerTransforms = _team1Side == TeamSide.Attacker ? team1PlayerTransforms : team2PlayerTransforms;
        
        // 攻擊方玩家數量
        int attackerCount = _team1Side == TeamSide.Attacker ? _team1Person : _team2Person;

        // 隨機選擇一名攻擊方玩家
        int randomIndex = Random.Range(0, attackerCount);
    
        // 給予炸彈
        int currentCount = 0;
        for (int i = 0; i < attackerTransforms.Length; i++)
        {
            if (attackerTransforms[i])
            {
                if (currentCount == randomIndex)
                {
                    NetWorkPlayerControl playerControl = attackerTransforms[i].GetComponent<NetWorkPlayerControl>();
                    playerControl.haveBomb = true;
                    break;
                }
                currentCount++;
            }
        }
    }

    public void BombDefused()
    {
        if (_team1Side == TeamSide.Defender) _team1Win++;
        else _team2Win++;
        SetPlayerWin();
        _gameState = State.Wait;
        time = waitTime + 1;
        _isBombExplosionOrDefused = true;

        bomb = null;

        SetPlayersTimeOrBombUi("Time");
    }
    
    

    private void ResetBombStatus(Transform[] playerTransforms)
    {
        foreach (var player in playerTransforms)
        {
            if (player)
            {
                NetWorkPlayerControl playerControl = player.GetComponent<NetWorkPlayerControl>();
                playerControl.haveBomb = false;
            }
        }
    }

    public void SetPlayersTimeOrBombUi( string state)
    {
        SetPlayerTimeOrBombUi(team1PlayerTransforms, state);
        SetPlayerTimeOrBombUi(team2PlayerTransforms,state);
    }
    
    private void SetPlayerTimeOrBombUi(Transform[] playerTransforms, string state)
    {
        foreach (var player in playerTransforms)
        {
            if (!player)return;
            if (!player.GetComponent<Ui>().time) return;
            if  (!player.GetComponent<Ui>().bombImage) return;
            
            if (player && state == "bomb")
            {
                player.GetComponent<Ui>().bombImage.enabled = true;
                player.GetComponent<Ui>().time.enabled = false;
            }
            else if (player)
            {
                player.GetComponent<Ui>().bombImage.enabled = false;
                player.GetComponent<Ui>().time.enabled = true;
            }
        }
    }


    private void Game()
    {
        if (_gameState == State.Wait && time == 0)
        {
            ResetRound();
        }
        else if (_gameState == State.BuyIng && time == 0)
        {
            time = roundTime + 1;
            _gameState = State.Fighting;
            SetPlayerMove(true);
            CloseBuyMenu();
        }
        else if (time == 0 && _gameState == State.Fighting)
        {
            if (_team1Side == TeamSide.Defender) _team1Win++;
            else _team2Win++;
            _timeOut = true;
            CheckWin();
        }
        
        if(!bomb)time -= 1;
        
        Invoke(nameof(Game), 1f);
    }
}
