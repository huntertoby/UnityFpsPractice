using FishNet;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;


public class SeverStarter : NetworkBehaviour
{
    void Start()
    {
        InstanceFinder.ServerManager.StartConnection(7777);
        InstanceFinder.ServerManager.SetFrameRate(120);
    }
    
    
}
