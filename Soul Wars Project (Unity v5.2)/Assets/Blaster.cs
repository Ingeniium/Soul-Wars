using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Blaster : Gun
{
    private readonly static string[] gun_ability_names = new string[12] { "Brigadier", "Gunslinger", null,
                                                                        "Sunder", "Curve", null,
                                                                         "Quake", "Tremor", null,
                                                                         "Meteor", "Comet", null,};                           
    private readonly static string[] gun_name_addons = new string[12] { "Punishment", "The Force", null,
                                                                        "Sundering", "Homing", null,
                                                                         "Robust", "Intense", null,
                                                                         "Meteor", "Comet", null,};
    private readonly static string[] gun_ability_desc = new string[12] {
        "Brigadier" + "\n Does 150% damage to" + "\n spawn points and shields.",
        "Gunslinger" + "\n Causes a 2 second stun.",
        null,

       "Sunder" + "\n + 10 Sunder Power.",
        "Curve" + "\n Grants slight homing ability.",
        null,

        "Quake" + "\n Does moderate damage " + "\n that's based on the gun to nearby targets.",
        "Tremor" + "\n Stuns nearby foes for 1 second.",
        null,

        "Meteor" + "\n Triples the size of your bullets.",
        "Comet" + "\n Triples the speed of your bullets.",
        null,
    };
    /*This class's pool of gun_abilities.Use of a static container of static methods requi"Markring explicit this
    pointers are used for onetime,pre-Awake() initialization of delegates*/
    private static List<Gun_Abilities> Gun_Mods = new List<Gun_Abilities>()//This class's pool of gun_abilities
    {
        Brigadier,
        Gunslinger,
        null,

        Sunder,
        Curve,
        null,

        Quake,
        Tremor,
        null,

        Meteor,
        Comet,
        null,
    };

   

    private static IEnumerator Brigadier(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        while (!script.has_collided && !script.Target)//Wait for collision and check target validity
        {
            yield return new WaitForEndOfFrame();
        }
        if (script.Target.type != HealthDefence.Type.Unit)
        {
            /*Because of lack of implicit casting of rvalue integral types in C#,
             multiplying 1.5 in an acceptable manner would be useless(as the 
             .5 would be truncated.Creating and changing floats would
            float _lower_bound_damage = script.lower_bound_damage;
             allow most of the change to take effect*/
            float _lower_bound_damage = script.lower_bound_damage;
            float _upper_bound_damage = script.upper_bound_damage;
            _upper_bound_damage *= 1.5f;
            _lower_bound_damage *= 1.5f;
            script.upper_bound_damage = (int)_upper_bound_damage;
            script.lower_bound_damage = (int)_lower_bound_damage;
        }
        script.coroutines_running--;
    }

    private static IEnumerator Gunslinger(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        while (!script.has_collided && !script.Target)//Wait for collision and check target validity
        {
            yield return new WaitForEndOfFrame();
        }
        if (script.Target.type == HealthDefence.Type.Unit)
        {
            script.Target.DetermineStun(2);
        }
        script.coroutines_running--;
    }

    private static IEnumerator Sunder(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        script.sunder_strength += .10;
        script.coroutines_running--;
        yield return null;
    }

    private static IEnumerator Curve(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        while (!script.homer)
        {
            yield return new WaitForEndOfFrame();
        }
        script.homer.GetComponent<SphereCollider>().radius = 1;
        script.homer.GetComponent<HomingScript>().home_speed = 1;
        script.coroutines_running--;    
    }

    private static IEnumerator Tremor(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        while (!script.has_collided && !script.Target)//Wait for collision and check target validity
        {
            yield return new WaitForEndOfFrame();
        }
        string layer = "";
        if (script.Target.gameObject.layer == 9)
        {
            layer = "Ally";
        }
        else
        {
            layer = "Enemy";
        }
        Collider[] target_colliders = Physics.OverlapSphere(script.gameObject.transform.position, 7,
            LayerMask.GetMask(layer), QueryTriggerInteraction.Collide);
        foreach (Collider col in target_colliders)
        {
            HealthDefence HP = col.GetComponent<HealthDefence>();
            if (HP && script.Target.netId != HP.netId && HP.type == HealthDefence.Type.Unit)
            {
                HP.DetermineStun(1);
            }
        }
        script.coroutines_running--;
    }

    private static IEnumerator Quake(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        while (!script.has_collided && !script.Target)//Wait for collision and check target validity
        {
            yield return new WaitForEndOfFrame();
        }
        string layer = "";
        if (script.Target.gameObject.layer == 9)
        {
            layer = "Ally";
        }
        else
        {
            layer = "Enemy";
        }
        Collider[] target_colliders = Physics.OverlapSphere(script.gameObject.transform.position, 7,
            LayerMask.GetMask(layer), QueryTriggerInteraction.Collide);
        foreach (Collider col in target_colliders)
        {
            HealthDefence HP = col.GetComponent<HealthDefence>();
            if (HP && script.Target.netId != HP.netId)
            {
                int d = rand.Next(script.lower_bound_damage, script.upper_bound_damage) / 2;
                HP.RpcDisplayHPChange(Color.red, d);
                HP.HP -= d;
            }
        }
        script.coroutines_running--;
    }

    private static IEnumerator Meteor(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        SphereCollider collider = script.gameObject.GetComponent<SphereCollider>();
        script.transform.position = new Vector3(script.transform.position.x, script.transform.position.y + 1.5f, script.transform.position.z);
        NetworkMethods.Instance.RpcSetScale(script.gameObject, new Vector3(2.4f, 2.4f, 2.4f));
        script.coroutines_running--;
        yield return null;
    }

    private static IEnumerator Comet(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        Rigidbody rb = script.GetComponent<Rigidbody>();
        yield return new WaitForFixedUpdate();
        rb.velocity *= 3;
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

    protected override string GunDesc()
    {
        return "Shoots a fast,large bullet";
    }

    public override string GetBaseName()
    {
        return "Blaster";
    }

    public override void SetBaseStats()
    {
        upper_bound_damage = 35;
        lower_bound_damage = 20;
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
        range = 20;
        projectile_speed = 10;
        knockback_power = 5;
        crit_chance = .05;
        reload_time = 4f;
        home_speed = 0;
        home_radius = 0;
        homes = false;
        /*Resources.Load seems to only work for getting prefabs as only game objects.*/
        Bullet = Resources.Load("CanonBall") as GameObject;
    }

    public override string GetImagePreviewString()
    {
        return "BlasterImage";
    }
}