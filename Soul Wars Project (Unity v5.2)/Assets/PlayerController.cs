using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {
	public GameObject Shield;
    public GameObject Gun;
    public float speed = .1f;
    private Transform str;
    private Gun gun;
    private float moveHorizontal;
    private float moveVertical;
	private Rigidbody rb;
	private Transform tr;
    private bool switching = false;
    private bool defending = false;
    private float turn = 0;
	void Start () 
    {
		rb = GetComponent<Rigidbody>();
		tr = GetComponent<Transform> ();
        str = Shield.GetComponent<Transform>();
        gun = Gun.GetComponent<Gun>();
	}
	void Update() 
    {
        if (Input.GetMouseButtonDown(1) && switching != true)
        {
            switching = true;
        }
        else if (Input.GetMouseButtonDown(0) && gun.next_time < Time.time)
        {
            gun.bullet = Instantiate(gun.Bullet, gun.barrel_end.position, gun.barrel_end.rotation) as GameObject;
            gun.bullet.GetComponent<Renderer>().material.color = Color.cyan;
            gun.bullet.AddComponent<Rigidbody>();
            gun.bullet.GetComponent<Rigidbody>().useGravity = false;
            gun.bullet.GetComponent<BulletScript>().damage = gun.damage;
            gun.bullet.GetComponent<BulletScript>().home_radius = gun.home_radius;
            gun.bullet.GetComponent<BulletScript>().home.layer = 10;
            gun.bullet.GetComponent<Rigidbody>().AddForce(gun.barrel_end.forward, ForceMode.Impulse);//works
            gun.next_time = Time.time + gun.reload_time;
        }
        if(switching)
        {
            turn += 10;
            if (turn == 90)
            {
                turn = 0;
                switching = false;
            }
            if (!defending)
            {
                str.RotateAround(tr.position, Vector3.up, -10f);
               
                speed -= .005f;
                if (turn == 0)
                {
                    defending = true;
                    if (Shield.GetComponent<HealthDefence>().regeneration == false)
                    {
                        Shield.GetComponent<HealthDefence>().shield_collider.enabled = true;
                    }
                    
                }
            }
            else
            {
                str.RotateAround(tr.position, Vector3.up, 10f);
                
                if (turn == 0)
                {
                    defending = false;
                    Shield.GetComponent<HealthDefence>().shield_collider.enabled = false;
                }
                speed += .005f;
            }

            
        }
	}
	
	// Update is called once per frame
	void FixedUpdate ()
    {
        if(rb.velocity != Vector3.zero){rb.velocity = Vector3.zero;}
		moveHorizontal = Input.GetAxis ("Horizontal");
		moveVertical = Input.GetAxis ("Vertical");
		tr.Translate (moveHorizontal*speed, 0, moveVertical*-speed,Space.World);
       
   }
}