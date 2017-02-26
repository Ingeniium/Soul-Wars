using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour {
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
        DisableScripts(killed.gameObject);
        if (killed.gameObject.layer == 9)
        {
            PlayerFollow.Player = null;
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
            killed.HP = killed.maxHP;
            killed.transform.position = AllySpawnPoints[RespawnInterface.Instance.spawn_index].transform.position + AllySpawnPoints[RespawnInterface.Instance.spawn_index].spawn_direction;
            PlayerFollow.Player = killed.gameObject;
        }
        else
        {
            yield return new WaitForSeconds(enemy_respawn_time);
            while (EnemySpawnPoints.Count < 1)
            {
                yield return new WaitForEndOfFrame();
            }
            killed.HP = killed.maxHP;
            killed.transform.position = EnemySpawnPoints[0].transform.position + EnemySpawnPoints[0].spawn_direction;
        }
        EnableScripts(killed.gameObject);
        killed.StartCoroutine(Blink(killed.gameObject));
    }

    private static void DisableScripts(GameObject killed)
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

    private static void EnableScripts(GameObject killed)
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
        Respawned.layer = 15;
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
