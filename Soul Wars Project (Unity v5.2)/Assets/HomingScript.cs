using UnityEngine;
using System.Collections;
//OnTriggerStay isn't working
public class HomingScript : MonoBehaviour
{
    private Rigidbody prb;
    private Transform ptr;
    private Collider col;
    private bool homing = false;
  
    void Start()
    {
        prb = GetComponentInParent<Rigidbody>();
        ptr = GetComponentInParent<Transform>();
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
        if (homing == true && col != null)
        {
           ptr.forward = Vector3.RotateTowards(ptr.forward, col.gameObject.transform.position - ptr.position , 10.0f,0);
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