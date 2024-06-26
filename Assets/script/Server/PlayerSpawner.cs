using System;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    public GameObject playerPrefab;
    public GameObject gameManagerPrefab; // Add GameManager prefab reference

    [SerializeField] private Transform spawnTransform;

    public override void OnStartServer()
    {
        base.OnStartServer();
        InstanceFinder.ServerManager.OnRemoteConnectionState += OnClientConnected;
        SpawnGameManager(); // Spawn the GameManager on the server
        GameObject.Find("Canvas").GetComponent<Canvas>().enabled = false;
        GameManager.Instance.teamUi.SetActive(true);
    }

    private void OnClientConnected(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        Debug.Log("Client connected, state: " + args.ConnectionState);

        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            Debug.Log("Spawning player for connection: " + conn.ClientId);
            GameObject playerInstance = Instantiate(playerPrefab, spawnTransform.position, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            ServerManager.Spawn(playerInstance, conn);
        }
    }

    private void SpawnGameManager()
    {
        Debug.Log("Spawning GameManager on the server.");
        GameObject gameManagerInstance = Instantiate(gameManagerPrefab, Vector3.zero, Quaternion.identity);
        gameManagerInstance.name = gameManagerPrefab.name;
        ServerManager.Spawn(gameManagerInstance);
    }
}