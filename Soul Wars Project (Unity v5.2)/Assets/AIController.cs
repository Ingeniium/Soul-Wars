using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;


public partial class AIController : GenericController {
    private Rigidbody prb;
    public Transform ptr;
    public HealthDefence Target;
    private bool target_focus = true;
    private Collider trig;
    private List<Collider> bullet_colliders = new List<Collider>();
    private float next_dodge = 0;
    public float shoot_constant = 5;
    public float shoot_delay;
    public float dodge_delay;
    public float dodge_cooldown;
    private Vector3 vec;
    public float reaction_delay;    
    [HideInInspector]
    public SphereCollider enemy_attack_detection;
    public float minimal_distance = 4f;
    private bool guarding = false;
    public float time_until_next_pathfind;
    private Vector3 move_dir;
    public Rigidbody rb;
    float cost_point;
    float dist_point;

    private static Dictionary<int, Func<AIController, Vector3>> MovementFuncs = new Dictionary<int, Func<AIController, Vector3>>()
    {
        {0,Charge},
        {1,MaintainDistance},
        {2,Intercept},
        {3,AvoidConfrontation},  
    };
    private static Dictionary<int, Func<AIController,IEnumerator>> EvasionFuncs = new Dictionary<int, Func<AIController,IEnumerator>>()
    {
        {0,Block},
        {1,SideStep}
    };

    public ValueGroup[] HateList = new ValueGroup[20]
    {
        new ValueGroup(0,-1), new ValueGroup(0,-1), 
        new ValueGroup(0,-1), new ValueGroup(0,-1), 
        new ValueGroup(0,-1), new ValueGroup(0,-1), 
        new ValueGroup(0,-1), new ValueGroup(0,-1), 
        new ValueGroup(0,-1), new ValueGroup(0,-1),
        
        new ValueGroup(0,-1), new ValueGroup(0,-1), 
        new ValueGroup(0,-1), new ValueGroup(0,-1), 
        new ValueGroup(0,-1), new ValueGroup(0,-1), 
        new ValueGroup(0,-1), new ValueGroup(0,-1), 
        new ValueGroup(0,-1), new ValueGroup(0,-1)   
    };

    public int[] attack_func_indexes = new int[]
    {
        1,
        1
    };
    [HideInInspector] public int movement_func_index = 4;
    private int evasion_func_index = 1;
    private static System.Random rand = new System.Random();
    private ObjectiveState State;
    Coordinate Path;
    Coordinate curCoord;
    float GetCoordinateDistFromTarget(Coordinate coord)
    {
        return Math.Abs(
            Vector3.Distance(Map.Instance.GetCenter(coord), Target.transform.position));
    }


    IEnumerator Travel()
    {
        while(this)
        {
            /*The wait for end of frame limits each run of the pathfinding algorithm
             to one execution per frame.The time_until_next_pathfind variable
             serves to spread out its execution between many AI,thereby
             hopefully increasing overall game performance by reducing how much times
             it executes in a single frame.*/
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(time_until_next_pathfind);
            if (Target)
            {
                Path = GetPath();
                if (Path != null)
                {
                    List<Coordinate> list = Path.GetParents();
                    if (list.Count > 1 && list[list.Count - 2] != null)
                    {
                        move_dir = Map.Instance.GetCenter(list[list.Count - 2]) - ptr.position;
                       
                    }
                    else
                    {
                        move_dir = Vector3.zero;
                    }
                }
            }
        }
    }

