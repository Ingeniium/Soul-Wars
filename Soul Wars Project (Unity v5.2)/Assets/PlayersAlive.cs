using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.Networking;

public class PlayersAlive : NetworkBehaviour
{
    public static PlayersAlive Instance;
    public SyncListUInt Players = new SyncListUInt();
    public List<AIController> Units = new List<AIController>();
    public List<BulletScript> Bullets = new List<BulletScript>();
    public List<Vector3> bullet_velocities = new List<Vector3>();
    float time_elapsed_when_paused;
    bool paused;

    void Awake()
    {
        Instance = this;
        if(Players.Count != 0)
        {
            Players.Clear();
        }
    }

    [Command]
    public void CmdPause()
    {
        RpcPause();
    }

    IEnumerator TimeElapsed()
    {
        float start_time = Time.realtimeSinceStartup;
        while(paused)
        {
            yield return new WaitForEndOfFrame();
            time_elapsed_when_paused = Time.realtimeSinceStartup - start_time;
        }
    }
    [ClientRpc]
    void RpcPause()
    {
        uint[] array = new uint[Players.Count];
        int i = 0;
        foreach (uint u in Players)
        {
            array[i] = u;
            i++;
        }
        if (!Array.Exists(array, delegate (uint u)
         {
             GameObject player_obj = ClientScene.FindLocalObject(
                 new NetworkInstanceId(u));
             PlayerController player = player_obj.GetComponent<PlayerController>();
             return player.enabled;
             
         }))
        {
            if(Bullets.Count > 0)
            {
                foreach(BulletScript b in Bullets)
                {
                    if (b)
                    {
                        bullet_velocities.Add(b.rb.velocity);
                        b.enabled = false;
                        b.rb.Sleep();
                        if(!paused)
                        {
                            b.lasting_time += 1000000;
                        }
                        
                    }
                }
            }
            if (Units.Count > 0)
            {
                foreach(AIController AI in Units)
                {
                    if (AI)
                    {
                        AI.GetComponentInParent<Rigidbody>().useGravity = false;
                        AI.ptr.gameObject.GetComponent<Collider>().enabled = false;
                        AI.enabled = false;
                    }
                }
            }
            if(!paused)
            {
                StartCoroutine(TimeElapsed());
            }
            paused = true;
        }
    }

    [Command]
    public void CmdUnpause()
    {
        RpcUnpause();
    }

    [ClientRpc]
    void RpcUnpause()
    {
        if (Bullets.Count > 0)
        {
            int i = 0;
            foreach (BulletScript b in Bullets)
            {
                if (b)
                {
                    b.enabled = true;
                    b.rb.WakeUp();
                    if (i < bullet_velocities.Count)
                    {
                        b.rb.velocity = bullet_velocities[i];
                    }
                    i++;
                    if (paused)
                    {
                        b.lasting_time += time_elapsed_when_paused;
                        b.lasting_time -= 999998;
                    }
                }
            }
        }
        if (Units.Count > 0)
        {
            foreach (AIController AI in Units)
            {
                if (AI)
                {
                    AI.ptr.GetComponent<Collider>().enabled = true;
                    AI.GetComponentInParent<Rigidbody>().useGravity = true;
                    AI.enabled = true;
                }
            }
        }
        paused = false;
    }
}

