using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;



public class EnemyInitialization : NetworkBehaviour
{
    [ServerCallback]
     void Start()
    {
        GameObject Enemy;
        foreach (EnemyGroup e in GetComponents<EnemyGroup>())
        {
            Enemy = Instantiate(e.Enemy, e.pos, Quaternion.identity) as GameObject;
            NetworkServer.Spawn(Enemy);
        }
    }
}
