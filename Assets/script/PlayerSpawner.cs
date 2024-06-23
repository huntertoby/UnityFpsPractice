using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    public GameObject playerPrefab;

    [SerializeField] private Transform SpawnTransform;
    public override void OnStartServer()
    {
        base.OnStartServer();
        InstanceFinder.ServerManager.OnRemoteConnectionState += OnClientConnected;
    }

    private void OnClientConnected(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        Debug.Log("Client connected, state: " + args.ConnectionState);
        
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            Debug.Log("Spawning player for connection: " + conn.ClientId);
            GameObject playerInstance = Instantiate(playerPrefab,SpawnTransform.position,Quaternion.LookRotation(Vector3.forward, Vector3.up));
            ServerManager.Spawn(playerInstance, conn);
        }
    }
}