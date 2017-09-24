using UnityEngine;
using System.Collections.Generic;
//OnTriggerStay isn't working
public class HomingScript : MonoBehaviour
{
    public Rigidbody prb;
    public Transform ptr;
    private List<Collider> bullet_colliders = new List<Collider>();//Made into a list as to enable consideration of multiple projectiles
    public Collider main_col;//Collider that device is currently homing in
    public bool homing = false;//Whether it is currently homing on a target
    public float home_speed;//angle speed at which bullet can turn
    Vector3 target_pos;//position of the collider device is homing on

    void Awake()
    {
        prb = GetComponentInParent<Rigidbody>();
        ptr = GetComponentsInParent<Transform>()[1];//Index 0 actually returns this object's transform rather than bullet object's
    }

    void OnTriggerEnter(Collider Target)
    {
        if (!bullet_colliders.Contains(Target))
        {
            bullet_colliders.Add(Target);//Add the collider for consideration
            bullet_colliders.RemoveNull();//Remove destroyed colliders 
            if (bullet_colliders.Count == 0)//If there are no colliders to consider,mark device as not homing
            {
                homing = false;
                main_col = null;
            }
            else
            {
                /*Sort by least distance to greatest distance*/
                bullet_colliders.SortByLeastToGreatDist(ptr.position);
                main_col = bullet_colliders[0];
            }

        }
        homing = true;//Make sure that homing is true
    }

    void OnTriggerExit(Collider Target)
    {
        if (bullet_colliders.Contains(Target))
        {
            bullet_colliders.Remove(Target);
            /*If the main collider is out of homing range,and there are other
             * targets in range,then choose the target that's closest to the bullet*/
            bullet_colliders.RemoveNull();//Remove destroyed colliders
            if (bullet_colliders.Count == 0)//If there are no colliders to consider,mark device as not homing
            {
                homing = false;
                main_col = null;
            }
            else
            {
                /*Sort by least distance to greatest distance*/
                bullet_colliders.SortByLeastToGreatDist(ptr.position);
                main_col = bullet_colliders[0];
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
                OnTriggerExit(main_col);
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