    Coordinate GetPath()
    {
        List<Coordinate> visited = new List<Coordinate>();
        if (Target)
        {
            Priority_Queue.SimplePriorityQueue<Coordinate> queue = new Priority_Queue.SimplePriorityQueue<Coordinate>();
            Coordinate start = Map.Instance.GetPos(main_gun.barrel_end.position);
            start.parent = null;
            Coordinate end = Map.Instance.GetPos(Target.transform.position);
            float tstart = Time.realtimeSinceStartup;
            queue.Enqueue(start, GetCoordinateDistFromTarget(start));
            while (Time.realtimeSinceStartup < tstart + .01f)
            {
                start = queue.Dequeue();
                if (start == end)
                {
                   /* Debug.Log(Time.realtimeSinceStartup - tstart + " seconds : " +
                         queue.Count + " end routes considered : " +
                         start.GetNumParents() + " parents.");*/
                    return start;
                }
                foreach (Coordinate coord in start.GetChildren())
                {
                   coord.traverse_cost = GetCoordinateDistFromTarget(coord);
                   /* if(coord.status == Coordinate.Status.Hazard)
                    {
                        coord.traverse_cost += 1000f;
                    }
                    int index =  (BulletScript.BulletCoords.FindIndex(delegate (ValueGroup<Coordinate, BulletScript> v)
                     {
                         return (v.index == coord);
                     })) ;
                    if(index != -1)
                    {
                        coord.traverse_cost += BulletScript.BulletCoords[index].value.upper_bound_damage * 3;
                    }*/
                    if (!visited.Contains(coord))
                    {
                        coord.parent = start;
                        queue.Enqueue(coord, coord.GetTotalCost());
                        visited.Add(coord);
                    }
                    else if (queue.Contains(coord) && coord.GetTotalCost(start) < queue.GetPriority(coord))
                    {
                        coord.parent = start;
                        queue.UpdatePriority(coord, coord.GetTotalCost());
                    }


                }
                if (queue.Count != 0)
                {
                    start = queue.First;
                }
                else
                {
                    return null;
                }
            }
        }
        return null;
    }

   
    [ServerCallback]
    void Awake()
    {
        GetComponentInParent<HealthDefence>().Controller = this;
    }

    [ServerCallback]
    void Start()
    {
        
        enemy_attack_detection = GetComponent<SphereCollider>();
        PlayersAlive.Instance.Units.Add(this);
		State = new Conquer (this);
        prb = GetComponentInParent<Rigidbody>();
        StartCoroutine(WaitForPlayers());
       
    }

