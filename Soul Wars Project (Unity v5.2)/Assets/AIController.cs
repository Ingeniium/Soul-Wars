using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class AIController : GenericController {
    private Rigidbody prb;
    public Transform ptr;
    private HealthDefence Target;
    private bool target_focus = true;
    private Collider trig;
    private List<Collider> bullet_colliders = new List<Collider>();
    private float next_dodge = 0;
    public float dodge_delay;
    public float dodge_cooldown;
    private Vector3 vec;
    public float reaction_delay;    
    [HideInInspector]
    public Transform gtr;
    public SphereCollider enemy_attack_detection;
    public Gun gun;
    public float minimal_distance = 1f;
    private bool guarding = false;
    private static UniversalCommunicator TeamController = new UniversalCommunicator();
    private FunctionChooser attack_func_chances = new FunctionChooser();
    private Vector3 move_dir;

    private static Dictionary<int, Func<AIController, Vector3>> MovementFuncs = new Dictionary<int, Func<AIController, Vector3>>()
    {
        {0,Charge},
        {1,MaintainDistance},
        {2,Intercept},
        {3,AvoidConfrontation}           
    };
    private static Dictionary<int, Func<AIController, bool>> AttackFuncs = new Dictionary<int, Func<AIController, bool>>()
    {
        {0,WildyFire},
        {1,FireWhenInRange},
        {2,GuardFire},
        {3,HaltFire}
    };
    private static Dictionary<int, Func<AIController,IEnumerator>> EvasionFuncs = new Dictionary<int, Func<AIController,IEnumerator>>()
    {
        {0,Block},
        {1,SideStep}
    };

    private ValueGroup[] HateList = new ValueGroup[20]
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
    
    private int attack_func_index = 0;
    private int movement_func_index = 0;
    private int evasion_func_index = 1;
    private static System.Random rand = new System.Random();
    private ObjectiveState State;
    private bool ally_in_range = false;

    void Awake()
    {
        
        enemy_attack_detection = GetComponent<SphereCollider>();
        GetComponentInParent<HealthDefence>().Controller = this;
    }

    void Start()
    {
        TeamController.Units.Add(this);
        if (!TeamController.set)
        {
            TeamController.Start(new List<GroupCommunicator>()
            {
                //new Conquer()
            });
            TeamController.set = true;
        }
        prb = GetComponentInParent<Rigidbody>();
        //GetCOmponent In parent apparently isn't working for transform
        gtr = Gun.GetComponent<Transform>();
        gun = Gun.GetComponent<Gun>();
    }

    void FixedUpdate()
    {
        
            State.AffirmTarget(Target);
            prb.AddForce(Vector3.up * 10);
            move_dir = MovementFuncs[movement_func_index](this);
            prb.AddForce(move_dir.normalized * 10);
            if (!guarding && AttackFuncs[attack_func_index](this))
            {
                gun.Shoot();
            }
        
    }

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
            EvasionFuncs[evasion_func_index](this);
        }
    }

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
                distances.RemoveAt(i);
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

    static bool WildyFire(AIController AI)
    {
        return AI.WildlyFire();
    }

    static bool FireWhenInRange(AIController AI)
    {
        return AI.FireWhenInRange();
    }

    static bool GuardFire(AIController AI)
    {
        return AI.GuardFire();
    }

    static bool HaltFire(AIController AI)
    {
        return AI.HaltFire();
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

    private struct ValueGroup//Unity doesn't support Tuple
    {
        public int index;
        public float value;
        public ValueGroup(int i, float v)
        {
            index = i;
            value = v;
        }
    }

    public void UpdateAggro(int damage = 0, NetworkInstanceId player_id = new NetworkInstanceId(),bool account_attack_dist = true)
    {
        try
        {
            if (damage != 0)
            {
                float dist_multiplier = 1;
                Transform playertr = ClientScene.FindLocalObject(player_id).transform;
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
                if (lhs.value * 1.2f > rhs.value)
                {
                    return -1;
                }
                else if (lhs.value < .8f * rhs.value)
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
                GameObject g = ClientScene.FindLocalObject(new NetworkInstanceId((uint)HateList[0].index));
                if (g == null)
                {
                    RemoveAggro(new NetworkInstanceId((uint)HateList[0].index));
                }
                else
                {
                    Target = g.GetComponent<HealthDefence>();
                }
            }
        }
        catch (System.Exception e)
        {
            int index = 
            Array.FindIndex(HateList, delegate(ValueGroup g)
            {
                return (g.index == (int)(player_id.Value));
            });
            if(index != -1)
            {
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
        HateList[index] = new ValueGroup(-1, -1);
        UpdateAggro();
    }

    void ClearHateList()
    {
        for (int i = 0; i < 20; i++)
        {
            HateList[i] = new ValueGroup(-1, -1);
        }
        State.ResetHateList();
    }


    private class FunctionChooser
    {
        public FunctionChance[] possibilities = new FunctionChance[4]
        {
            new FunctionChance(0,.25),
            new FunctionChance(.25,.50),
            new FunctionChance(.50,.75),
            new FunctionChance(.75,1)
        };

        public int Roll(System.Random r)
        {
            double num = r.NextDouble();
            for (int i = 0; i < possibilities.Length; i++)
            {
                if (num < possibilities[i].upper_bound && num > possibilities[i].lower_bound)
                {
                    return i;
                }
            }
            return 0;
        }

        public void ModifyChances(int index, double num)
        {
            List<ValueGroup> values = new List<ValueGroup>();
            for (int i = 0; i < possibilities.Length; i++)
            {
                values.Add(new ValueGroup(i, possibilities[i].success_rate));
            }
            values.Sort(delegate(ValueGroup lhs, ValueGroup rhs)
            {
                if (lhs.value < rhs.value)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });
            if (values[0].index < index)
            {
                possibilities[index].lower_bound -= num;
                for (int i = index; i > values[0].index; i--)
                {
                    possibilities[i].upper_bound -= num;
                    possibilities[i].lower_bound -= num;
                }
                possibilities[values[0].index].upper_bound -= num;
            }
            else if(values[0].index > index)
            {
                possibilities[index].lower_bound += num;
                for (int i = values[0].index; i > index; i--)
                {
                    possibilities[i].upper_bound += num;
                    possibilities[i].lower_bound += num;
                }
                possibilities[values[0].index].lower_bound -= num;
            }
        }
    }

    private struct FunctionChance
    {
        public double lower_bound;
        public double upper_bound;
        public float success_rate;

        public FunctionChance(double l, double u)
        {
            lower_bound = l;
            upper_bound = u;
            success_rate = 1;
        }
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
                   Vector3 proj = Target.GetComponent<Rigidbody>().velocity;
                   ptr.LookAt(Target.transform);
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
                       return dif.normalized;
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
          
               Vector3 dir = Target.transform.position - ptr.position;
               if (target_focus && dir.magnitude < gun.range)
               {
                   ptr.LookAt(Target.transform);
                   return dir.normalized;
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

   

   bool WildlyFire()
   {
       return gun.HasReloaded();
   }

   bool FireWhenInRange()
   {
       try
       {
           if (gun.HasReloaded() && gun.range > Vector3.Distance(ptr.position, Target.transform.position))
           {
               return true;
           }
           else
           {
               return false;
           }
       }
       catch (System.Exception e)
       {
           return false;
       }
   }

   bool GuardFire()
   {
       try
       {
           if (gun.HasReloaded() && Vector3.Distance(ptr.position, Target.transform.position) < gun.range - (1.5 * minimal_distance))
           {
               return true;
           }
           else
           {
               return false;
           }
       }
       catch (System.Exception e)
       {
           return false;
       }
   }

   bool HaltFire()
   {
       try
       {
           if (gun.HasReloaded())
           {
               Vector3 proj = Target.GetComponent<Rigidbody>().velocity;
               if (proj.magnitude > 1)
               {
                   Vector3 dif = (Target.transform.position - ptr.transform.position);
                   Vector3 dir = Vector3.Project(dif, proj);
                   ptr.LookAt(dir);
               }
               return true;               
           }
           else
           {
               return false;
           }
       }
       catch (System.Exception e)
       {
           return false;
       }
   }

   IEnumerator Block()
   {
       yield return new WaitForSeconds(reaction_delay);
       if (trig)
       {
           target_focus = false; 
           ptr.LookAt(trig.gameObject.transform);        
           StartShieldBlocking();
           yield return new WaitForSeconds(reaction_delay / 2);
           target_focus = true;
           EndShieldBlocking();
       }
   }

   IEnumerator SideStep()
   {
       yield return new WaitForSeconds(reaction_delay / 2);
       int sign = rand.Next(1);
       while (trig)
       {
           yield return new WaitForFixedUpdate();
           Vector3 dif =  trig.gameObject.transform.position - ptr.position;
           if (sign == 0)
           {
               move_dir += Quaternion.AngleAxis(90, Vector3.up) * dif.normalized;
           }
           else
           {
               move_dir += Quaternion.AngleAxis(-90, Vector3.up) * dif.normalized;
           }
       }
   }

    IEnumerator Evasion()
    {
        yield return new WaitForSeconds(reaction_delay);
      if(trig != null)
      {
        if((next_dodge > Time.time || Vector3.Distance(trig.gameObject.transform.position,ptr.position) < 2) && Shield.GetComponent<HealthDefence>().regeneration == false)
        {
            target_focus = false;
            int turn = 0;
            while(turn != 90 && trig != null)
            {
                ptr.LookAt(trig.transform);
                Shield.transform.rotation *= Quaternion.AngleAxis(-45,Vector3.up);
                turn += 45;
                yield return new WaitForEndOfFrame();
            }
            Shield.transform.localPosition = new Vector3(.09f, .37f, .80f);
            gtr.localPosition = new Vector3(.87f, 0, -.071f);
            yield return new WaitForSeconds(.5f);
            target_focus = true;
            while (turn != 0)
            {
                Shield.transform.rotation *= Quaternion.AngleAxis(45,Vector3.up);
                turn -= 45;
                yield return new WaitForEndOfFrame();
            }
            Shield.transform.localPosition = new Vector3(.87f, .37f, -.071f);
            gtr.localPosition = new Vector3(.09f, 0, .80f);
        }
        else if(next_dodge < Time.time)
        {
                next_dodge = Time.time + dodge_cooldown;
                yield return new WaitForSeconds(dodge_delay);
                if (trig != null)
                {
                    vec = Quaternion.AngleAxis(90, trig.gameObject.transform.up) * trig.gameObject.transform.forward;
                }
                prb.AddForce(vec * 10, ForceMode.Impulse);
        }

     }
      enemy_attack_detection.enabled = true;
      enemy_attack_detection.isTrigger = true;
    }

    
   
}

