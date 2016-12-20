using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {
	public GameObject Shield;
    public GameObject Gun;
    public Gun gun;
    public float speed = .1f;
    private Transform str;
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
            gun.Shoot();
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