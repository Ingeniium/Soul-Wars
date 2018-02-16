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
    private Coordinate current_coord;
    public float coord_radius = 3;

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
            StartCoroutine(TrackBulletCoordinates());
        }
    }

    /*Updates coordinates based on what the bullet "occupies".
     The collider that is considered for what it "occupies"
     is set by coord_area,which is set editor time and gun 
     abilities at run time.*/
    IEnumerator TrackBulletCoordinates()
    {
        while (!gun_reference.client_user &&
            !gun_reference.transform.parent)
        {
            yield return new WaitForFixedUpdate();
        }
        int bullet_layer = GetBulletLayer();
        const float TIME_FOR_NEXT_TRACKING = .2f;
        while (this)
        {
            AddColliderCoordsToPathCoords(bullet_layer);
            yield return new WaitForSeconds(TIME_FOR_NEXT_TRACKING);
        }
    }

    /*Adds coordinates that the collider "occupies"
     based on the world bounds the collider takes up.*/
    void AddColliderCoordsToPathCoords(int bullet_layer)
    {
        ClearHazardCoords();
        /*Using collider.bounds actually doesn't give you absolute extrema x and z position values(i checked), 
         on sphere colliders.Since not every bullet has a sphere collider as a homing or damage collision mechanism,
         a field named coord_radius is used instead.*/
        Vector3 pos = transform.position;
        for (float i = pos.x - coord_radius; i <= pos.x + coord_radius; i += Map.Instance.interval_x)
        {
            for (float j = pos.z - coord_radius; j <= pos.z + coord_radius; j += Map.Instance.interval_z)
            {
                Vector3 new_pos = new Vector3(i, 13, j);
                Coordinate coord = Map.Instance.GetPos(new_pos);
                float cost = upper_bound_damage * 25;
                AddCoordToPathCoords(coord, cost, bullet_layer);
            }
        }
    }

    /*Adds a bullet to the path coords list, and updates each coordinate's
      hazard info.*/
    void AddCoordToPathCoords(Coordinate coord, float cost, int bullet_layer)
    {
        if (coord != null && !path_coords.Contains(coord))
        {
            coord.hazard_layers.Add(new ValueGroup<ValueGroup<uint, int>, float>(
                new ValueGroup<uint, int>(netId.Value,
                bullet_layer),
                cost));
            coord.status = Coordinate.Status.Hazard;
            path_coords.Add(coord);
        }
    }

    /*Returns the layer of whomever owns the gun that
      created this bullet*/
    int GetBulletLayer()
    {
        int bullet_layer = 0;
        if (gun_reference.client_user)
        {
            bullet_layer = gun_reference.client_user.gameObject.layer;
        }
        else
        {
            bullet_layer = gun_reference.GetComponentInParent<HealthDefence>().gameObject.layer;
        }
        return bullet_layer;
    }

    /*Removes the bullet's info from the coord (essentially marking that the 
     bullet no longer "occupies" the area anymore) and clears the list of path
     coordinates.*/
    void ClearHazardCoords()
    {
        foreach (Coordinate coord in path_coords)
        {
            coord.hazard_layers.RemoveAll(delegate (ValueGroup<ValueGroup<uint, int>, float> v)
            {
                uint id = v.index.index;
                return (id == netId.Value);
            });
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
            ClearHazardCoords();
            NetworkServer.Destroy(gameObject);
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
            ClearHazardCoords();
            NetworkServer.Destroy(gameObject);
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
                ValueGroup<bool, int> v = GetDamage(Target);
                bool crit = v.index;
                int damage = v.value;
                if (damage > 0)
                {
                    double[] powers = { burn_strength,sunder_strength,chill_strength, mezmerize_strength };
                    Target.DetermineStatusEffects(powers, damage);
                    if (crit)
                    {
                        Target.RpcDisplayHPChange(new Color(114, 0, 198), damage);//Violet
                    }
                    else
                    {
                        Target.RpcDisplayHPChange(Color.red, damage);
                    }
                    ApplyExperience(damage, Target as UnitHealthDefence);
                    ApplyAggro(damage, Target as UnitHealthDefence);                 
                    ApplySpawnCounterDamage(damage,Target as SpawnPointHealthDefence);
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
    ValueGroup<bool, int> GetDamage(HealthDefence Target)
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
    void ApplyExperience(int damage, UnitHealthDefence Target)
    {
        if (Target && gun_reference.client_user)
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
    void ApplyAggro(int damage, UnitHealthDefence Target)
    {
        /*This distinction is made,for players' guns aren't children for the sake
          of manual position syncing in multiplayer.*/
        if (Target)
        {
            AIController AI = Target.GetComponentInChildren<AIController>();
            if (AI)
            {
                NetworkInstanceId ID;
                if (gun_reference.client_user)
                {
                    ID = gun_reference.client_user.netId;
                }
                else
                {
                    NetworkBehaviour pnb = gun_reference.transform.parent.GetComponent<NetworkBehaviour>();
                    ID = pnb.netId;
                    AIController aAI = pnb.GetComponentInChildren<AIController>();
                }
                AI.UpdateAggro(damage, ID);
            }
        }
    }

    /*Adds damage d to the counter of SpawnManager SP.Assumes that 
     SP isn't null.*/
    void ApplySpawnCounterDamage(int damage,SpawnPointHealthDefence Target)
    {
        if (Target)
        {
            if (gun_reference.client_user)
            {
                Target.UpdateDamageCounter(damage, gun_reference.client_user.gameObject.layer);
            }
            else
            {
                Target.UpdateDamageCounter(damage, gun_reference.GetComponentInParent<HealthDefence>().gameObject.layer);
            }
        }
    }





}


