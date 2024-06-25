using System.Collections;
using System.Collections.Generic;
using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientStarter : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button clientStartButton; 
    void Start()
    {
        clientStartButton.onClick.AddListener(ClientStart);
    }
    
    private void ClientStart()
    {
        // InstanceFinder.ClientManager.StartConnection(inputField.text, 7777);
        InstanceFinder.ClientManager.StartConnection("127.0.0.1", 7777);
        InstanceFinder.ClientManager.SetFrameRate(120);
    }
}
