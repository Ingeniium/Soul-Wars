using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SpawnManager : NetworkBehaviour
{
    private static List<SpawnManager> AllySpawnPoints = new List<SpawnManager>();
    private static List<SpawnManager> EnemySpawnPoints = new List<SpawnManager>();
    private static List<SpawnManager> EnemySpawnPoints2 = new List<SpawnManager>();
    private static List<SpawnManager> EnemySpawnPoints3 = new List<SpawnManager>();
    private static List<SpawnManager> EnemySpawnPoints4 = new List<SpawnManager>();
    private static List<SpawnManager> TotalSpawnPoints = new List<SpawnManager>();
    private static List<List<SpawnManager>> SpawnTeamList = new List<List<SpawnManager>>();
    private HealthDefence HP;
    static float cpu_respawn_time = 2;
    public int ally_defence_bonus = 30;
    public static float ally_respawn_time = 3;
    public GameObject stand;
    public Vector3 spawn_direction;
    static List<Color> team_colors = new List<Color>()
    {
        Color.blue,
        Color.red,
        Color.green,
        Color.gray,
        Color.yellow
    };
    // Use this for initialization

    void Awake()
    {
        SpawnTeamList.Add(AllySpawnPoints);
        SpawnTeamList.Add(EnemySpawnPoints);
        SpawnTeamList.Add(EnemySpawnPoints2);
        SpawnTeamList.Add(EnemySpawnPoints3);
        SpawnTeamList.Add(EnemySpawnPoints4);
        HP = GetComponent<HealthDefence>();
        TotalSpawnPoints.Add(this);
    }

    void Start()
    {
        if (isServer)
        {
            RpcChangeTeam(gameObject.layer);
            ChangeTeam(gameObject.layer);
        }
    }

    public static void BeforeSceneLoad()
    {
        /*Oddly enough,static variables actually REMAIN across scene changes.*/
        if (PlayerController.Client.isServer)
        {
            foreach (SpawnManager s in TotalSpawnPoints)
            {
                NetworkServer.Destroy(s.gameObject);
            }
        }
        TotalSpawnPoints.Clear();
        foreach (List<SpawnManager> sp in SpawnTeamList)
        {
            sp.Clear();
        }
        SpawnTeamList.Clear();
    }

    /*Gets the list of spawn points captured by that specific team
     (denoted by their respective layer)*/
    public static List<SpawnManager> GetTeamSpawns(int layer)
    {
        if (layer == LayerMask.NameToLayer("Ally"))
        {
            return AllySpawnPoints;
        }
        else if (layer == LayerMask.NameToLayer("Enemy"))
        {
            return EnemySpawnPoints;
        }
        else
        {
            const int EXTRA_TEAM_OFFSET = 16;
            return SpawnTeamList[layer - EXTRA_TEAM_OFFSET];
        }
    }

    public static List<SpawnManager> GetOpponentSpawns(int excluded_layer)
    {
        List<SpawnManager> spawn_points = new List<SpawnManager>();
        foreach (SpawnManager s in TotalSpawnPoints)
        {
            if (s.gameObject.layer != excluded_layer)
            {
                spawn_points.Add(s);
            }
        }
        return spawn_points;
    }

    /*Gets color of the specific team (denoted by their respective layer)*/
    public static Color GetTeamColor(int layer, float alpha = 1)
    {
        if (layer == LayerMask.NameToLayer("Ally"))
        {
            return new Color(team_colors[0].r,
                team_colors[0].g,
                team_colors[0].b,
                alpha);
        }
        else if (layer == LayerMask.NameToLayer("Enemy"))
        {
            return new Color(team_colors[1].r,
                team_colors[1].g,
                team_colors[1].b,
                alpha);
        }
        else
        {
            const int EXTRA_TEAM_OFFSET = 16;
            return new Color(team_colors[layer - EXTRA_TEAM_OFFSET].r,
                team_colors[layer - EXTRA_TEAM_OFFSET].g,
                team_colors[layer - EXTRA_TEAM_OFFSET].b,
                alpha);
        }
    }

    [ClientRpc]
    public void RpcChangeTeam(int layer)
    {
        ChangeTeam(layer);
    }

    /*Changes the team of the spawn point */
    void ChangeTeam(int layer)
    {
        if (layer != gameObject.layer)
        {
            List<SpawnManager> prev_list = GetTeamSpawns(gameObject.layer);
            prev_list.Remove(this);
            if (prev_list == AllySpawnPoints)
            {
                HP.defence -= ally_defence_bonus;
            }
        }
        List<SpawnManager> new_list = GetTeamSpawns(layer);
        if (!new_list.Contains(this))
        {
            new_list.Add(this);
            if (new_list == AllySpawnPoints)
            {
                HP.defence += ally_defence_bonus;
            }
            Color new_color = GetTeamColor(layer);
            Renderer[] rends = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in rends)
            {
                r.material.color = new_color;
            }
            gameObject.layer = layer;
        }
    }

    /*Deals with the process of death and respawn*/
    public static IEnumerator WaitForRespawn(HealthDefence killed)
    {
        int ORIGINAL_LAYER = killed.gameObject.layer;
        List<SpawnManager> team_spawns = GetTeamSpawns(ORIGINAL_LAYER);
        NetworkMethods.Instance.CmdSetLayer(killed.gameObject, LayerMask.NameToLayer("Invincible"));
        TotalSpawnPoints[0].RpcDisableScripts(killed.gameObject);

        bool is_cpu = !killed.Controller || killed.Controller is AIController;
        if (is_cpu)
        {
            yield return new WaitForSeconds(cpu_respawn_time);
        }
        else
        {
            PlayersAlive.Instance.Players.Remove(killed.netId.Value);
            TotalSpawnPoints[0].RpcInterface(killed.netId);
            yield return new WaitForSeconds(ally_respawn_time);
        }

        while (team_spawns.Count < 1)
        {
            yield return new WaitForEndOfFrame();
        }

        if (is_cpu)
        {
            killed.gameObject.transform.position = team_spawns[0].transform.position
             + team_spawns[0].spawn_direction;
           (killed.Controller as AIController).ResetHateList();
        }
        else
        {
            PlayersAlive.Instance.Players.Add(killed.netId.Value);
            while (RespawnInterface.Instance.respawning)
            {
                yield return new WaitForEndOfFrame();
            }
            int index = RespawnInterface.Instance.spawn_index;
            killed.gameObject.transform.position = team_spawns[index].transform.position
                + team_spawns[index].spawn_direction;

        }

        killed.HP = killed.maxHP;
        TotalSpawnPoints[0].RpcEnableScripts(killed.gameObject);
        TotalSpawnPoints[0].RpcBlink(killed.gameObject);
        NetworkMethods.Instance.CmdSetLayer(killed.gameObject, ORIGINAL_LAYER);
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


