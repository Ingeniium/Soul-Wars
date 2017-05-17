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
    public Gun gun_reference;
    [SyncVar] public bool has_collided = false;
    [SyncVar] public bool legit_target = false;
    public bool homes = false;
    [SyncVar] public int coroutines_running = 0;
    private HealthDefence Target;
    public GameObject health_change_canvas;
    public GameObject health_change_show;
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

   [ServerCallback]
    void OnCollisionEnter(Collision hit)
    {
        try
        {
            //RpcHit(hit.gameObject);
            StartCoroutine(Damage(hit));
        }
        catch (System.NullReferenceException e)
        {
            RpcStopAllCoroutines();
            NetworkServer.Destroy(gameObject);
            //Always destroy the object upon any detectable impact upon an exception           
        }
    }

    [ClientRpc]
    void RpcHit(GameObject g)
    {
       // StartCoroutine(Damage(g.GetComponent<Collider>()));
    }

    [ClientRpc]
    void RpcStopAllCoroutines()
    {
        if (health_change_show)
        {
            Destroy(health_change_show.gameObject);
        }
        StopAllCoroutines();
    }

     [ServerCallback]
    IEnumerator Damage(Collision hit)
    {
        Target = hit.gameObject.GetComponent<HealthDefence>();
        /*If the target still exist and doesn't even have this script,
         Don't bother executing the rest of the code.As for the exception,it is there
         in the event the object "dies" midway execution,presumably from another bullet.*/
       // has_collided = true;
        /*If a spawn point is hit by enemy,just run on client.*/
        /*If an enemy is hit(for enemy guns have no client user set),test whether the code is
         running on the same client as the one who shot the bullet(for number UI to show up)
         *If a player is hit,run the code only on whoever got hit*/
        if (Target != null)
        {
            legit_target = true;
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
                
                RpcDisplayHPChange(d,crit,(int)Target.type);

                if (Target.has_exp)
                {
                    gun_reference.experience += d * Target.exp_rate;
                }
                if (Target.type == HealthDefence.Type.Unit && Target.HP - d > 0)
                {
                    float knockback = knockback_power - Target.knockback_resistance;
                    if (knockback > 0)
                    {
                        Target.rb.AddForce(new Vector3(transform.forward.x, 0, transform.forward.z) * knockback, ForceMode.Impulse);
                        //Target.StartCoroutine(Stun(Target, knockback));
                    }
                }
                AIController AI = Target.Controller as AIController;
                if (AI != null)
                {
                    //AI.UpdateAggro(d, gun_reference.client_user.netId);
                }
                Target.HP -= d;
                StartCoroutine(WaitForNetworkDestruction(gameObject,.15f));
            

        }
        else//if target is null
        {
           /* Before destruction,Stop all coroutines(the gun_abilities operating on this instance)
             to prevent exceptions from those coroutines*/
            RpcStopAllCoroutines();
            StartCoroutine(WaitForNetworkDestruction(gameObject, .15f));
        }
    }
    /*
    IEnumerator Damage(Collider hit)
    {
        Target = hit.gameObject.GetComponent<HealthDefence>();
        /*If the target still exist and doesn't even have this script,
         Don't bother executing the rest of the code.As for the exception,it is there
         in the event the object "dies" midway execution,presumably from another bullet.
        has_collided = true;
        /*If a spawn point is hit by enemy,just run on client.
        /*If an enemy is hit(for enemy guns have no client user set),test whether the code is
         running on the same client as the one who shot the bullet(for number UI to show up)
         *If a player is hit,run the code only on whoever got hit
        if (Target != null)
        {
            legit_target = true;
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

            DisplayHPChange(d, crit, (int)Target.type);

            if (Target.has_exp)
            {
                gun_reference.experience += d * Target.exp_rate;
            }
            if (Target.type == HealthDefence.Type.Unit && Target.HP - d > 0)
            {
                float knockback = knockback_power - Target.knockback_resistance;
                if (knockback > 0)
                {
                    Target.rb.AddForce(new Vector3(transform.forward.x, 0, transform.forward.z) * knockback, ForceMode.Impulse);
                    //Target.StartCoroutine(Stun(Target, knockback));
                }
            }
            AIController AI = Target.Controller as AIController;
            if (AI != null)
            {
                AI.UpdateAggro(d, gun_reference.client_user.netId);
            }
            Target.HP -= d;
            StartCoroutine(WaitForNetworkDestruction(gameObject, .15f));


        }
        else//if target is null
        {
            /*Before destruction,Stop all coroutines(the gun_abilities operating on this instance)
             to prevent exceptions from those coroutines
            RpcStopAllCoroutines();
            StartCoroutine(WaitForNetworkDestruction(gameObject, .15f));
        }
    } */

    
    void RpcDisplayHPChange(int num,bool crit,int type)
    {
        health_change_show = Instantiate(health_change_canvas, gameObject.transform.position + new Vector3(0, 0, 1), Quaternion.Euler(90, 0, 0)) as GameObject;
        if (type != 1)
        {
            health_change_show.GetComponentInChildren<Text>().text = "-" + num;
            if (crit)
            {
                health_change_show.GetComponentInChildren<Text>().color = new Color(114, 0, 198);//A violet like color
            }
            else
            {
                health_change_show.GetComponentInChildren<Text>().color = Color.red;
            }
        }
        else
        {
            health_change_show.GetComponentInChildren<Text>().text = "*BLOCKED*";
            if (crit)
            {
                health_change_show.GetComponentInChildren<Text>().color = Color.black;
            }
            else
            {
                health_change_show.GetComponentInChildren<Text>().color = Color.grey;
            }
        }
       
        Destroy(health_change_show, 1f);
    }
    

    IEnumerator Stun(HealthDefence Target,float knockback)
    {
        Target.Controller.enabled = false;
        yield return new WaitForSeconds(knockback / 10);
        Target.Controller.enabled = true;
        Target.standing_power -= knockback;
    }

    

   
}


