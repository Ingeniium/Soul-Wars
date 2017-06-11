using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class SpawnManager : NetworkBehaviour {
    public static List<SpawnManager> AllySpawnPoints = new List<SpawnManager>();
    public static List<SpawnManager> EnemySpawnPoints = new List<SpawnManager>() ;
    public static List<SpawnManager> UnclaimedSpawnPoints = new List<SpawnManager>();
    public static float enemy_respawn_time = 5;
    public static float ally_respawn_time = 3;
    public GameObject stand;
    public Vector3 spawn_direction;
	// Use this for initialization
    void Awake()
    {
        switch (gameObject.layer)
        {
            case 9:
                AllySpawnPoints.Add(this);
                break;
            case 8:
                EnemySpawnPoints.Add(this);
                break;
            case 0:
                UnclaimedSpawnPoints.Add(this);
                break;
        }
	}

    public static IEnumerator WaitForRespawn(HealthDefence killed) 
    {
        if (killed.gameObject.layer == 9)
        {
            if (SpawnManager.AllySpawnPoints.Count != 0)
            {
                SpawnManager.AllySpawnPoints[0].RpcOnClientDeath(killed.gameObject);
            }
            else
            {
                SpawnManager.EnemySpawnPoints[0].RpcOnClientDeath(killed.gameObject);
            }
        }
        else if (killed.gameObject.layer == 8) 
        {
            if (SpawnManager.AllySpawnPoints.Count != 0)
            {
                SpawnManager.AllySpawnPoints[0].RpcDisableScripts(killed.gameObject);
            }
            else
            {
                SpawnManager.EnemySpawnPoints[0].RpcDisableScripts(killed.gameObject);
            }
            yield return new WaitForSeconds(enemy_respawn_time);
            while (EnemySpawnPoints.Count < 1)
            {
                yield return new WaitForEndOfFrame();
            }
            killed.HP = killed.maxHP;
            killed.transform.position = EnemySpawnPoints[0].transform.position + EnemySpawnPoints[0].spawn_direction;
            if (SpawnManager.AllySpawnPoints.Count != 0)
            {
                SpawnManager.AllySpawnPoints[0].RpcEnableScripts(killed.gameObject);
            }
            else
            {
                SpawnManager.EnemySpawnPoints[0].RpcEnableScripts(killed.gameObject);
            }
        }
        int l = killed.gameObject.layer;
        killed.gameObject.layer = 15;
        killed.RpcChangeLayer(15);
        killed.StartCoroutine(Blink(killed.gameObject));
        yield return new WaitForSeconds(1.5f);
        killed.gameObject.layer = l;
        killed.RpcChangeLayer(l);
    }

    [ClientRpc]
    public void RpcOnClientDeath(GameObject g)
    {
        if (PlayerController.Client.netId == g.GetComponent<NetworkIdentity>().netId)
        {
            StartCoroutine(OnClientDeath(g));
        }
    }

    public static IEnumerator OnClientDeath(GameObject g)
    {
        if (SpawnManager.AllySpawnPoints.Count != 0)
        {
            SpawnManager.AllySpawnPoints[0].RpcDisableScripts(g);
        }
        else
        {
            SpawnManager.EnemySpawnPoints[0].RpcDisableScripts(g);
        }
        HealthDefence killed = g.GetComponent<HealthDefence>();
        (killed.Controller as PlayerController).cam_show.GetComponent<PlayerFollow>().Player = null;
        RespawnInterface.Instance.respawning = true;
        yield return new WaitForSeconds(ally_respawn_time);
        while (AllySpawnPoints.Count < 1)
        {
             yield return new WaitForEndOfFrame();
        }
        while (RespawnInterface.Instance.respawning)
        {
             yield return new WaitForEndOfFrame();
        }
        if (SpawnManager.AllySpawnPoints.Count != 0)
        {
            SpawnManager.AllySpawnPoints[0].RpcEnableScripts(killed.gameObject);
        }
        else
        {
            SpawnManager.EnemySpawnPoints[0].RpcEnableScripts(killed.gameObject);
        }
        killed.HP = killed.maxHP;
        killed.transform.position = AllySpawnPoints[RespawnInterface.Instance.spawn_index].transform.position + AllySpawnPoints[RespawnInterface.Instance.spawn_index].spawn_direction;
        (killed.Controller as PlayerController).cam_show.GetComponent<PlayerFollow>().Player = killed.Controller as PlayerController;
        
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
            yield return new WaitForEndOfFrame();
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
        if (Respawned.tag == "Player")
        {
            Respawned.layer = 9;
        }
        else
        {
            Respawned.layer = 8;
        }
    }
	
}
