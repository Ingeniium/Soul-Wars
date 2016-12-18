using UnityEngine;
using System.Collections;

public class AIController : MonoBehaviour {
    private Rigidbody prb;
    public Transform ptr;
    public GameObject Shield;
    public GameObject Gun;
    private GameObject Target;
    private Transform ttr;
    private bool target_focus = true;
    private Collider trig;
    private float next_dodge = 0;
    public float dodge_delay;
    public float dodge_cooldown;
    private Vector3 vec;
    public float reaction_delay;
    [HideInInspector]
    public Transform gtr;
    public Collider enemy_attack_detection;
    public Gun gun;

	// Use this for initialization
	void Start ()
    {
      prb = GetComponentInParent<Rigidbody>();
        //GetCOmponent In parent apparently isn't working for transform
      enemy_attack_detection = GetComponent<Collider>();
      gtr = Gun.GetComponent<Transform>();
      gun = Gun.GetComponent<Gun>();
    }

   void OnTriggerEnter(Collider col)
    {
        trig = col;
        StartCoroutine(Evasion());
        enemy_attack_detection.enabled = false;
        enemy_attack_detection.isTrigger = false;                
    }
	
	// Update is called once per frame
   void Update()
   {
       if (Target == null)
       {
           Target = GameObject.FindGameObjectWithTag("Player");
           if (Target != null)
           {
               ttr = Target.GetComponent<Transform>();
           }
       }
       if (target_focus && ttr != null)
       {
           ptr.LookAt(ttr);
           ptr.position = Vector3.MoveTowards(ptr.position, ttr.position, .1f);
           if (gun.next_time < Time.time)
           {
               gun.Shoot();
           }
       }
   }

    IEnumerator Evasion()
    {
        yield return new WaitForSeconds(reaction_delay);
      if(trig != null)
      {
        if((next_dodge > Time.time || Vector3.Distance(trig.gameObject.transform.position,ptr.position) < 2) && Shield.GetComponent<HealthDefence>().regeneration == false)
        {
            target_focus = false;
            int turn = 0;
            while(turn != 90 && trig != null)
            {
                ptr.LookAt(trig.transform);
                Shield.transform.rotation *= Quaternion.AngleAxis(-45,Vector3.up);
                turn += 45;
                yield return new WaitForEndOfFrame();
            }
            Shield.transform.localPosition = new Vector3(.09f, .37f, .80f);
            gtr.localPosition = new Vector3(.87f, 0, -.071f);
            yield return new WaitForSeconds(.5f);
            target_focus = true;
            while (turn != 0)
            {
                Shield.transform.rotation *= Quaternion.AngleAxis(45,Vector3.up);
                turn -= 45;
                yield return new WaitForEndOfFrame();
            }
            Shield.transform.localPosition = new Vector3(.87f, .37f, -.071f);
            gtr.localPosition = new Vector3(.09f, 0, .80f);
        }
        else if(next_dodge < Time.time && trig != null)
        {
            next_dodge = Time.time + dodge_cooldown;
            yield return new WaitForSeconds(dodge_delay);
            vec = Quaternion.AngleAxis(90, trig.gameObject.transform.up) * trig.gameObject.transform.forward;
            prb.AddForce(vec * 10, ForceMode.Impulse);
        }

     }
      enemy_attack_detection.enabled = true;
      enemy_attack_detection.isTrigger = true;
    }
   
}

