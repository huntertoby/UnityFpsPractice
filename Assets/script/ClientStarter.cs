using System.Collections;
using System.Collections.Generic;
using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientStarter : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    void Start()
    {
        InstanceFinder.ClientManager.StartConnection(inputField.text, 7777);
        InstanceFinder.ClientManager.SetFrameRate(120);
    }
}
