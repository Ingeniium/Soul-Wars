using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Haze : Gun
{
    private readonly static string[] gun_ability_names = new string[12] { "Fog", "Conflagration", null,
                                                                         "Resistance","Fume",null,
                                                                         "Debilitate","Engulf",null,
                                                                         "Infect","Cloud",null};
    private readonly static string[] gun_name_addons = new string[12] { "Fog", "Conflagration", null,
                                                                        "Insulant", "Toxic",null,
                                                                        "Debilitating", "Ominous",null,
                                                                        "Infectious", "Cloudy", null};
    private readonly static string[] gun_ability_desc = new string[12] {
        "Fog" + "\n Adds +10 Chill strength to bullets.",
        "Conflagration" + "\n Adds +10 Burn strenght to bullets.",
        null,

        "Resistance" + "\n Enemy bullets that pass thru" + "\n this bullet will have half power" + "\n to most status effects.",
        "Fume" + "\n Adds one to two points of damage" + "\n to the bullet upon hitting" + "\n new targets.",
        null,

        "Debilitate" + "\n Bullets ignore half target resistance" + "\n to most status effects.",
        "Engulf" + "\n Causes bullets to stick to their first target.",
        null,

        "Infect" + "\n Bullets from allies and yourself will" + "\n have +5 resistance to most status effects" + "\n upon crossing bullets from this gun.",
        "Cloud" + "\n Doubles the size of the bullets.",
        null
    };
    /*This class's pool of gun_abilities.Use of a static container of static methods requi"Markring explicit this
     pointers are used for onetime,pre-Awake() initialization of delegates*/
    private static List<Gun_Abilities> Gun_Mods = new List<Gun_Abilities>()//This class's pool of gun_abilities
    {
        Fog,
        Conflagration,
        null,

        Resistance,
        Fume,
        null,

        Debilitate,
        Engulf,
        null,

        Infect,
        Cloud,
        null
    };

    private static IEnumerator Fog(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        script.chill_strength += .10;
        script.coroutines_running--;
        yield return null;
    }

    private static IEnumerator Conflagration(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        script.burn_strength += .10;
        script.coroutines_running--;
        yield return null;
    }

    private static IEnumerator Fume(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        while (script.has_collided == false)
        {
            yield return new WaitForEndOfFrame();
        }
        script.coroutines_running--;
        if (script.legit_target == false)//Check if target even is valid
        {
            yield return null;
        }
        else
        {
            List<HealthDefence> prevTargs = new List<HealthDefence>();
            while(script)
            {
                if(script.Target && !prevTargs.Exists(delegate(HealthDefence h)
                {
                    return (h.netId == script.Target.netId);
                }))
                {
                    prevTargs.Add(script.Target);
                    script.upper_bound_damage += 2;
                    script.lower_bound_damage++;
                }
                yield return new WaitForEndOfFrame();
            }
            
        }
    }

    private static IEnumerator Resistance(Gun gun, BulletScript script)
    {
        string layer = null;
        List<uint> IDs = new List<uint>();
        if (script.gameObject.layer == 13)
        {
            layer = "EnemyAttack";
        }
        else
        {
            layer = "AllyAttack";
        }
        while (script)
        {
            Collider[] bullet_colliders = Physics.OverlapSphere(script.gameObject.transform.position, 2,
                LayerMask.GetMask(layer), QueryTriggerInteraction.Collide);
            foreach (Collider col in bullet_colliders)
            {
                BulletScript b = col.gameObject.GetComponent<BulletScript>();
                if (b && !IDs.Contains(b.netId.Value))
                {
                    IDs.Add(b.netId.Value);
                    b.chill_strength /= 2;
                    b.burn_strength /= 2;
                    b.mezmerize_strength /= 2;
                    b.sunder_strength /= 2;
                }
            }
            yield return new WaitForEndOfFrame();
        }
    }

    private static IEnumerator Debilitate(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        while (script.has_collided == false)
        {
            yield return new WaitForEndOfFrame();
        }
        List<uint> IDs = new List<uint>();
        double chill = 0;
        double burn = 0;
        double mez = 0;
        double sunder = 0;
        script.coroutines_running--;
        while(script)
        {
        
        if(script.Target && !IDs.Contains(script.Target.netId.Value))
        {
            script.chill_strength -= chill;
            script.chill_strength -= burn;
            script.mezmerize_strength -= mez;
            script.sunder_strength -= sunder;

            chill = script.Target.chill_resistance /= 2;
            burn = script.Target.burn_resistance /= 2;
            mez = script.Target.mezmerize_resistance /= 2;
            sunder = script.Target.sunder_resistance /= 2;

            script.chill_strength += chill;
            script.chill_strength += burn;
            script.mezmerize_strength += mez;
            script.sunder_strength += sunder;

            IDs.Add(script.Target.netId.Value);
        }
        yield return new WaitForEndOfFrame();
        }
    }

    private static IEnumerator Infect(Gun gun, BulletScript script)
    {
        string layer = null;
        List<uint> IDs = new List<uint>();
        if (script.gameObject.layer == 13)
        {
            layer = "AllyAttack";
        }
        else
        {
            layer = "EnemyAttack";
        }
        while (script)
        {
            Collider[] bullet_colliders = Physics.OverlapSphere(script.gameObject.transform.position, 2,
                LayerMask.GetMask(layer), QueryTriggerInteraction.Collide);
            foreach (Collider col in bullet_colliders)
            {
                BulletScript b = col.gameObject.GetComponent<BulletScript>();
                if (b && !IDs.Contains(b.netId.Value))
                {
                    IDs.Add(b.netId.Value);
                    b.chill_strength += .05;
                    b.burn_strength += .05;
                    b.mezmerize_strength += .05;
                    b.sunder_strength += .05;
                }
            }
            yield return new WaitForEndOfFrame();
        }
    }

    private static IEnumerator Engulf(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        while (!script.Target)
        {
            yield return new WaitForEndOfFrame();
        }
        Rigidbody rb = script.GetComponent<Rigidbody>();
        Rigidbody trb = script.Target.GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        script.transform.position = new Vector3(script.Target.transform.position.x,
            script.transform.position.y,
            script.Target.transform.position.z);
        script.coroutines_running--;
        while (script)
        {
            rb.velocity = new Vector3(trb.velocity.x,
                0,
                trb.velocity.z);
            yield return new WaitForFixedUpdate();
        }
    }

    private static IEnumerator Cloud(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        BoxCollider collider = script.gameObject.GetComponent<BoxCollider>();
        collider.size = new Vector3(collider.size.x, collider.size.y * .5f, collider.size.z);
        NetworkMethods.Instance.RpcSetScale(script.gameObject,new Vector3(4,4,4));
        script.coroutines_running--;
        yield return null;
    }

    protected override string GunAbilityDesc(int index)
    {
        return gun_ability_desc[index];
    }

    protected override string ClassGunAbilityNames(int index)
    {
        return gun_ability_names[index];
    }

    public override Gun_Abilities ClassGunMods(int index)
    {
        return Gun_Mods[index];
    }

    protected override void SetGunNameAddons(int index)
    {
        if (index < 4 || index > 12)
        {
            suffixes.Add(gun_name_addons[index]);
        }
        else
        {
            prefixes.Add(gun_name_addons[index]);
        }
    }

    protected override bool AreGunLevelUpButtonsAssignedForClass()
    {
        return (GunTable.buttons[0].method == null);
    }

    protected override string GunDesc()
    {
        return "Emits a slow but large," + " \n lingering projectile.";
    }

    public override string GetBaseName()
    {
        return "Haze";
    }

    public override void SetBaseStats()
    {
        upper_bound_damage = 8;
        lower_bound_damage = 6;
        if (client_user)
        {
            layer = 13;
            home_layer = 10;
            color = new Color(43, 179, 234);
        }
        else
        {
            layer = 14;
            home_layer = 12;
            color = Color.red;
        }
        range = 10;
        projectile_speed = 2;
        knockback_power = 5;
        crit_chance = .05;
        reload_time = 2f;
        home_speed = 0;
        home_radius = 0;
        homes = false;
        can_pierce = true;
        /*Resources.Load seems to only work for getting prefabs as only game objects.*/
        Bullet = Resources.Load("Cloud") as GameObject;
    }

    public override string GetImagePreviewString()
    {
        return "HazeImage";
    }

}
   