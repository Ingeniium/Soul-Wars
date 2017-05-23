using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

public class BulletScript : NetworkBehaviour {
	[SyncVar] public int upper_bound_damage;
    [SyncVar] public int lower_bound_damage;
    static System.Random rand = new System.Random();
	public GameObject home;
    public GameObject homer;
	[SyncVar] public float home_radius;
    [SyncVar] public float home_speed;
    [SyncVar] public double crit_chance;
    [SyncVar] public float knockback_power;
    [SyncVar] public double chill_strength;
    [SyncVar] public double burn_strength;
    public Gun gun_reference;
    [SyncVar] public bool has_collided = false;
    [SyncVar] public bool legit_target = false;
    public bool homes = false;
    [SyncVar] public int coroutines_running = 0;
    [SyncVar] public bool can_pierce;
    public HealthDefence Target;
	// Use this for initialization

	void Start () 
    {
        StartCoroutine(WaitForGunReference());
	}

    IEnumerator WaitForGunReference()
    {
        while (!gun_reference)
        {
            yield return new WaitForEndOfFrame();
        }
        homer = transform.GetChild(0).gameObject;
        homer.GetComponent<SphereCollider>().radius = home_radius;
        homer.layer = gun_reference.home_layer;
        GetComponent<Collider>().isTrigger = can_pierce;
        HomingScript script = homer.GetComponent<HomingScript>();
        script.home_speed = home_speed;
        script.prb = GetComponent<Rigidbody>();
        script.ptr = transform;
        if (!homes)
        {
            homer.GetComponent<HomingScript>().enabled = true;
        }
        StartCoroutine(WaitForNetworkDestruction(gameObject));
    }

    IEnumerator WaitForNetworkDestruction(GameObject g,float num = 3f)
    {
       
        yield return new WaitForSeconds(num);
        if (isServer)
        {
            NetworkServer.Destroy(g);
        }
    }

    IEnumerator Pierce(Collision hit)
    {
        Collider first = GetComponent<Collider>();
        Collider second = hit.gameObject.GetComponent<Collider>();
        Collider third = null;
        if (homer)
        {
            third = homer.GetComponent<Collider>();
            Physics.IgnoreCollision(third, second);
        }
        Physics.IgnoreCollision(first, second);
        yield return new WaitForEndOfFrame();
        Physics.IgnoreCollision(first, second, false);
        if (homer)
        {
            Physics.IgnoreCollision(third, second, false);
        }
    }

    IEnumerator Pierce(Collider hit)
    {
        Collider first = GetComponent<Collider>();
        Collider second = hit;
        Collider third = null;
        if (homer)
        {
            third = homer.GetComponent<Collider>();
            Physics.IgnoreCollision(third, second);
        }
        Physics.IgnoreCollision(first, second);
        yield return new WaitForSeconds(1);
        if (second.enabled)
        {
            Physics.IgnoreCollision(first, second, false);
            if (homer)
            {
                Physics.IgnoreCollision(third, second, false);
            }
        }
    }

   [ServerCallback]
    void OnCollisionEnter(Collision hit)
    {
        try
        {
            StartCoroutine(Damage(hit));
        }
        catch (System.NullReferenceException e)
        {
            StopAllCoroutines();
            NetworkServer.Destroy(gameObject);
            //Always destroy the object upon any detectable impact upon an exception           
        }
    }

   [ServerCallback]
   void OnTriggerEnter(Collider hit)
   {
       try
       {
           if (can_pierce)//Check put there b/c otherwise,homing detection would call it
           {
               StartCoroutine(Damage(null, hit));
           }
       }
       catch (System.NullReferenceException e)
       {
           StopAllCoroutines();
           NetworkServer.Destroy(gameObject);
           //Always destroy the object upon any detectable impact upon an exception           
       }
   }

     
    IEnumerator Damage(Collision hit,Collider col = null)
    {
        if (col == null)
        {
            Target = hit.gameObject.GetComponent<HealthDefence>();
        }
        else
        {
            Target = col.gameObject.GetComponent<HealthDefence>();
        }
        /*If the target still exist and doesn't even have this script,
         Don't bother executing the rest of the code.As for the exception,it is there
         in the event the object "dies" midway execution,presumably from another bullet.*/
        /*If a spawn point is hit by enemy,just run on client.*/
        /*If an enemy is hit(for enemy guns have no client user set),test whether the code is
         running on the same client as the one who shot the bullet(for number UI to show up)
         *If a player is hit,run the code only on whoever got hit*/
        if (Target != null)
        {
            if (hit != null)
            {
                StartCoroutine(Pierce(hit));
            }
            else
            {
                StartCoroutine(Pierce(col));
            }
            legit_target = true;
            has_collided = true;
            //Wait until all coroutines operating on the bullet finish
            while (coroutines_running > 0)
            {
                yield return new WaitForFixedUpdate();
            }
            int damage = rand.Next(lower_bound_damage, upper_bound_damage);
            int d = (damage - Target.defence);
            bool crit = false;
            if ((crit_chance - Target.crit_resistance) >= rand.NextDouble() + .001)//bullets with a crit chance of 0 shouldn't be able to land a crit
            {
                crit = true;
                d *= 3;
            }
            Target.StartCoroutine(Target.DetermineChill(chill_strength));
            Target.StartCoroutine(Target.DetermineBurn(burn_strength,d));
            if (crit)
            {
                Target.RpcDisplayHPChange(new Color(114, 0, 198), d);//Violet
            }
            else
            {
                Target.RpcDisplayHPChange(Color.red, d);
            }
            //RpcDisplayHPChange(d, crit, (int)Target.type);

            if (Target.has_exp)
            {
                gun_reference.experience += d * Target.exp_rate;
            }
            AIController AI = Target.Controller as AIController;
            if (AI != null)
            {
                AI.UpdateAggro(d, gun_reference.client_user.netId);
            }
            Target.HP -= d;
            if (!can_pierce)
            {
                NetworkServer.Destroy(gameObject);
            }
        }
        /*If target is null or hit enemy detetion*/
        else if ((col && !col.isTrigger) || (hit != null && !hit.gameObject.GetComponent<Collider>().isTrigger))
        {
            /* Before destruction,Stop all coroutines(the gun_abilities operating on this instance)
             to prevent exceptions from those coroutines*/
           
            StopAllCoroutines();
            NetworkServer.Destroy(gameObject);
        }
    }


       

   
}


