using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class BulletScript : MonoBehaviour {
	public int damage;
	public GameObject home;
    private GameObject homer;
	public float home_radius;
    public float home_speed;
    public double crit_chance;
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
        if (homes)
        {
            homer = Instantiate(home, transform.position, Quaternion.identity) as GameObject;
            homer.transform.parent = gameObject.transform;
            //Pass values to homing device
            homer.GetComponent<SphereCollider>().radius = home_radius;
            homer.GetComponent<HomingScript>().home_speed = home_speed;
            Destroy(gameObject, 3.0f);
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
            if (gun_reference)
            {
                gun_reference.StopAllCoroutines();
            }
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
                int d = (damage - Target.defence);
                bool crit = false;
                System.Random rand = new System.Random();
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
                Target.HP -= d;
                Destroy(health_change_show, 1f);
                Destroy(gameObject);
            

        }
        else//if target is null
        {
            /*Before destruction,Stop all coroutines(the gun_abilities operating on this instance)
             to prevent exceptions from those coroutines*/
            if (gun_reference)
            {
                gun_reference.StopAllCoroutines();
            }
            Destroy(gameObject);//Destroy object immediately if null
        }
    }

   
}


