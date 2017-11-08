using System;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

/*Class created for syncing a client scene to that of the hosts in the
 event that the host is in a scene other than the first*/
public class NetworkSceneSync : NetworkBehaviour
{
    public int current_level;
    [SyncVar] public uint server_level;//This represents the scene the server is on
    public readonly uint max_level = 10;
    public static NetworkSceneSync Instance;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnStartClient()
    {
         if(current_level != server_level)
        {
            SceneManager.LoadScene("Level " + server_level);
        }
    }
}
