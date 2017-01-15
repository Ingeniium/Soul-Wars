using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class AIController : MonoBehaviour {
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
    private objective obj = 0;
    public float minimal_distance = 1f;

    private enum objective
    {
            ATTACK_ALLY_UNIT = 0,
            ATTACK_ALLY_BASE = 1,
            GUARD_LOCATION = 2
    }

    void SetTarget()
    {
        switch(obj)
        {
            case objective.ATTACK_ALLY_UNIT :
                if(Item.Player != null)
                {
                    Target = Item.Player.gameObject;
                }
                else
                {
                    obj = objective.ATTACK_ALLY_BASE;
                    SetTarget();
                }
                break;
            case objective.ATTACK_ALLY_BASE :
                break;
            case objective.GUARD_LOCATION :
                break;
        }
    }

	void Start ()
    {
      prb = GetComponentInParent<Rigidbody>();
        //GetCOmponent In parent apparently isn't working for transform
      enemy_attack_detection = GetComponent<Collider>();
      gtr = Gun.GetComponent<Transform>();
      gun = Gun.GetComponent<Gun>();
    }

   void OnTriggerEnter(Collider col)
    {
        /*If there's a player controlled unit or obstacle/wall pieces
         within radius of collider,simply move away,maintaining minimal distance*/
        if (col.gameObject.layer == 9)
        {
            //obstacles.Add(col);
            prb.AddForce(AvoidObstacles(col),ForceMode.Impulse);
        }
        else
        {
            trig = col;
            StartCoroutine(Evasion());
            enemy_attack_detection.enabled = false;
            enemy_attack_detection.isTrigger = false;
        }  
    }

   void OnTriggerExit(Collider col)
   {
       if (col.gameObject.layer == 9)
       {
           obstacles.Remove(col);
       }
   }

   void Update()
   {
       prb.AddForce(Vector3.up * 25);
       if (obstacles.Count > 0)
           {
               Vector3 dir = Vector3.zero;
               foreach (Collider o in obstacles)
               {
                   try
                   {
                       dir += AvoidObstacles(o);
                   }
                   catch (System.Exception e)
                   {
                       dir += Vector3.zero;
                   }
               }
               prb.AddForce(dir);
           }
       prb.AddForce(Intercept());
       FireWhenInRange();
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
               return (dir * 10);
           }
           else
           {
               return Vector3.zero;
           }
       }
       catch (System.Exception e)
       {
           SetTarget();
           return Vector3.zero;
       }
   }

   Vector3 Intercept()
   {
       try
       {
           Vector3 dif = (Target.transform.position - ptr.transform.position);
           if (target_focus)
           {
              
               Vector3 proj = Target.GetComponent<Rigidbody>().velocity;
               ptr.LookAt(Target.transform);
               if (proj.magnitude > 1)
               {
                   Vector3 dir = Vector3.Project(dif, proj); 
                   return dir * 10;
               }
               else if (dif.magnitude > minimal_distance)
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
           SetTarget();
           return Vector3.zero;
       }
   }


   Vector3 AvoidObstacles(Collider col)
   {      
      Vector3  dir = transform.position - col.gameObject.transform.position;
      dir = Quaternion.AngleAxis(90, ptr.up) * dir;
      float num = 5 - dir.magnitude;
      dir = dir.normalized * num;
      return dir;
   }

   void WildlyFire()
   {
       if (gun.next_time < Time.time)
       {
           gun.Shoot();
       }
   }

   void FireWhenInRange()
   {
       try
       {
           if (gun.next_time < Time.time && gun.range <= Vector3.Distance(ptr.position, Target.transform.position))
           {
               gun.Shoot();
           }
       }
       catch (System.Exception e)
       {
           SetTarget();
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

