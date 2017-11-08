using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class BulletScript : NetworkBehaviour
{
    [SyncVar] public int upper_bound_damage;
    [SyncVar] public int lower_bound_damage;
    Vector3 last_pos;
    static System.Random rand = new System.Random();
    public GameObject home;
    public GameObject homer;
    [SyncVar] public float home_radius;
    [SyncVar] public float home_speed;
    [SyncVar] public double crit_chance;
    [SyncVar] public float knockback_power;
    [SyncVar] public double chill_strength;
    [SyncVar] public double burn_strength;
    [SyncVar] public double mezmerize_strength;
    [SyncVar] public double sunder_strength;
    public Gun gun_reference;
    [SyncVar] public bool has_collided = false;
    [SyncVar] public bool legit_target = false;
    public bool homes = false;
    [SyncVar] public int coroutines_running = 0;
    public bool can_pierce
    {
        get { return _can_pierce; }
        set
        {
            _can_pierce = value;
            GetComponent<Collider>().isTrigger = value;
        }
    }
    private bool _can_pierce;
    public bool can_bounce = false;
    public float lasting_time = 3f;
    public bool damaging = false;
    public HealthDefence Target;
    public Rigidbody rb;
    public static List<ValueGroup<Coordinate, BulletScript>> BulletCoords = new List<ValueGroup<Coordinate, BulletScript>>();
    List<Coordinate> path_coords = new List<Coordinate>();
    private float start_time = 0;
    private int bullet_layer = 0;
    private Coordinate current_coord;
    // Use this for initialization

    void Start()
    {
        last_pos = transform.position;
        rb = GetComponent<Rigidbody>();
        StartCoroutine(WaitForGunReference());
        PlayersAlive.Instance.Bullets.Add(this);
    }

    IEnumerator WaitForGunReference()
    {
        while (!gun_reference)
        {
            yield return new WaitForEndOfFrame();
        }
        homer = transform.GetChild(0).gameObject;
        homer.GetComponent<SphereCollider>().radius = home_radius;
        homer.layer = gun_reference.home_layer;
        GetComponent<Collider>().isTrigger = can_pierce;
        HomingScript script = homer.GetComponent<HomingScript>();
        script.home_speed = home_speed;
        StartCoroutine(WaitForNetworkDestruction());
        if (gameObject.layer != LayerMask.NameToLayer("AllyAttackUndetectable"))
        {
            StartCoroutine(RecordBulletCoord());
        }
    }

    IEnumerator RecordBulletCoord()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        if (gun_reference.client_user)
        {
            bullet_layer = gun_reference.client_user.gameObject.layer;
        }
        else
        {
            while (!gun_reference.transform.parent)
            {
                yield return new WaitForEndOfFrame();
            }
            bullet_layer = gun_reference.GetComponentInParent<HealthDefence>().gameObject.layer;
        }
        Quaternion last_rot = transform.rotation * new Quaternion(180, 180, 180, 0);
        Vector3 projected_end_pos;
        Coordinate projected_end_coord;
        Coordinate start_coord;
        uint min_x;
        uint min_z;
        uint max_x;
        uint max_z;
        while (this)
        {
            if (Mathf.Abs(
                Quaternion.Angle(last_rot, transform.rotation))
                > 5f)
            {
                ClearHazardCoords();
                last_rot = transform.rotation;
                projected_end_pos = transform.position + rb.velocity * (start_time + lasting_time - Time.realtimeSinceStartup);
                projected_end_coord = Map.Instance.GetPos(projected_end_pos);
                start_coord = Map.Instance.GetPos(transform.position);
                if (projected_end_coord != null && start_coord != null)
                {
                    if (start_coord.x < projected_end_coord.x)
                    {
                        min_x = start_coord.x;
                        max_x = projected_end_coord.x;
                    }
                    else
                    {
                        min_x = projected_end_coord.x;
                        max_x = start_coord.x;
                    }
                    if (start_coord.z < projected_end_coord.z)
                    {
                        min_z = start_coord.z;
                        max_z = projected_end_coord.z;
                    }
                    else
                    {
                        min_z = projected_end_coord.z;
                        max_z = start_coord.z;
                    }
                    min_x--;
                    max_x++;
                    min_z--;
                    max_x++;
                    for (uint i = min_x; i <= max_x; i++)
                    {
                        for (uint j = min_z; j <= max_z; j++)
                        {
                            if (Map.Instance.GetPos(i, j) != null &&
                                !path_coords.Contains(
                                Map.Instance.GetPos(i, j)))
                            {
                                Map.Instance.GetPos(i, j).hazard_layers.Add(bullet_layer);
                                path_coords.Add(Map.Instance.GetPos(i, j));
                                Map.Instance.GetPos(i, j).status = Coordinate.Status.Hazard;
                                Map.Instance.GetPos(i, j).hazard_cost += upper_bound_damage * 1000;
                            }
                        }
                    }
                }
            }
            yield return new WaitForFixedUpdate();
        }
    }

    void ClearHazardCoords()
    {
        foreach (Coordinate coord in path_coords)
        {
            coord.hazard_cost -= upper_bound_damage * 1000;
            coord.hazard_layers.Remove(bullet_layer);
            if (coord.hazard_layers.Count == 0)
            {
                coord.status = Coordinate.Status.Safe;
            }
        }
        path_coords.Clear();
    }

    [ServerCallback]
    public IEnumerator WaitForNetworkDestruction()
    {
        start_time = Time.time;
        /*WaitForEndOfFrame used instead of wait for seconds in situations
         where lastng time changes dynamically(IE, gun abilities)*/
        while (Time.time < start_time + lasting_time)
        {
            yield return new WaitForEndOfFrame();
        }
        GetComponent<Collider>().enabled = false;
        ClearHazardCoords();
        NetworkServer.Destroy(gameObject);
    }


    IEnumerator Pierce(Collision hit)
    {
        Collider first = GetComponent<Collider>();
        Collider second = null;
        Collider third = null;
        if (hit != null)
        {
            second = hit.gameObject.GetComponent<Collider>();
            third = null;
            if (homer)
            {
                third = homer.GetComponent<Collider>();
                Physics.IgnoreCollision(third, second);
            }
        }
        yield return new WaitForSeconds(1);
        if (second)
        {
            Physics.IgnoreCollision(first, second, false);
            if (third)
            {
                Physics.IgnoreCollision(third, second, false);
            }
        }

    }

    IEnumerator Pierce(Collider hit)
    {
        Collider first = GetComponent<Collider>();
        Collider second = hit;
        Collider third = null;
        if (hit)
        {
            if (homer)
            {
                third = homer.GetComponent<Collider>();
                Physics.IgnoreCollision(third, second);
            }
            Physics.IgnoreCollision(first, second);
        }
        yield return new WaitForSeconds(1);
        if (second && second.enabled)
        {
            Physics.IgnoreCollision(first, second, false);
            if (third)
            {
                Physics.IgnoreCollision(third, second, false);
            }
        }
    }


    [ServerCallback]
    void OnCollisionEnter(Collision hit)
    {
        try
        {
            StartCoroutine(Damage(hit));
        }
        catch (System.NullReferenceException e)
        {
            StopAllCoroutines();
            NetworkServer.Destroy(gameObject);
            ClearHazardCoords();
            //Always destroy the object upon any detectable impact upon an exception           
        }
    }

    [ServerCallback]
    void OnTriggerEnter(Collider hit)
    {
        try
        {
            if (can_pierce)//Check put there b/c otherwise,homing detection would call it
            {
                StartCoroutine(Damage(null, hit));
            }
        }
        catch (System.NullReferenceException e)
        {
            StopAllCoroutines();
            NetworkServer.Destroy(gameObject);
            ClearHazardCoords();
            //Always destroy the object upon any detectable impact upon an exception           
        }
    }


    IEnumerator Damage(Collision hit, Collider col = null)
    {
        if (!damaging)
        {
            damaging = true;
            if (col == null)
            {
                Target = hit.gameObject.GetComponent<HealthDefence>();
            }
            else
            {
                Target = col.gameObject.GetComponent<HealthDefence>();
            }
            /*If the target still exist and doesn't even have this script,
             Don't bother executing the rest of the code.As for the exception,it is there
             in the event the object "dies" midway execution,presumably from another bullet.*/
            /*If a spawn point is hit by enemy,just run on client.*/
            /*If an enemy is hit(for enemy guns have no client user set),test whether the code is
             running on the same client as the one who shot the bullet(for number UI to show up)
             *If a player is hit,run the code only on whoever got hit*/
            if (Target != null)
            {
                legit_target = true;
                has_collided = true;
                //Wait until all coroutines operating on the bullet finish
                while (coroutines_running > 0)
                {
                    yield return new WaitForFixedUpdate();
                }
                if (can_pierce)
                {
                    if (hit != null)
                    {
                        StartCoroutine(Pierce(hit));
                    }
                    else
                    {
                        StartCoroutine(Pierce(col));
                    }
                }
                ValueGroup<bool,int> v = GetDamage(Target);
                bool crit = v.index;
                int damage = v.value;
                if (damage > 0)
                {
                    Target.StartCoroutine(Target.DetermineChill(chill_strength));
                    Target.StartCoroutine(Target.DetermineBurn(burn_strength, damage));
                    Target.StartCoroutine(Target.DetermineMezmerize(mezmerize_strength));
                    Target.StartCoroutine(Target.DetermineSunder(sunder_strength, damage));
                    if (crit)
                    {
                        Target.RpcDisplayHPChange(new Color(114, 0, 198), damage);//Violet
                    }
                    else
                    {
                        Target.RpcDisplayHPChange(Color.red, damage);
                    }
                    ApplyExperience(damage, Target);
                    if (Target.Controller)
                    {
                        Target.GetComponent<Rigidbody>().velocity = Vector3.zero;
                    }
                    AIController AI = Target.GetComponentInChildren<AIController>();
                    if (AI != null)
                    {
                        ApplyAggro(AI,damage);
                    }
                    else
                    {
                        SpawnManager SP = Target.GetComponent<SpawnManager>();
                        if (SP)
                        {
                            ApplySpawnCounterDamage(SP, damage);
                        }
                    }
                    Target.HP -= damage;
                }
            }
            if (!can_pierce && !can_bounce)
            {
                GetComponent<Collider>().enabled = false;
                ClearHazardCoords();
                NetworkServer.Destroy(gameObject);
            }
            damaging = false;
        }
    }

    /*returns damage based on random rolls, the boundaries of damage set
      by lower-bound_damage and upper_bound_damage fields,Target defence, and gun abilities
      which can affect those fields.Also returns whether it crit or not. */
    ValueGroup<bool,int> GetDamage(HealthDefence Target)
    {
        int damage = rand.Next(lower_bound_damage, upper_bound_damage);
        float da = (100 - Target.defence);
        da /= 100;
        int d = (int)((float)damage * da);
        bool crit = false;
        if ((crit_chance - Target.crit_resistance) 
            >= rand.NextDouble() + .001)//bullets with a crit chance of 0 shouldn't be able to land a crit
        {
            crit = true;
            d *= 3;
        }
        return new ValueGroup<bool, int>(crit, d);
    }

    /*Applies experience to whomever shot the damaging bullet*/
    void ApplyExperience(int damage,HealthDefence Target)
    {
        if (Target.has_exp && gun_reference.client_user)
        {                                                                                        //
            if (damage >= Target.HP)
            {
                gun_reference.experience += (int)(Target.HP * Target.exp_rate);
            }
            else
            {
                gun_reference.experience += (int)(damage * Target.exp_rate);
            }
        }
    }

    /*Applies aggro to whoever damage the AIController.Assumes that
      the target is a cpu.*/
    void ApplyAggro(AIController AI,int damage)
    {
        /*This distinction is made,for players' guns aren't children for the sake
          of manual position syncing in multiplayer.*/
        NetworkInstanceId ID;
        string name;
        if (gun_reference.client_user)
        {
            name = gun_reference.client_user.player_name;
            ID = gun_reference.client_user.netId;
        }
        else
        {
            NetworkBehaviour pnb = gun_reference.transform.parent.GetComponent<NetworkBehaviour>();
            ID = pnb.netId;
            AIController aAI = pnb.GetComponentInChildren<AIController>();
            name = LayerMask.LayerToName(aAI.ptr.gameObject.layer) + " CPU "
                + aAI.index;
        }
        AI.UpdateAggro(damage, ID);
        AI.PrintAggro(name, ID.Value);
    }

    /*Adds damage d to the counter of SpawnManager SP.Assumes that 
     SP isn't null.*/
    void ApplySpawnCounterDamage(SpawnManager SP,int d)
    {
        if (gun_reference.client_user)
        {
            Target.UpdateDamageCounter(d, gun_reference.client_user.gameObject.layer);
        }
        else
        {
            Target.UpdateDamageCounter(d, gun_reference.GetComponentInParent<HealthDefence>().gameObject.layer);
        }
    }

    



}


