using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Blaster : Gun
{
    private readonly static string[] gun_ability_names = new string[12] 
    {
        "Brigadier", "Gunslinger", "Destroyer",
        "Sunder", "Curve", "Bounce",
        "Quake", "Tremor", "StarFall",
        "Meteor", "Comet", "Asteroid"
    };                           
    private readonly static string[] gun_name_addons = new string[12] 
    {
        "Punishment", "The Force", "Destruction",
        "Sundering", "Homing", "Rebounding",
        "Robust", "Intense", "Astral",
        "Meteor", "Comet", "Asteroid"
    };
    private readonly static string[] gun_ability_desc = new string[12]
    {
        "Brigadier" + "\n Does 150% damage to" + "\n spawn points and shields.",
        "Gunslinger" + "\n Causes a 2 second stun.",
        "Destroyer" + "\n Does 30% more damage for" + "\n each status effect the target" + "\n currently suffers.",

        "Sunder" + "\n + 10 Sunder Power.",
        "Curve" + "\n Grants slight homing ability.",
        "Bounce" + "\n Causes bullets to bounce on impact" + "\n and quadruples bullet lifetime.",

        "Quake" + "\n Does half damage to nearby targets." + "\n Damage from the quake cannot crit.",
        "Tremor" + "\n Stuns nearby foes for 1 second.",
        "StarFall" + "\n Drops a piercing bullet " + "\n wherever your mouse is when shooting" + "\n in addition to the bullet already fired." ,

        "Meteor" + "\n Triples the size of your bullets.",
        "Comet" + "\n Triples the speed of your bullets.",
        "Asteroid" + "\n Causes your bullets to do max damage," + "\n but removes crit chance.",
    };
    /*This class's pool of gun_abilities.Use of a static container of static methods requi"Markring explicit this
    pointers are used for onetime,pre-Awake() initialization of delegates*/
    private static List<Gun_Abilities> Gun_Mods = new List<Gun_Abilities>()//This class's pool of gun_abilities
    {
        Brigadier,
        Gunslinger,
        Destroyer,

        Sunder,
        Curve,
        Bounce,

        Quake,
        Tremor,
        StarFall,

        Meteor,
        Comet,
        Asteroid,
    };

    private static IEnumerator Asteroid(Gun gun,BulletScript script)
    {
        script.coroutines_running++;
        script.lower_bound_damage = script.upper_bound_damage;
        script.crit_chance = 0;
        script.coroutines_running--;
        yield return null;
    }

    private static IEnumerator StarFall(Gun gun,BulletScript script)
    {
        
        script.coroutines_running++;
        Vector3 pos;
        if (gun.client_user)
        {
            pos = new Vector3(Input.mousePosition.x / Screen.width,
                Input.mousePosition.y / Screen.height,
               20);
            pos = PlayerFollow.camera.ViewportToWorldPoint(pos);
            pos = new Vector3(pos.x, 15, pos.z);
        }
        else
        {
            AIController AI = gun.GetComponentInParent<AIController>();
            if (AI.Target)
            {
                Transform ttr = AI.Target.transform;
                pos = new Vector3(ttr.position.x, ttr.position.y + 10f, ttr.position.z);
            }
            else
            {
                pos = Vector3.zero;
            }
        }
        float original_next_time = gun.next_time;
        GameObject new_bullet = Instantiate(gun.Bullet, pos, gun.Bullet.transform.rotation) as GameObject;
        NetworkServer.Spawn(new_bullet);
        Blaster blaster = gun as Blaster;
        gun.mez_threshold += 100;
        blaster.ReadyWeaponForFire(ref new_bullet);
        blaster.next_time = original_next_time;
        BulletScript new_script = new_bullet.GetComponent<BulletScript>();      
        new_script.can_pierce = true;
        new_script.rb.velocity = Vector3.zero;
        new_script.rb.useGravity = true;
        gun.mez_threshold -= 100;
        script.coroutines_running--;
        yield return null;
    }

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
        if (gun.client_user)
        {
            layer = LayerMask.LayerToName(
                gun.client_user.gameObject.layer);
        }
        else
        {
            layer = LayerMask.LayerToName(
                gun.GetComponentInParent<HealthDefence>()
                .gameObject.layer);
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
        if (gun.client_user)
        {
            layer = LayerMask.LayerToName(
                gun.client_user.gameObject.layer);
        }
        else
        {
            layer = LayerMask.LayerToName(
                gun.GetComponentInParent<HealthDefence>()
                .gameObject.layer);
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
        script.transform.position = new Vector3(script.transform.position.x, script.transform.position.y + 1.0f, script.transform.position.z);
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

    private static IEnumerator Bounce(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        script.can_bounce = true;
        script.lasting_time *= 4;
        script.coroutines_running--;
        yield return new WaitForFixedUpdate();
        Vector3 original_velocity = script.rb.velocity;
        while (script)
        {
            while (!script.damaging)
            {
                yield return new WaitForEndOfFrame();
            }
            script.rb.velocity = new Vector3(script.rb.velocity.x,
                                             0,
                                             script.rb.velocity.z);
            if (script.rb.velocity.magnitude < original_velocity.magnitude)
            {
                script.rb.velocity = script.rb.velocity.normalized * original_velocity.magnitude;
            }
            yield return new WaitForFixedUpdate();
        }      

    }

    private static IEnumerator Destroyer(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        while(!script.Target)
        {
            yield return new WaitForEndOfFrame();
        }
        float multiplier = 1;
        if(script.Target.chilling)
        {
            multiplier += .3f;
        }
        if (script.Target.burning)
        {
            multiplier += .3f;
        }
        if (script.Target.mezmerized)
        {
            multiplier += .3f;
        }
        if (script.Target.stunned)
        {
            multiplier += .3f;
        }
        if (script.Target.sundered)
        {
            multiplier += .3f;
        }
        float upper = script.upper_bound_damage;
        float lower = script.lower_bound_damage;
        upper *= multiplier;
        lower *= multiplier;
        script.upper_bound_damage = (int)upper;
        script.lower_bound_damage = (int)lower;
        script.coroutines_running--;
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

    public override void SetBaseStats(string _layer = "Ally")
    {
        upper_bound_damage = 35;
        lower_bound_damage = 20;
        layer = LayerMask.NameToLayer(_layer + "Attack");
        home_layer = LayerMask.NameToLayer(_layer + "Homing");
        color = SpawnManager.GetTeamColor(
            LayerMask.NameToLayer(_layer));
        range = 20;
        projectile_speed = 10;
        knockback_power = 5;
        crit_chance = .10;
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