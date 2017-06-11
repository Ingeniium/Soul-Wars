using UnityEngine;
using System.Collections.Generic;
//OnTriggerStay isn't working
public class HomingScript : MonoBehaviour
{
    public Rigidbody prb;
    public Transform ptr;
    private List<Collider> col = new List<Collider>();//Made into a list as to enable consideration of multiple projectiles
    public Collider main_col;//Collider that device is currently homing in
    public bool homing = false;//Whether it is currently homing on a target
    public float home_speed;
    Vector3 target_pos;
   

    void OnTriggerEnter(Collider Target)
    {
        col.Add(Target);//Add the collider for consideration
        if (main_col == null)
        {
            main_col = Target;
        }
        homing = true;//Make sure that homing is true
    }

    void OnTriggerExit(Collider Target)
    {
        col.Remove(Target);
        if (col.Count == 0)//If there are no colliders to consider,mark device as not homing
        {
            homing = false;
            main_col = null;
        }
        /*If the main collider is out of homing range,and there are other
         * targets in range,then choose the target that's closest to the bullet*/
        else
        {
            try
            {
                float[] distances = new float[col.Count];//A soon to be sorted array
                float[] dist_ref = new float[col.Count];//An array that has original indices
                for (int i = 0; i < col.Count; i++)
                {
                    distances[i] = Vector3.Distance(col[i].transform.position, ptr.position);
                    dist_ref[i] = distances[i];
                }
                System.Array.Sort(distances);
                main_col = col[System.Array.FindIndex(dist_ref, delegate(float f) { return (f == distances[0]); })];
            }
            catch (System.Exception e)
            {
                homing = false;
                main_col = null;
            }
        }
    }

    void Update()
    {
       
        if (homing)//Exception handling present in case object is destroyed rather than exiting the trigger
        {
            try
            {
                target_pos = new Vector3(main_col.gameObject.transform.position.x, ptr.position.y, main_col.gameObject.transform.position.z);
                ptr.forward = Vector3.RotateTowards(ptr.forward, target_pos - ptr.position, home_speed, 0);                
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
            prb.velocity = prb.velocity.magnitude * ptr.forward;//For maintaining magnitude while changing direction
        }

    }
}