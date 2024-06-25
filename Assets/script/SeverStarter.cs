using FishNet;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine.UI;


public class SeverStarter : NetworkBehaviour
{
    [SerializeField] private Button severStartButton;
    void Start()
    {
        severStartButton.onClick.AddListener(ServerStart);
    }
    private void ServerStart()
    {
        InstanceFinder.ServerManager.StartConnection(7777);
        InstanceFinder.ServerManager.SetFrameRate(120);
    }
}
