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
            PlayerFollow.Player = null;
            killed.transform.position = new Vector3(1000, 1000, -1000);
            yield return new WaitForSeconds(ally_respawn_time);
            while (AllySpawnPoints.Count < 1)
            {
               yield return new WaitForEndOfFrame();
            }
            killed.HP = killed.maxHP;
            killed.transform.position = AllySpawnPoints[0].transform.position;
            PlayerFollow.Player = killed.gameObject;
        }
        else
        {
            killed.transform.position = new Vector3(1000, 1000, 1000);
            yield return new WaitForSeconds(enemy_respawn_time);
            while (EnemySpawnPoints.Count < 1)
            {
                yield return new WaitForEndOfFrame();
            }
            killed.HP = killed.maxHP;
            killed.transform.position = EnemySpawnPoints[0].transform.position;
        }
    }
	
}
