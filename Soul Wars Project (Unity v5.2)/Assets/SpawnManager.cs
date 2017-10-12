using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class SpawnManager : NetworkBehaviour {
    public static List<SpawnManager> AllySpawnPoints = new List<SpawnManager>();
    public static List<SpawnManager> EnemySpawnPoints = new List<SpawnManager>() ;
    private static List<SpawnManager> TotalSpawnPoints = new List<SpawnManager>();
    private HealthDefence HP;
    public static float enemy_respawn_time = 10f;
    public int ally_defence_bonus = 30;
    public static float ally_respawn_time = 3;
    public GameObject stand;
    public Vector3 spawn_direction;
	// Use this for initialization

    void Awake()
    {
        HP = GetComponent<HealthDefence>();
        TotalSpawnPoints.Add(this);
        switch (gameObject.layer)
        {
            case 9:
                AllySpawnPoints.Add(this);
                HP.defence += ally_defence_bonus;
                break;
            case 8:
                EnemySpawnPoints.Add(this);
                break;
        }

    }

    public static void BeforeSceneLoad()
    {
        Debug.Log("Called");
        /*Oddly enough,static variables actually REMAIN across scene changes.*/
        if (PlayerController.Client.isServer)
        {
            foreach (SpawnManager s in TotalSpawnPoints)
            {
                NetworkServer.Destroy(s.gameObject);
            }
        }
        TotalSpawnPoints.Clear();
        AllySpawnPoints.Clear();
        EnemySpawnPoints.Clear();
    }

    [ClientRpc]
    public void RpcMakeEnemy()
    {
        AllySpawnPoints.Remove(this);
        EnemySpawnPoints.Add(this);
        HP.defence -= ally_defence_bonus;
    }

    [ClientRpc]
    public void RpcMakeAlly()
    {
        EnemySpawnPoints.Remove(this);
        AllySpawnPoints.Add(this);
        HP.defence += ally_defence_bonus;
    }
    
    public static IEnumerator WaitForRespawn(HealthDefence killed) 
    {

        NetworkMethods.Instance.CmdSetLayer(killed.gameObject, LayerMask.NameToLayer("Invincible"));
        int layer;
            if (SpawnManager.AllySpawnPoints.Count != 0)
            {
                SpawnManager.AllySpawnPoints[0].RpcDisableScripts(killed.gameObject);
            }
            else
            {
                SpawnManager.EnemySpawnPoints[0].RpcDisableScripts(killed.gameObject);
            }
            if (killed.gameObject.layer == 8)
            {
                yield return new WaitForSeconds(enemy_respawn_time);
                while (EnemySpawnPoints.Count < 1)
                {
                    yield return new WaitForEndOfFrame();
                }
            killed.transform.position = EnemySpawnPoints[0].transform.position + EnemySpawnPoints[0].spawn_direction;
            layer = LayerMask.NameToLayer("Enemy");
            }
            else
            {
            layer = LayerMask.NameToLayer("Ally");
                PlayersAlive.Instance.Players.Remove(killed.netId.Value);
                if (SpawnManager.AllySpawnPoints.Count != 0)
                {
                    SpawnManager.AllySpawnPoints[0].RpcInterface(killed.netId);
                }
                else
                {
                    SpawnManager.EnemySpawnPoints[0].RpcInterface(killed.netId);
                }
                yield return new WaitForSeconds(ally_respawn_time);
                while (AllySpawnPoints.Count < 1)
                {
                    yield return new WaitForEndOfFrame();
                }
                PlayersAlive.Instance.Players.Add(killed.netId.Value);
            }
            killed.HP = killed.maxHP;
            if (SpawnManager.AllySpawnPoints.Count != 0)
            {
                SpawnManager.AllySpawnPoints[0].RpcEnableScripts(killed.gameObject);
                SpawnManager.AllySpawnPoints[0].RpcBlink(killed.gameObject);
            }
            else
            {
                SpawnManager.EnemySpawnPoints[0].RpcEnableScripts(killed.gameObject);
                SpawnManager.EnemySpawnPoints[0].RpcBlink(killed.gameObject);
            }
        NetworkMethods.Instance.CmdSetLayer(killed.gameObject, layer);



    }

    [ClientRpc]
    void RpcInterface(NetworkInstanceId ID)
    {
        if (PlayerController.Client.netId == ID)
        {
            RespawnInterface.Instance.respawning = true;
        }
    }

    [ClientRpc]
    void RpcBlink(GameObject g)
    {
        StartCoroutine(Blink(g));
    }

   

    [ClientRpc]
    private void RpcDisableScripts(GameObject killed)
    {
        killed.GetComponent<Renderer>().enabled = false;
        Renderer[] child_rends = killed.GetComponentsInChildren<Renderer>();
        if (child_rends.Length > 0)
        {
            foreach (Renderer r in child_rends)
            {
                r.enabled = false;
            }
        }
        killed.GetComponent<Collider>().enabled = false;
        Collider shield_collider = killed.GetComponentInChildren<Collider>();
        if (shield_collider)
        {
            shield_collider.enabled = false;
        }
        MonoBehaviour[] parent_scripts = killed.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in parent_scripts)
        {
            script.enabled = false;
        }
        MonoBehaviour[] child_scripts = killed.GetComponentsInChildren<MonoBehaviour>();
        if (child_scripts.Length > 0)
        {
            foreach (MonoBehaviour script in child_scripts)
            {
                script.enabled = false;
            }
        }
    }

    [ClientRpc]
    private void RpcEnableScripts(GameObject killed)
    {
        killed.GetComponent<Renderer>().enabled = true;
        Renderer[] child_rends = killed.GetComponentsInChildren<Renderer>();
        if (child_rends.Length > 0)
        {
            foreach (Renderer r in child_rends)
            {
                r.enabled = true;
            }
        }
        killed.GetComponent<Collider>().enabled = true;
        Collider shield_collider = killed.GetComponentInChildren<Collider>();
        if (shield_collider)
        {
            shield_collider.enabled = true;
        }
        MonoBehaviour[] parent_scripts = killed.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in parent_scripts)
        {
            script.enabled = true;
        }
        MonoBehaviour[] child_scripts = killed.GetComponentsInChildren<MonoBehaviour>();
        if (child_scripts.Length > 0)
        {
            foreach (MonoBehaviour script in child_scripts)
            {
                script.enabled = true;
            }
        }
    }

    public static IEnumerator Blink(GameObject Respawned, float invis_time = 1.5f)
    {
        Renderer rend = Respawned.GetComponent<Renderer>();
        Renderer[] child_rends = Respawned.GetComponentsInChildren<Renderer>();
        rend.enabled = false;
        foreach (Renderer r in child_rends)
        {
            r.enabled = false;
        }
        invis_time += Time.time;
        while (Time.time < invis_time)
        {
            yield return new WaitForSecondsRealtime(.2f);
            switch (rend.enabled)
            {
                case true:
                    rend.enabled = false;
                    foreach (Renderer r in child_rends)
                    {
                        r.enabled = false;
                    }
                     break;
                case false:
                    rend.enabled = true;
                    foreach (Renderer r in child_rends)
                    {
                        r.enabled = true;
                    }
                    break;
            }
        }
        rend.enabled = true;
        foreach (Renderer r in child_rends)
        {
            r.enabled = true;
        }
        
    }
}
	

