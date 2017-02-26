using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class RespawnInterface : MonoBehaviour
{
    public static RespawnInterface Instance;
    public Canvas wait_for_respawn;
    private Canvas wait_for_respawn_show;
    public Canvas game_over;
    private Canvas game_over_show;
    public Canvas choose_respawn_location;
    private Canvas choose_respawn_location_show;
    private Text spawn_index_text;
    private int _spawn_index = 0;
    public  int spawn_index
    {
        get { return _spawn_index; }
        set
        {
            if (value == SpawnManager.AllySpawnPoints.Count)
            {
                _spawn_index = 0;
            }
            else if (value < 0)
            {
                _spawn_index = SpawnManager.AllySpawnPoints.Count - 1;
            }
            else
            {
                _spawn_index = value;
            }
            PlayerFollow.camera.transform.position = SpawnManager.AllySpawnPoints[spawn_index].transform.position + PlayerFollow.offset;
        }
    }
    public bool respawning
    {
        get { return enabled; }
        set
        {          
            if (value == true)
            {
                wait_for_respawn_show = Instantiate(wait_for_respawn, wait_for_respawn.transform.position, wait_for_respawn.transform.rotation) as Canvas;
                wait_for_respawn_show.worldCamera = PlayerFollow.camera;
                StartCoroutine(Cooldown.NumericalCooldown(wait_for_respawn_show, SpawnManager.ally_respawn_time));
            }
            enabled = value;
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (SpawnManager.AllySpawnPoints.Count == 0 && PlayerFollow.Player == null)
        {
            if (wait_for_respawn_show)
            {
                Destroy(wait_for_respawn_show.gameObject);
            }
            if (choose_respawn_location_show)
            {
                Destroy(choose_respawn_location_show.gameObject);
            }
            game_over_show = Instantiate(game_over, game_over.transform.position, game_over.transform.rotation) as Canvas;
            game_over_show.worldCamera = PlayerFollow.camera;
            enabled = false;
        }
        if (!wait_for_respawn_show && !choose_respawn_location_show)
        {
            if (SpawnManager.AllySpawnPoints.Count > 1)
            {
                choose_respawn_location_show = Instantiate(choose_respawn_location, choose_respawn_location.transform.position, choose_respawn_location.transform.rotation) as Canvas;
                choose_respawn_location_show.worldCamera = PlayerFollow.camera;
                PlayerFollow.camera.transform.position = SpawnManager.AllySpawnPoints[spawn_index].transform.position + PlayerFollow.offset;
                Button[] buttons = choose_respawn_location_show.GetComponentsInChildren<Button>();
                spawn_index_text = choose_respawn_location_show.GetComponentInChildren<Text>();
                spawn_index_text.text = spawn_index.ToString();
                buttons[0].onClick.AddListener(delegate()
                {
                    spawn_index -= 1;
                    spawn_index_text.text = spawn_index.ToString();
                });
                buttons[1].onClick.AddListener(delegate()
                {
                    respawning = false;
                    spawn_index = spawn_index;//performs a last check if there are last second changes in claimed points
                    if (choose_respawn_location_show)
                    {
                        Destroy(choose_respawn_location_show.gameObject);
                    }
                });
                buttons[2].onClick.AddListener(delegate()
                {
                    spawn_index += 1;
                    spawn_index_text.text = spawn_index.ToString();
                });
            }
            else if (SpawnManager.AllySpawnPoints.Count == 1)
            {
                spawn_index = 0;
                respawning = false;
            }
        }
      
    }

   
}
