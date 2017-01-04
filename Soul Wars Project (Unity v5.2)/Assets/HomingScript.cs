using UnityEngine;
using System.Collections;
//OnTriggerStay isn't working
public class HomingScript : MonoBehaviour
{
    public Rigidbody prb;
    public Transform ptr;
    private Collider col;
    private bool homing = false;
    public float home_speed;
   
    void Start()
    {
        prb = GetComponentInParent<Rigidbody>();
        ptr = transform.parent;/*After many hours of frusration,I found that GetComponentInParent<Transform>()
        returned THIS objects transform,not the bullet's*/
        ptr.eulerAngles = new Vector3(100, ptr.eulerAngles.y, ptr.eulerAngles.z);
    }

    void OnTriggerEnter(Collider Target)
    {
        homing = true;
        col = Target;
        
    }

    void OnTriggerExit()
    {
        homing = false;
        col = null;
       
    }

    void Update()
    {
       // ptr.Rotate(Vector3.up, 100, Space.World);
        if (homing)
        {
            try
            {
                ptr.forward = Vector3.RotateTowards(ptr.forward, col.gameObject.transform.position - ptr.position, home_speed, 0);                
            }
            catch (System.Exception e) 
            {
                homing = false;
            }
        }
    }

    void FixedUpdate()
    {
       
        if (homing)
        {
           prb.AddForce(ptr.forward);
        }

    }
}