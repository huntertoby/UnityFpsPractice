using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class Map : NetworkBehaviour
{
    [SerializeField] public Transform[] attackerSpawnTransforms;
    [SerializeField] public Transform[] defenderSpawnTransforms;
}
