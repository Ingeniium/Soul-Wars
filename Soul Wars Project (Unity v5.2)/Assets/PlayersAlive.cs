using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayersAlive : NetworkBehaviour
{
    public static PlayersAlive Instance;
    public SyncListUInt Players = new SyncListUInt();

    void Awake()
    {
        Instance = this;
    }
}

