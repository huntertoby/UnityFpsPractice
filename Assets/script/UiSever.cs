using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiSever : NetworkBehaviour
{
    // Start is called before the first frame update
    
    private int _index = -1;

    private ButtonType _buttonType = ButtonType.No;
    private enum ButtonType
    {
        Attacker,Defender,No
    }
    
    private Button[] _attackerButton;
    private Button[] _defenderButton;
    private TMP_InputField _inputField;
    private Button _prepare;

    private Button _startButton;

    private bool _prepared;

    [HideInInspector]public Transform player;

    private GameManager gameManager;
    
    private void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _attackerButton = gameManager.attackerButton;
        _defenderButton = gameManager.defenderButton;
        _inputField = gameManager.inputField;
        _prepare = gameManager.prepare;
        _startButton = gameManager.startButton;
        
        _startButton.interactable = false;
        
        _prepare.onClick.AddListener(PrePare);
        _startButton.onClick.AddListener(StartPlay);
        
        for (int i = 0; i < 5; i++)
        {
            int index = i; // 在本地變量中存儲迴圈變量
            _attackerButton[index].onClick.AddListener(() => AttackerButtonPress(index));
        }
        for (int i = 0; i < 5; i++)
        {
            int index = i; // 在本地變量中存儲迴圈變量
            _defenderButton[index].onClick.AddListener(() => DefenderButtonPress(index));
        }
    }

    private void StartPlay()
    {
        CmdStartPlay();
    }
    
    [ObserversRpc]
    private void RpcStartPlay()
    {        
        gameManager.GameStart();
    }

    [ServerRpc] 
    public void CmdStartPlay()
    {
        RpcStartPlay();
    }
    
    private void PrePare()
    {
        _prepared = !_prepared;
        Color color;
        if (_prepared) color = Color.green;
        else color = Color.red;
        string text;
        if (_inputField.text == "") text = "Player" + NetworkObject.OwnerId;
        else text = _inputField.text;
        if (_buttonType == ButtonType.Attacker) CmdAttackerButton(_index,text,color,false,player);
        if (_buttonType == ButtonType.Defender) CmdDefenderButton(_index,text,color,false,player);
    }
    
    private void AttackerButtonPress(int value)
    {
        if (_buttonType != ButtonType.No) ResetOldButton();
        string text;
        if (_inputField.text == "") text = "Player" + NetworkObject.OwnerId;
        else text = _inputField.text;
        
        CmdAttackerButton(value,text,Color.red,false,player);
        _index = value;
        _buttonType = ButtonType.Attacker;
    }

    private void CheckPrePare()
    {
        _startButton.interactable = true;
        
        for (int i = 0; i < 5; i++)
        {
            if (_attackerButton[i].gameObject.GetComponentInChildren<TextMeshProUGUI>().text != "Empty" &&
                _attackerButton[i].GetComponent<Image>().color == Color.red)
            {
                _startButton.interactable = false;
                return;
            }
            if (_defenderButton[i].gameObject.GetComponentInChildren<TextMeshProUGUI>().text != "Empty" &&
                _defenderButton[i].GetComponent<Image>().color == Color.red)
            {
                _startButton.interactable = false;
                return;
            }
        }
    }
    
    private void DefenderButtonPress(int value)
    {
        if (_buttonType != ButtonType.No) ResetOldButton();
        string text;
        if (_inputField.text == "") text = "Player" + NetworkObject.OwnerId; 
        else text = _inputField.text; 
        CmdDefenderButton(value,text,Color.red,false,player);
        _index = value;
        _buttonType = ButtonType.Defender;
    }

    private void ResetOldButton()
    {
        if (_buttonType==ButtonType.Attacker)
        {
            CmdAttackerButton(_index,"Empty",Color.white,true,null);
            _buttonType = ButtonType.No;
        }
        else if(_buttonType==ButtonType.Defender)
        { ;
            CmdDefenderButton(_index,"Empty",Color.white,true,null);
            _buttonType = ButtonType.No;
        }
    }
    
   
    [ObserversRpc]
    private void RpcAttackerButton(int value,string text,Color color,bool clickable,Transform transform)
    {
        Button button = _attackerButton[value];
        button.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = text;
        button.GetComponent<Image>().color = color;
        button.interactable = clickable;
        gameManager.team1PlayerTransforms[value] = transform;
        CheckPrePare();
    }

    [ServerRpc]
    public void CmdAttackerButton(int value,string text,Color color,bool clickable,Transform transform)
    {
        RpcAttackerButton(value,text,color,clickable,transform);
    }
    
    [ObserversRpc]
    private void RpcDefenderButton(int value,string text,Color color,bool clickable,Transform transform)
    {
        Button button = _defenderButton[value];
        button.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = text;
        button.GetComponent<Image>().color = color;
        button.interactable = clickable;
        gameManager.team2PlayerTransforms[value] = transform;
        CheckPrePare();
    }

    [ServerRpc]
    public void CmdDefenderButton(int value,string text,Color color,bool clickable,Transform transform)
    {
        RpcDefenderButton(value,text,color,clickable,transform);
    }
}
