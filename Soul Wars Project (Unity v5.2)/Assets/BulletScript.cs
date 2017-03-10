using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

public class BulletScript : NetworkBehaviour {
	public int upper_bound_damage;
    public int lower_bound_damage;
    static System.Random rand = new System.Random();
	public GameObject home;
    public GameObject homer;
	public float home_radius;
    public float home_speed;
    public double crit_chance;
    public float knockback_power;
    public Gun gun_reference;
    public bool has_collided = false;
    public bool legit_target = false;
    public bool homes = false;
    public int coroutines_running = 0;
    private HealthDefence Target;
    public GameObject health_change_canvas;
    private GameObject health_change_show;
	// Use this for initialization

	void Start () 
    {
        /*homer = Instantiate(home, transform.position, Quaternion.identity) as GameObject;
        homer.transform.parent = gameObject.transform;
        /*Pass values to homing device,even if homing is currently disabled for midway homing toggle
        homer.GetComponent<SphereCollider>().radius = home_radius;
        homer.GetComponent<HomingScript>().home_speed = home_speed;
        if (!homes)
        {
            homer.GetComponent<HomingScript>().enabled = true;
        }*/
        Item.Player.CmdSpawnHomingDevice(transform.position, transform.rotation);
        Destroy(gameObject, 3.0f);
	}

    public void InitHomingDevice(NetworkInstanceId ID)
    {
        homer = ClientScene.FindLocalObject(ID);
        homer.transform.parent = transform;
        homer.GetComponent<SphereCollider>().radius = home_radius;
         homer.GetComponent<HomingScript>().home_speed = home_speed;
        if (!homes)
        {
            homer.GetComponent<HomingScript>().enabled = true;
        }
    }

	void OnCollisionEnter (Collision hit) 
    {
        try
        {
            StartCoroutine(Damage(hit));
        }
        catch (System.NullReferenceException e)
        {
            if (health_change_show)
            {
                Destroy(health_change_show.gameObject);
            }
            StopAllCoroutines();
            Destroy(gameObject);//Always destroy the object upon any detectable impact upon an exception           
        }
        
    }

    IEnumerator Damage( Collision hit)
    {
        Target = hit.gameObject.GetComponent<HealthDefence>();
        /*If the target still exist and doesn't even have this script,
         Don't bother executing the rest of the code.As for the exception,it is there
         in the event the object "dies" midway execution,presumably from another bullet.*/
        has_collided = true;
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
                //Note that whether the bullet crit or not changes the color of the damage numbers and block indication
                health_change_show = Instantiate(health_change_canvas, hit.gameObject.transform.position + new Vector3(0, 0, 1), Quaternion.Euler(90, 0, 0)) as GameObject;
                if (Target.type != HealthDefence.Type.Shield)
                {
                    health_change_show.GetComponentInChildren<Text>().text = "-" + d;
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
                        Target.StartCoroutine(Stun(Target, knockback));
                    }
                }
                Target.HP -= d;           
                Destroy(health_change_show, 1f);
                Destroy(gameObject);
            

        }
        else//if target is null
        {
            /*Before destruction,Stop all coroutines(the gun_abilities operating on this instance)
             to prevent exceptions from those coroutines*/
            StopAllCoroutines();
            Destroy(gameObject);//Destroy object immediately if null
        }
    }

    IEnumerator Stun(HealthDefence Target,float knockback)
    {
        Target.Controller.enabled = false;
        yield return new WaitForSeconds(knockback / 10);
        Target.Controller.enabled = true;
        Target.standing_power -= knockback;
    }

    

   
}


