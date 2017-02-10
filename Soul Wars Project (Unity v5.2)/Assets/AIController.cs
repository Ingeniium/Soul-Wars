using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class AIController : GenericController {
    private Rigidbody prb;
    public Transform ptr;
    public GameObject Shield;
    public GameObject Gun;
    private GameObject Target;
    private bool target_focus = true;
    private Collider trig;
    private List<Collider> obstacles = new List<Collider>();
    private float next_dodge = 0;
    public float dodge_delay;
    public float dodge_cooldown;
    private Vector3 vec;
    public float reaction_delay;    
    [HideInInspector]
    public Transform gtr;
    public Collider enemy_attack_detection;
    public Gun gun;
    public float minimal_distance = 1f;
    private bool guarding = false;
    private static UniversalCommunicator TeamController = new UniversalCommunicator();
    private FunctionChooser attack_func_chances = new FunctionChooser();

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

    private int attack_func_index = 0;
    private int movement_func_index = 0;
    private static System.Random rand = new System.Random();
    private ObjectiveState State;
    private bool ally_in_range = false;

    void Awake()
    {
        
        enemy_attack_detection = GetComponent<Collider>();
    }

    void Start()
    {
        TeamController.Units.Add(this);
        if (!TeamController.set)
        {
            TeamController.Start(new List<GroupCommunicator>()
            {
                new Conquer()
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
        if (!State.standby)
        {
            prb.AddForce(Vector3.up * 10);
            prb.AddForce(MovementFuncs[movement_func_index](this));
            if (!guarding && AttackFuncs[attack_func_index](this))
            {
                gun.Shoot();
            }
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.layer == 9)
        {
            prb.AddForce(AvoidObstacles(col), ForceMode.Impulse);
            ally_in_range = true;
            if (State.RespondtoAllyUnit != null)
            {
                State.immediate_responding = true;
                State.RespondtoAllyUnit(col);
            }
        }
        else
        {
            trig = col;
            StartCoroutine(Evasion());
            enemy_attack_detection.enabled = false;
            enemy_attack_detection.isTrigger = false;
        }
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
           if ((State.immediate_responding && State.EndAllyUnitResponse()) || State.ShouldFindNewTarget())
           {
              State.immediate_responding = false;
              State.SetTarget();
              return Vector3.zero;
           }
           else if (target_focus && dir.magnitude > minimal_distance)
           {
               ptr.LookAt(Target.transform);
               dir = Vector3.Normalize(dir);
               return (dir * 10);
           }
           else
           {
               return Vector3.zero;
           }
       }
       catch (System.Exception e)
       {
           State.SetTarget();
           return Vector3.zero;
       }
   }

   Vector3 Intercept()
   {
       try
       {

               if ((State.immediate_responding && State.EndAllyUnitResponse()) || State.ShouldFindNewTarget())
               {
                   State.immediate_responding = false;
                   State.SetTarget();
                   return Vector3.zero;
               }
               else if (target_focus)
               {
                   Vector3 dif = (Target.transform.position - ptr.transform.position);
                   Vector3 proj = Target.GetComponent<Rigidbody>().velocity;
                   ptr.LookAt(Target.transform);
                   if (proj.magnitude > 1)
                   {
                       Vector3 dir = Vector3.Project(dif, proj);
                       return dir * 10;
                   }
                   else if (dif.magnitude < minimal_distance)
                   {
                       return Vector3.zero;
                   }
                   else
                   {
                       return dif.normalized * 10;
                   }
               }
               else
               {
                   return Vector3.zero;
               }
           }
       
       catch (System.Exception e)
       {
           State.SetTarget();
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
           if ((State.immediate_responding && State.EndAllyUnitResponse()) || State.ShouldFindNewTarget())
           {
               State.immediate_responding = false;
               State.SetTarget();
               return Vector3.zero;
           }
           else
           {
               Vector3 dir = Target.transform.position - ptr.position;
               if (target_focus && dir.magnitude < gun.range)
               {
                   ptr.LookAt(Target.transform);
                   return dir.normalized * 10;
               }
               else
               {
                   return Vector3.zero;
               }
           }
       }
       catch (System.Exception e)
       {
           State.SetTarget();
           return Vector3.zero;
       }
   }

   Vector3 Wait()
   {
       return Vector3.zero;
   }

   Vector3 AvoidObstacles(Collider col)
   {      
      Vector3  dir = transform.position - col.gameObject.transform.position;
      dir = Quaternion.AngleAxis(90, ptr.up) * dir;
      float num = 5 - dir.magnitude;
      dir = dir.normalized * num;
      return dir;
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
           State.SetTarget();
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
           State.SetTarget();
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
           State.SetTarget();
           return false;
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