    [ServerCallback]
    IEnumerator WaitForPlayers()
    {
        while(PlayersAlive.Instance.Players.Count < 1)
        {
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForSeconds(.2f);
        uint[] array = new uint[PlayersAlive.Instance.Players.Count];
        int i = 0;
        foreach (uint u in PlayersAlive.Instance.Players)
        {
            array[i] = u;
            i++;
        }
        PlayersAlive.Instance.CmdPause();
        while (!Array.Exists(array, delegate(uint u)
        {
            return (NetworkServer.FindLocalObject(new NetworkInstanceId(u)).GetComponent<PlayerController>().enabled);
        }))
        {
            yield return new WaitForEndOfFrame();
        }
        PlayersAlive.Instance.CmdUnpause();
        StartCoroutine(Travel());
    }
      

    

    [ServerCallback]
    void FixedUpdate()
    {
        State.AffirmTarget(Target);
        if (Target)
        {
            if (target_focus)
            {
                ptr.LookAt(Target.transform);
            }
            bool hit = WillBulletHitObstacle(main_gun);
            prb.velocity = move_dir.normalized * speed;
            int i = 0;
            foreach (Gun gun in weapons)
            {
                if (AttackFuncs[attack_func_indexes[i]](this, gun) && !hit)
                {
                    main_gun = gun;
                    gun.Shoot();
                }
                i++;
            }
        }
   
    }

    [ServerCallback]
    void OnTriggerEnter(Collider col)
    {
        /*If a player or spawn point was detected within aggro radius,
         react based on State instructions*/
        if (col.gameObject.layer == 9)
        {
            State.UnitAggroReaction(col);
        }
        /*If bullet,prepare to evade*/
        else
        {
            bullet_colliders.Add(col);
            trig = GetClosestBullet();
           
            //StartCoroutine(DetermineEvasion());
        }
    }

    [ServerCallback]
    void OnTriggerExit(Collider col)
    {
        if (col.gameObject.layer != 9)
        {
            bullet_colliders.Remove(col);
        }
    }

    Collider GetClosestBullet()
    {
        List<ValueGroup> distances = new List<ValueGroup>();   
        /*using foreach in this place casues an invalid operation exception when a collider DOES need to be destroyed,as 
         due to the destruction itself*/
        for (int i = 0; i < bullet_colliders.Count; i++)
        {
            if (bullet_colliders[i] == null)
            {
                bullet_colliders.RemoveAt(i);
            }
        }       
        for(int i = 0;i < bullet_colliders.Count;i++)
        {
            try
            {
                distances.Add(new ValueGroup(i, Vector3.Distance(ptr.position, bullet_colliders[i].gameObject.transform.position)));
            }
            catch (System.Exception e) 
            {
                if (distances.Count > i)
                {
                    distances.RemoveAt(i);
                }
            }
        }     
        distances.Sort(delegate(ValueGroup lhs, ValueGroup rhs)
        {
            if (lhs.value > rhs.value)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        });
        return bullet_colliders[distances[0].index];      
    }

    static Vector3 Charge(AIController AI)
    {
        return AI.Charge();
    }

    static Vector3 MaintainDistance(AIController AI)
    {
        return AI.MaintainDistance();
    }

    static Vector3 Intercept(AIController AI)
    {
        return AI.Intercept();
    }

    static Vector3 AvoidConfrontation(AIController AI)
    {
        return AI.AvoidConfrontation();
    }


    static IEnumerator Block(AIController AI)
    {
         AI.StartCoroutine(AI.Block());
         yield return null;
    }

    static IEnumerator SideStep(AIController AI)
    {
        AI.StartCoroutine(AI.SideStep());
        yield return null;
    }


   
    public void UpdateAggro(int damage = 0, NetworkInstanceId player_id = new NetworkInstanceId(),bool account_attack_dist = true)
    {
        try
        {
            if (damage != 0)
            {
                float dist_multiplier = 1;
                Transform playertr = NetworkServer.FindLocalObject(player_id).transform;
                if (account_attack_dist)
                {
                    /*The closer the player is to the enemy the more threat generated from
                     the respective attack done by the player*/
                    float dist_ratio = enemy_attack_detection.radius /
                        Vector3.Distance(Target.transform.position, ptr.position);
                    /*The distance only has 25% bearing on the threat,however.*/
                    dist_multiplier = .75f + .25f * (dist_ratio);
                }
                /*If the player is not currently on the enemy's hatelist...
                 (netIds are used as ValueGroup indeces)*/
                if (!Array.Exists(HateList, delegate(ValueGroup g)
                {
                    return (g.index == (int)(player_id.Value));
                }))
                {
                    /*Find the first empty slot to store threat info 
                     in*/
                    int index = Array.FindIndex(HateList, delegate(ValueGroup g)
                    {
                        return (g.value == -1);
                    });
                    HateList[index] = new ValueGroup((int)player_id.Value, dist_multiplier * (float)damage);
                }
                else
                {
                    /*If the player is in the aggro list,then simply add to the threat 
                     data stored into it*/
                    int index = Array.FindIndex(HateList, delegate(ValueGroup g)
                    {
                        return (g.index == (int)player_id.Value);
                    });
                    HateList[index].value += (float)damage * dist_multiplier;
                }
            }
            /*There needs to be a .2 times more threat to move up one place.
             This is to prevent rapid switching of targets constantly w/o huge 
             damage changes.*/
            
            Array.Sort(HateList, delegate(ValueGroup lhs, ValueGroup rhs)
            {
                if (lhs.value == -1)
                {
                    return 0;
                }
                else if (lhs.value * .8f > rhs.value)
                {
                    return -1;
                }
                else if (lhs.value * 1.2f <  rhs.value)
                {
                    return 1;
                }       
                else
                {
                    return 0;
                }
            });
            if (HateList[0].value != -1)
            {
                /*Assign target to one with most threat.The Gameobject's
                 existence is checked in event that a player disconnects.*/
                GameObject g = NetworkServer.FindLocalObject(new NetworkInstanceId((uint)HateList[0].index));
                if (g == null)
                {
                    RemoveAggro(new NetworkInstanceId((uint)HateList[0].index));
                }
                else
                {

                    Target = g.GetComponent<HealthDefence>();
                }
            }
            else
            {
                Target = null;
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("AggroException!");
            int index = 
            Array.FindIndex(HateList, delegate(ValueGroup g)
            {
                return (g.index == (int)(player_id.Value));
            });
            if(index != -1)
            {
                Target = null;
                HateList[index] = new ValueGroup(-1,-1);
                UpdateAggro();
            }
        }


    }

    /*For removing info about captured spawn points or killed players
     from the HateList*/
    void RemoveAggro(NetworkInstanceId ID)
    {
        int index = Array.FindIndex(HateList, delegate(ValueGroup v)
        {
            return (v.index == (int)(ID.Value));
        });
        if (index != -1)
        {
            HateList[index] = new ValueGroup(-1, -1);
            UpdateAggro();
        }
    }

    void ClearHateList()
    {
        for (int i = 0; i < 20; i++)
        {
            HateList[i] = new ValueGroup(-1, -1);
        }
        State.ResetHateList();
    }


       

   Vector3 Charge()
   {
       try
       {
           Vector3 dir = (Target.transform.position - ptr.transform.position);
           if (target_focus && dir.magnitude > minimal_distance)
           {
               ptr.LookAt(Target.transform);
               dir = Vector3.Normalize(dir);
               return dir;
           }
           else
           {
               return Vector3.zero;
           }
       }
       catch (System.Exception e)
       {
           return Vector3.zero;
       }
   }

   Vector3 Intercept()
   {
       try
       {
               if (target_focus)
               {
                   Vector3 dif = (Target.transform.position - ptr.transform.position);
                   ptr.LookAt(Target.transform);
                   if (Target.type != HealthDefence.Type.Spawn_Point)
                   {
                       Vector3 proj = Target.GetComponent<Rigidbody>().velocity;
                       if (proj.magnitude > 1)
                       {
                           Vector3 dir = Vector3.Project(dif, proj);
                           return dir;
                       }
                       else if (dif.magnitude < minimal_distance)
                       {
                           return Vector3.zero;
                       }
                       else
                       {
                           return dif;
                       }
                   }
                   else
                   {
                       if (dif.magnitude < minimal_distance)
                       {
                           return Vector3.zero;
                       }
                       else
                       {
                           return dif;
                       }
                   }
               }
               else
               {
                   return Vector3.zero;
               }
           }
       
       catch (System.Exception e)
       {
           return Vector3.zero;
       }
   }

   Vector3 AvoidConfrontation()
   {
       return Intercept() * -1;
   }

   Vector3 MaintainDistance()
   {
       try
       {
           if (target_focus)
           {
               Vector3 dir = (Target.transform.position - ptr.transform.position);
               ptr.LookAt(Target.transform);
               if (dir.magnitude < minimal_distance * 1.5f)
               {
                   return dir * -1;
               }
               else if (dir.magnitude > minimal_distance * 2f)
               {
                   return dir.normalized;
               }
               else
               {
                   return Vector3.zero;
               }
           }
           else
           {
               return Vector3.zero;
           }
       }
       catch (System.Exception e)
       {
           return Vector3.zero;
       }
   }

   Vector3 Wait()
   {
       return Vector3.zero;
   }


   IEnumerator DetermineEvasion()
   {
       yield return new WaitForSeconds(reaction_delay);
       if (trig)
       {
           try
           {
           if (!Shield.GetComponent<HealthDefence>().regeneration && !trig.GetComponent<BulletScript>().can_pierce && !bullet_colliders.Exists(delegate(Collider col)
           {
               if (col)
               {
                   return (col.GetComponent<Rigidbody>().velocity.magnitude > (col.gameObject.transform.position - ptr.position).magnitude &&
                       (Math.Abs(Vector3.Angle(col.gameObject.transform.position, trig.gameObject.transform.position)) > 30));
               }
               else
               {
                   return false;
               }
           }))
           {
               StartCoroutine(Block());
           }
           else
           {
               StartCoroutine(EvasionFuncs[evasion_func_index](this));
           }
           }
           catch(System.Exception e)
           {
               trig = null;
               Debug.Log(e);
           }
           
       }
   }

   IEnumerator Block()
   {
       
     
           while (trig && Math.Abs(trig.transform.position.magnitude - ptr.position.magnitude) > .5f)
           {
               yield return new WaitForEndOfFrame();
           }
           if (trig)
           {
               blocking = true;
               target_focus = false;
               ptr.LookAt(trig.gameObject.transform);
               StartShieldBlocking();
               while (trig)
               {
                   yield return new WaitForEndOfFrame();
               }
               yield return new WaitForSeconds(.5f);
               target_focus = true;
               EndShieldBlocking();
               blocking = false;
           }
   }

   IEnumerator SideStep()
   {
       yield return new WaitForSeconds(reaction_delay / 2);
       int sign = rand.Next(1);
       while (trig)
       {
           yield return new WaitForFixedUpdate();
           try
           {
               Vector3 dif = trig.gameObject.transform.position - ptr.position;
               if (sign == 0)
               {
                   move_dir += Quaternion.AngleAxis(90, Vector3.up) * dif.normalized;
               }
               else
               {
                   move_dir += Quaternion.AngleAxis(-90, Vector3.up) * dif.normalized;
               }
           }
           catch (System.Exception e)
           {
               Debug.Log(e);
           }
       }
   }
    

    
   
}

