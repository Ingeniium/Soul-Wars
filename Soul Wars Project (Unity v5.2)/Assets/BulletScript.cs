using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class BulletScript : MonoBehaviour {
	public int damage;
	public GameObject home;
    private GameObject homer;
	public float home_radius;
    public float home_speed;
    private HealthDefence Target;
    public GameObject health_change_canvas;
    private GameObject health_change_show;
	// Use this for initialization
	void Start () 
    {
	    homer = Instantiate (home, transform.position, Quaternion.identity) as GameObject;
		homer.transform.parent = gameObject.transform;
		homer.GetComponent<SphereCollider> ().radius = home_radius;
        homer.GetComponent<HomingScript>().home_speed = home_speed;
		Destroy(gameObject,8.0f);
	}

	void OnCollisionEnter (Collision hit) 
    {
        try
        {
            Target = hit.gameObject.GetComponent<HealthDefence>();
            /*If the target still exist and doesn't even have this script,
             Don't bother executing the rest of the code.As for the exception,it is there
             iin the event the object "dies" midway execution,presumably from another bullet.*/
            if (Target != null)
            {

                health_change_show = Instantiate(health_change_canvas, hit.gameObject.transform.position + new Vector3(0, 0, 1), Quaternion.Euler(90, 0, 0)) as GameObject;
                if (!Target.shield)
                {
                    health_change_show.GetComponentInChildren<Text>().text = "-" + (damage - Target.defence);
                    health_change_show.GetComponentInChildren<Text>().color = Color.red;
                }
                else
                {
                    health_change_show.GetComponentInChildren<Text>().text = "*BLOCKED*";
                    health_change_show.GetComponentInChildren<Text>().color = Color.grey;
                }
                Target.HP -= damage - Target.defence;
                Destroy(health_change_show, 1f);

            }
        }
        catch (System.NullReferenceException e)
        {
            if (health_change_show)
            {
                Destroy(health_change_show.gameObject);
            }
        }

        Destroy(gameObject);
    }
}
