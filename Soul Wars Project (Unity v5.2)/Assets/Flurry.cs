using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Flurry : Gun
{
    [SyncVar] public int num_bullets = 3;

    protected override GunMod GetGunMod(int index)
    {
        return flurry_mods[index];
    }

    private readonly static GunMod[] flurry_mods = new GunMod[12]
    {
        new GunMod(Hunter,
            "Precision",
            "Hunter" + "\n Causes bullets that aren't" + "\n homing in on a target to" + "\n to reroute to another flurry " + "\n bullet's target."
            ),
        new GunMod(Archer,
            "Archery",
            "Archer" + "\n Bullets have a (50 + level)%" + "\n chance to be target piercing."),
        null,

        new GunMod(Debris,
            "Perilous",
            "Debris" + "\n Causes bullets to enlarge and" + "\n move and turn at half speed" + "\n after 1.5 seconds"),
        new GunMod(Boomerang,
            "Psychic",
            "Boomerang" + "\n Causes bullets to pierce" + "\n its first target and" + "\n return to its firing position."),
        null,

        new GunMod(Shadow,
            "Umbra",
            "Shadow" + "\n Bullets have a 25% chance" + "\n to teleport behind target." ),
        new GunMod(Arrows,
            "Myriad",
            "Arrows" + "\n +2 Arrows fired."),
        null,

        new GunMod(Randomizer,
            "Random",
            "Randomizer" + "\n Bullets disappear until either" + "\n a random amount of time passes" + "\n or a target enters homing range."), 
        new GunMod(Seeker,
            "Potent",
            "Seeker" + "\n Each bullet gains a 20% speed" +"\n and homing radius boost each time" + "\n it homes in on a new target.") ,
        new GunMod(Diverge,
            "Divergent",
            "Diverge" + "\n Each bullet has a 10% chance" + "\n to create 10 other weaker, non homing" + "\n bullets on target impact.")
    };
    
    
    private static IEnumerator Hunter(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        while (!script.homer)//wait for homingscript reference
        {
            yield return new WaitForEndOfFrame();
        }
        HomingScript home = script.homer.GetComponent<HomingScript>();
        while (!home.homing && !script.has_collided)//Wait untuil it homes or collides
        {
            yield return new WaitForEndOfFrame();
        }
        Collider[] bullet_colliders = Physics.OverlapSphere(script.gameObject.transform.position, 
            10,
            LayerMask.GetMask("Default"), 
            QueryTriggerInteraction.Collide);
        foreach (Collider col in bullet_colliders)
        {
            /*Check if its a flurry bullet that isn't isn't homing*/
            BulletScript b = col.GetComponent<BulletScript>();
            if (b 
                && b.gameObject.layer == script.gameObject.layer 
                && b.gun_reference is Flurry 
                && !b.homer.GetComponent<HomingScript>().homing 
                && home.main_col)
            {
                /*Make bullet face target and fire towards them*/
                b.transform.LookAt(new Vector3(home.main_col.gameObject.transform.position.x, script.transform.position.y, home.main_col.gameObject.transform.position.z));
                b.GetComponent<Rigidbody>().velocity = b.transform.forward * b.GetComponent<Rigidbody>().velocity.magnitude;
            }
        }
        script.coroutines_running--;     
       
    }

    private static IEnumerator Archer(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        if (rand.NextDouble() < .30+ (double)gun.level / 10)
        {
            script.can_pierce = true;
        }
        script.coroutines_running--;
        yield return null;
    }

    private static IEnumerator Boomerang(Gun gun, BulletScript script)
    {
        
        Vector3 start_pos = script.transform.position;
        yield return new WaitForFixedUpdate();
        if (script.rb)
        {
            script.coroutines_running++;
            float speed = script.rb.velocity.magnitude;
            while (!script.Target)
            {
                yield return new WaitForEndOfFrame();
            }
            script.rb.velocity = Vector3.zero;
            script.coroutines_running--;
            if (!script.can_pierce)
            {
                script.can_pierce = true;
                script.coroutines_running--;
                script.transform.LookAt(start_pos);
                script.rb.velocity = speed * script.transform.forward;
                yield return new WaitForFixedUpdate();
                script.can_pierce = false;
            }
            else
            {
                script.transform.LookAt(start_pos);
                script.rb.velocity = speed * script.transform.forward;
                script.coroutines_running--;
            }
        }
     
    }

    private static IEnumerator Debris(Gun gun, BulletScript script)
    {
        script.lasting_time *= 2;
        yield return new WaitForSeconds(1f);
        script.homer.GetComponent<HomingScript>().home_speed /= 2;
        script.transform.position = new Vector3(
            script.transform.position.x,
            script.transform.position.y + 1f,
            script.transform.position.z);
        NetworkMethods.Instance.RpcSetScale(script.gameObject, Vector3.one * 2);
        script.GetComponent<Rigidbody>().velocity /= 2;
    }


    private static IEnumerator Arrows(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        Quaternion rot = Quaternion.LookRotation(
            gun.barrel_end.forward
            ) *
            Quaternion.Euler(0,90,0);
        Flurry Gun = (Flurry)gun;
        GameObject b = Instantiate(Gun.Bullet, gun.barrel_end.position,rot) as GameObject;
        NetworkServer.Spawn(b);
        Gun.Claimed_Gun_Mods -= Arrows;
        Gun.ReadyWeaponForFire(ref b);
        Gun.RpcFire(b.transform.forward, b);
        rot = Quaternion.LookRotation(
           gun.barrel_end.forward
           ) *
           Quaternion.Euler(0, -90, 0);
        b = Instantiate(Gun.Bullet, gun.barrel_end.position, rot) as GameObject;
        NetworkServer.Spawn(b);
        Gun.ReadyWeaponForFire(ref b);
        Gun.RpcFire(b.transform.forward, b);
        while (!script.Target && !Gun.HasReloaded(-.2f))
        {
            yield return new WaitForFixedUpdate();
        }
        Gun.Claimed_Gun_Mods += Arrows;
        script.coroutines_running--;
    }

    private static IEnumerator Seeker(Gun gun, BulletScript script)
    {
        while (!script.Target)
        {
            yield return new WaitForEndOfFrame();
        }
        List<HealthDefence> hp = new List<HealthDefence>();
        Rigidbody rb = script.GetComponent<Rigidbody>();
        SphereCollider home_col = script.homer.GetComponent<SphereCollider>();
        float speed_boost = rb.velocity.magnitude * .2f;
        float home_boost = home_col.radius * .2f;
        while (script)
        {
            if (!hp.Contains(script.Target))
            {
                hp.Add(script.Target);
                rb.velocity = script.transform.forward * (rb.velocity.magnitude + speed_boost);
                home_col.radius += home_boost;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    private static IEnumerator Shadow(Gun gun, BulletScript script)
    {
        while(!script.homer)
        {
            yield return new WaitForEndOfFrame();
        }
        HomingScript h = script.homer.GetComponent<HomingScript>();
        while (!h.homing)
        {
            yield return new WaitForEndOfFrame();
        }
        if(rand.NextDouble() < .25)
        {
            Vector3 path = h.main_col.transform.position - script.transform.position;
            script.transform.position += (path * 1.5f);
        }
    }

    private static IEnumerator Randomizer(Gun gun, BulletScript script)
    {
        float num = UnityEngine.Random.Range(.5f, 2.5f);
        while (!script.homer)
        {
            yield return new WaitForEndOfFrame();
        }
        HomingScript h = script.homer.GetComponent<HomingScript>();
        if (!script.Target && !h.main_col)
        {
            NetworkMethods.Instance.RpcSetEnabled(script.gameObject, "Renderer", false);
            NetworkMethods.Instance.RpcSetLayer(script.gameObject, 16);
            Rigidbody rb = script.GetComponent<Rigidbody>();
            Vector3 speed = rb.velocity;
            rb.velocity = Vector3.zero;
            float start_time = Time.time;
            while (start_time + num > Time.time && !h.main_col && !script.Target)
            {
                yield return new WaitForEndOfFrame();
            }
            NetworkMethods.Instance.RpcSetEnabled(script.gameObject, "Renderer", true);
            NetworkMethods.Instance.RpcSetLayer(script.gameObject, gun.layer);
            rb.velocity = speed.magnitude * script.transform.forward;
        }
    }

    public static IEnumerator Diverge(Gun gun, BulletScript script)
    {
        if (rand.NextDouble() < .1)
        {
            Flurry ggun = (Flurry)gun;
            script.coroutines_running++;
            while (!script.Target)
            {
                yield return new WaitForEndOfFrame();
            }
            for(float i = 0; i < 360;i += 36)
            {
                GameObject bullet = Instantiate(ggun.Bullet,
                    script.transform.position,
                    Quaternion.Euler(0, i, 0));
                BulletScript s = bullet.GetComponent<BulletScript>();
                Destroy(s.homer);
                NetworkServer.Spawn(bullet);
                ggun.Claimed_Gun_Mods -= Diverge;
                ggun.ReadyWeaponForFire(ref bullet);
                s.can_pierce = true;
                ggun.RpcFire(bullet.transform.forward,bullet);
                ggun.Claimed_Gun_Mods += Diverge;
            }
            script.coroutines_running--;
        }
        else
        {
            yield return null;
        }
    }
    public override void Shoot(Vector3 forward,Vector3 pos,Quaternion q)
    {
        base.Shoot(forward,pos,q);
        for (int i = 1; i < num_bullets; i++)
        {
            Quaternion rot;
            if (i % 2 != 0)
            {
                rot = GetComponentsInChildren<Transform>()[2].rotation;
            }
            else
            {
                rot = GetComponentsInChildren<Transform>()[3].rotation;
            }
            bullet = Instantiate(Bullet, pos, rot) as GameObject;
            NetworkServer.Spawn(bullet);
            ReadyWeaponForFire(ref bullet);
            RpcFire(bullet.transform.forward,bullet);
        }
    }

    public override void Shoot(GameObject g)
    {
        base.Shoot(g);
        for (int i = 1; i < num_bullets; i++)
        {
            Quaternion rot;
            if (i % 2 != 0)
            {
                rot = GetComponentsInChildren<Transform>()[2].rotation;
            }
            else
            {
                rot = GetComponentsInChildren<Transform>()[3].rotation;
            }
            bullet = Instantiate(Bullet, barrel_end.position, rot) as GameObject;
            NetworkServer.Spawn(bullet);
            ReadyWeaponForFire(ref bullet);
            RpcFire(bullet.transform.forward,bullet);
        }
    }

    protected override string GunDesc()
    {
        return "Shoots 3 homing arrows.";
    }

    public override string GetBaseName()
    {
        return "Flurry";
    }

    public override void SetBaseStats(string _layer = "Ally")
    {
        upper_bound_damage = 7;
        lower_bound_damage = 4;
        layer = LayerMask.NameToLayer(_layer + "Attack");
        home_layer = LayerMask.NameToLayer(_layer + "Homing");
        color = SpawnManager.GetTeamColor(
            LayerMask.NameToLayer(_layer));
        range = 10;
        projectile_speed = 5;
        knockback_power = 5;
        crit_chance = .05;
        reload_time = 1.5f;
        home_speed = 1.5f;
        home_radius = 1f;
        homes = true;
        /*Resources.Load seems to only work for getting prefabs as only game objects.*/
        Bullet = Resources.Load("Bullet") as GameObject;
        coord_radius = 2f;
    }

    public override string GetImagePreviewString()
    {
        return "FlurryImage";
    }

}
