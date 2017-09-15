using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public partial class AIController : GenericController
{

    public int[] attack_func_indexes = new int[]
   {
        1,
        1
   };

    public enum AttackMode
    {
        WildlyFire = 0,
        FireWhenInRange = 1,
        FireWhenClose = 2,
        HaltFire = 3,
    }

    /*Static Dictionary is set up with static functions that take arguments in order for func indexing to
      be used at runtime without having each Enemy set up their own container.The second argument,Gun, is
      used in order to retrict use of certain guns depending on the situation.*/
    private static Dictionary<int, Func<AIController,Gun, bool>> AttackFuncs = new Dictionary<int, Func<AIController,Gun, bool>>()
    {
        {0,WildyFire},
        {1,FireWhenInRange},
        {2,GuardFire},
        {3,HaltFire}
    };

    static bool WildyFire(AIController AI,Gun gun)
    {
        return AI.WildlyFire(gun);
    }

    static bool FireWhenInRange(AIController AI,Gun gun)
    {
        return AI.FireWhenInRange(gun);
    }

    static bool GuardFire(AIController AI,Gun gun)
    {
        return AI.GuardFire(gun);
    }

    static bool HaltFire(AIController AI,Gun gun)
    {
        return AI.HaltFire(gun);
    }

    bool WillBulletHitObstacle(Gun gun)
    {
        return Physics.Raycast(ptr.position,
            Target.transform.position - gun.transform.position,
            Vector3.Distance(gun.transform.position,Target.transform.position),
            LayerMask.GetMask("Obstacle","Default"));
    }


    bool WildlyFire(Gun gun)
    {
        return gun.HasReloaded(shoot_delay);
    }

    bool FireWhenInRange(Gun gun)
    {
        try
        {
            if (gun.HasReloaded(shoot_delay) && 
                  Math.Abs(
                      Vector3.Distance(ptr.position,Target.transform.position)) <
                      gun.projectile_speed  * 3)
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

    bool GuardFire(Gun gun)
    {
        try
        {
            if (gun.HasReloaded(shoot_delay) && 
                Math.Abs(
                    Vector3.Distance(ptr.position, Target.transform.position)) < 
                gun.projectile_speed * 1.25f )
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

    bool HaltFire(Gun gun)
    {
        try
        {
            if (gun.HasReloaded(shoot_delay))
            {
                Rigidbody rb = Target.GetComponent<Rigidbody>();
                if (rb)
                {
                    Vector3 velocity = rb.velocity;
                    if (velocity.magnitude > 1)
                    {
                        Vector3 dif = (Target.transform.position - ptr.transform.position);
                        Vector3 target_path = Target.transform.position + velocity * (float)rand.NextDouble();
                        ptr.LookAt(target_path);
                    }
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


}


