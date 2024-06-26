using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiSever : NetworkBehaviour
{
    private int _index = -1;

    private ButtonType _buttonType = ButtonType.No;
    private enum ButtonType
    {
        Attacker, Defender, No
    }

    [SerializeField] public Button[] attackerButton;
    [SerializeField] public Button[] defenderButton;
    [SerializeField] public TMP_InputField inputField;
    [SerializeField] public Button prepare;
    [SerializeField] public Button startButton;

    private bool prepared;

    public Transform player;

    private GameManager _gameManager;

    private int _team;

    private void Start()
    {
        _gameManager = GameManager.Instance;
        startButton.interactable = false;
        prepare.onClick.AddListener(PrePare);
        startButton.onClick.AddListener(StartPlay);
        for (int i = 0; i < 5; i++)
        {
            int index = i; // Store loop variable in a local variable
            attackerButton[index].onClick.AddListener(() => AttackerButtonPress(index));
            defenderButton[index].onClick.AddListener(() => DefenderButtonPress(index));
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsClient)
        {
            CmdRequestAllButtonStates();
        }
    }

    private void StartPlay()
    {
        _gameManager.CmdGameStart();
    }

    private void PrePare()
    {
        prepared = !prepared;
        Color color = prepared ? Color.green : Color.red;
        string text = inputField.text == "" ? "Player" + player.GetComponent<NetworkObject>().OwnerId : inputField.text;
        prepare.GetComponent<Image>().color = color;

        if (_buttonType == ButtonType.Attacker)
            CmdAttackerButton(_index, text, color, false,player);
        if (_buttonType == ButtonType.Defender)
            CmdDefenderButton(_index, text, color, false,player);
        
        CmdGiveTeam(player,_team,_index,text);

        CheckPrePare();
    }

    private void AttackerButtonPress(int value)
    {
        if (_buttonType != ButtonType.No) ResetOldButton();
        string text = inputField.text == "" ? "Player" + player.GetComponent<NetworkObject>().OwnerId : inputField.text;

        CmdAttackerButton(value, text, Color.red, false,player);
        _index = value;
        _buttonType = ButtonType.Attacker;

        _team = 2;

        CmdGiveTeam(player,_team,_index,text);

        ResetPreparedState();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void CmdGiveTeam(Transform player,int team,int teamIndex,string playerName)
    {
        player.GetComponent<NetWorkPlayerControl>().team = team;
        player.GetComponent<NetWorkPlayerControl>().teamIndex = teamIndex;
        player.GetComponent<NetWorkPlayerControl>().playerName = playerName;
        RpcGiveTeam(player,team,teamIndex,playerName);
    }
    
    [ObserversRpc]
    private void RpcGiveTeam(Transform player,int team,int teamIndex,string playerName)
    {
        player.GetComponent<NetWorkPlayerControl>().team = team;
        player.GetComponent<NetWorkPlayerControl>().teamIndex = teamIndex;
        player.GetComponent<NetWorkPlayerControl>().playerName = playerName;
    }

    private void DefenderButtonPress(int value)
    {
        if (_buttonType != ButtonType.No) ResetOldButton();
        string text = inputField.text == "" ? "Player" + player.GetComponent<NetworkObject>().OwnerId : inputField.text;

        CmdDefenderButton(value, text, Color.red, false,player);
        _index = value;
        _buttonType = ButtonType.Defender;

        _team = 1;

        CmdGiveTeam(player,_team,_index,text);

        ResetPreparedState();
    }

    private void CheckPrePare()
    {
        startButton.interactable = true;

        for (int i = 0; i < 5; i++)
        {   
            if (attackerButton[i].gameObject.GetComponentInChildren<TextMeshProUGUI>().text != "Empty" &&
                attackerButton[i].GetComponent<Image>().color == Color.red)
            {
                startButton.interactable = false;
                return;
            }
            if (defenderButton[i].gameObject.GetComponentInChildren<TextMeshProUGUI>().text != "Empty" &&
                defenderButton[i].GetComponent<Image>().color == Color.red)
            {
                startButton.interactable = false;
                return;
            }
        }
    }


    private void ResetOldButton()
    {
        CmdGiveTeam(player,0,-1,"");

        if (_buttonType == ButtonType.Attacker)
        {
            CmdAttackerButton(_index, "Empty", Color.white, true,null);
        }
        else if (_buttonType == ButtonType.Defender)
        {
            CmdDefenderButton(_index, "Empty", Color.white, true,null);
        }
        _buttonType = ButtonType.No;
    }

    private void ResetPreparedState()
    {
        prepared = false;
        prepare.GetComponent<Image>().color = Color.red;
        CheckPrePare();
    }

    [ObserversRpc]
    private void RpcUpdateButtonState(int buttonIndex, string text, Color color, bool isAttacker,bool clickable)
    {
        Button button = isAttacker ? attackerButton[buttonIndex] : defenderButton[buttonIndex];
        button.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = text;
        button.GetComponent<Image>().color = color;
        button.interactable = clickable;
        CheckPrePare();
    }

    [ObserversRpc]
    private void RpcUpdatePlayerName(Transform player, string name)
    {
        player.GetComponent<NetWorkPlayerControl>().playerName = name;
    }


    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestAllButtonStates()
    {
        for (int i = 0; i < attackerButton.Length; i++)
        {
            Color color = attackerButton[i].GetComponent<Image>().color;
            string text = attackerButton[i].gameObject.GetComponentInChildren<TextMeshProUGUI>().text;
            bool clickable = attackerButton[i].interactable;
            RpcUpdateButtonState(i, text, color, true,clickable);
            if (_gameManager.team2PlayerTransforms[i])
                RpcUpdatePlayerName(_gameManager.team2PlayerTransforms[i],
                    _gameManager.team2PlayerTransforms[i].GetComponent<NetWorkPlayerControl>().playerName);

        }

        for (int i = 0; i < defenderButton.Length; i++)
        {
            Color color = defenderButton[i].GetComponent<Image>().color;
            string text = defenderButton[i].gameObject.GetComponentInChildren<TextMeshProUGUI>().text;
            bool clickable = defenderButton[i].interactable;
            RpcUpdateButtonState(i, text, color, false,clickable);
            if (_gameManager.team1PlayerTransforms[i])
                RpcUpdatePlayerName(_gameManager.team1PlayerTransforms[i],
                    _gameManager.team1PlayerTransforms[i].GetComponent<NetWorkPlayerControl>().playerName);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdAttackerButton(int value, string text, Color color, bool clickable,Transform player)
    {
        attackerButton[value].GetComponentInChildren<TextMeshProUGUI>().text = text;
        attackerButton[value].GetComponent<Image>().color = color;
        attackerButton[value].interactable = clickable;
        _gameManager.team2PlayerTransforms[value] = player;
        RpcUpdateButtonState(value, text, color, true,clickable);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdDefenderButton(int value, string text, Color color, bool clickable,Transform player)
    {
        defenderButton[value].GetComponentInChildren<TextMeshProUGUI>().text = text;
        defenderButton[value].GetComponent<Image>().color = color;
        defenderButton[value].interactable = clickable;
        _gameManager.team1PlayerTransforms[value] = player;
        RpcUpdateButtonState(value, text, color, false,clickable);
    }
}
