using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Blaster : Gun
{
    protected override GunMod GetGunMod(int index)
    {
        return blaster_mods[index];
    }

    private readonly static GunMod[] blaster_mods = new GunMod[12]
    {
         new GunMod(Brigadier,
             "Punishment",
             "Brigadier" + "\n Does 150% damage to" + "\n spawn points and shields."),
         new GunMod(Gunslinger,
             "The Force",
             "Gunslinger" + "\n Causes a 2 second stun."),
         new GunMod(Destroyer,
             "Destruction",
             "Destroyer" + "\n Does 30% more damage for" + "\n each status effect the target" + "\n currently suffers."),

         new GunMod(Sunder,
             "Sundering",
             "Sunder" + "\n + 10 Sunder Power."),
         new GunMod(Curve,
             "Homing",
             "Curve" + "\n Grants slight homing ability."),
         new GunMod(Bounce,
             "Rebounding",
             "Bounce" + "\n Causes bullets to bounce on impact" + "\n and quadruples bullet lifetime."),

         new GunMod(Quake,
             "Robust",
             "Quake" + "\n Does half damage to nearby targets." + "\n Damage from the quake cannot crit."),
         new GunMod(Tremor,
             "Intense",
             "Tremor" + "\n Stuns nearby foes for 1 second."),
         new GunMod(StarFall,
             "Astral",
             "StarFall" + "\n Drops a piercing bullet " + "\n wherever your mouse is when shooting" + "\n in addition to the bullet already fired."),

         new GunMod(Meteor,
             "Meteor",
             "Meteor" + "\n Triples the size of your bullets."),
         new GunMod(Comet,
             "Comet",
             "Comet" + "\n Triples the speed of your bullets."),
         new GunMod(Asteroid,
             "Asteroid",
             "Asteroid" + "\n Causes your bullets to do max damage," + "\n but removes crit chance.")
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
            if (AI && AI.Target)
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
        new_script.rb.constraints = RigidbodyConstraints.None;
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
        if (!(script.Target is UnitHealthDefence))
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
        UnitHealthDefence tgt = script.Target as UnitHealthDefence;
        if(tgt)
        {
            const int STUN_TIME = 2;
            tgt.DetermineStun(STUN_TIME);
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
            UnitHealthDefence HP = col.GetComponent<UnitHealthDefence>();
            if (HP && script.Target.netId != HP.netId)
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
        script.coord_radius *= 3;
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
        if (script.Target.burning)
        {
            multiplier += .3f;
        }
        if (script.Target.sundered)
        {
            multiplier += .3f;
        }
        UnitHealthDefence tgt = script.Target as UnitHealthDefence;
        if (tgt)
        {
            if (tgt.chilling)
            {
                multiplier += .3f;
            }
            if (tgt.mezmerized)
            {
                multiplier += .3f;
            }
            if (tgt.stunned)
            {
                multiplier += .3f;
            }
        }
        float upper = script.upper_bound_damage;
        float lower = script.lower_bound_damage;
        upper *= multiplier;
        lower *= multiplier;
        script.upper_bound_damage = (int)upper;
        script.lower_bound_damage = (int)lower;
        script.coroutines_running--;
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
        projectile_speed = 15;
        knockback_power = 5;
        crit_chance = .10;
        reload_time = 4f;
        home_speed = 0;
        home_radius = 0;
        homes = false;
        /*Resources.Load seems to only work for getting prefabs as only game objects.*/
        Bullet = Resources.Load("CanonBall") as GameObject;
        coord_radius = 2f;
    }

    public override string GetImagePreviewString()
    {
        return "BlasterImage";
    }
}