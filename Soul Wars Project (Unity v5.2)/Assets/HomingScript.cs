using UnityEngine;
using System.Collections.Generic;
//OnTriggerStay isn't working
public class HomingScript : MonoBehaviour
{
    public Rigidbody prb;
    public Transform ptr;
    private List<Collider> col;//Made into a list as to enable consideration of multiple projectiles
    private Collider main_col;
    private bool homing = false;
    public float home_speed;
   
    void Start()
    {
        prb = GetComponentInParent<Rigidbody>();
        ptr = transform.parent;/*After many hours of frusration,I found that GetComponentInParent<Transform>()
        returned THIS object's transform,not the bullet's*/
        ptr.eulerAngles = new Vector3(100, ptr.eulerAngles.y, ptr.eulerAngles.z);
    }

    void OnTriggerEnter(Collider Target)
    {
        col.Add(Target);
        if (main_col == null)
        {
            main_col = Target;
        }
        homing = true;
    }

    void OnTriggerExit(Collider Target)
    {
        col.Remove(Target);
        if (col.Count == 0)
        {
            homing = false;
            main_col = null;
        }
        /*If main collider its homing towards if out of range,and there are other
         * targets in range,then choose the target that's closest to the bullet*/
        else
        {
            float[] distances = new float[col.Count];
            float[] dist_ref = new float[col.Count];
            for (int i = 0; i < col.Count; i++)
            {
                distances[i] = Vector3.Distance(col[i].transform.position, ptr.position);
                dist_ref[i] = distances[i];
            }
            System.Array.Sort(distances);
            main_col = col[System.Array.FindIndex(dist_ref,delegate(float f) {return (f == distances[0]);})];
        }
    }

    void Update()
    {
       
        if (homing)
        {
            try
            {
                ptr.forward = Vector3.RotateTowards(ptr.forward, main_col.gameObject.transform.position - ptr.position, home_speed, 0);                
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
           //prb.AddForce(ptr.forward);
            prb.velocity = prb.velocity.magnitude * ptr.forward;
        }

    }
}