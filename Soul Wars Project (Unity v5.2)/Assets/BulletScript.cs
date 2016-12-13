using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class BulletScript : MonoBehaviour {
	public int damage;
	public GameObject home;
    private GameObject homer;
	public float home_radius;
    private HealthDefence Target;
    public GameObject health_change_canvas;
    private GameObject health_change_show;
	// Use this for initialization
	void Start () {
	    homer = Instantiate (home, transform.position, Quaternion.identity) as GameObject;
		homer.transform.parent = gameObject.transform;
		homer.GetComponent<SphereCollider> ().radius = home_radius;
		Destroy(gameObject,8.0f);
	}

	void OnCollisionEnter (Collision hit) {
        Target = hit.gameObject.GetComponent<HealthDefence>();
        if (Target != null)
        {
            
            health_change_show = Instantiate(health_change_canvas,hit.gameObject.transform.position + new Vector3(0,0,1),Quaternion.Euler(90,0,0))as GameObject;
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
        Destroy(gameObject);
    }
}
