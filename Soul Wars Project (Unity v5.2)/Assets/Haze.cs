using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Haze : Gun
{
    protected override GunMod GetGunMod(int index)
    {
        return haze_mods[index];
    }

    private readonly static GunMod[] haze_mods = new GunMod[12]
    {
        new GunMod(Fog,
            "Fog",
            "Fog" + "\n Adds +10 Chill strength to bullets."),
        new GunMod(Conflagration,
            "Conflagration",
            "Conflagration" + "\n Adds +10 Burn strength to bullets."),
        null,

        new GunMod(Resistance,
            "Insulant",
            "Resistance" + "\n Enemy bullets that pass thru" + "\n this bullet will have half power" + "\n to most status effects."),
        new GunMod(Fume,
            "Toxic",
            "Fume" + "\n Adds one to two points of damage" + "\n to the bullet upon hitting" + "\n new targets."),
        new GunMod(Epidemic,
            "Contagious",
            "Epidemic" + "\n Allows chance for status effects" + "\n of enemies to spread within bullet's radius."),

        new GunMod(Debilitate,
            "Debilitating",
            "Debilitate" + "\n Bullets ignore half target resistance" + "\n to most status effects."),
        new GunMod(Engulf,
            "Ominous",
            "Engulf" + "\n Causes bullets to stick to their first target."),
        null,

        new GunMod(Infect,
            "Infectious",
            "Infect" + "\n Bullets from allies and yourself will" + "\n gain +5 standard status power" + "\n upon crossing bullets from this gun."),
        new GunMod(Cloud,
            "Cloudy",
            "Cloud" + "\n Doubles the size of the bullets."),
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

    private static IEnumerator Epidemic(Gun gun, BulletScript script)
    {
        bool chill = false;
        bool stun = false;
        bool burn = false;
        bool mez = false;
        bool sunder = false;
        const double num = .05;
        while (script)
        {
            while (!script.Target)
            {
                yield return new WaitForEndOfFrame();
            }
            if (burn)
            {
                int damage = script.lower_bound_damage
                    + (script.upper_bound_damage - script.lower_bound_damage) / 2
                    - script.Target.defence;
                script.Target.StartCoroutine(script.Target.DetermineBurn(num, damage));
            }
            if (sunder)
            {
                int damage = script.lower_bound_damage
                    + (script.upper_bound_damage - script.lower_bound_damage) / 2
                    - script.Target.defence;
                script.Target.StartCoroutine(script.Target.DetermineSunder(num, damage));
            }
            UnitHealthDefence tgt = script.Target as UnitHealthDefence;
            if (tgt)
            {
                if (chill)
                {
                    tgt.StartCoroutine(tgt.DetermineChill(num));
                }
                if (stun)
                {
                    tgt.DetermineStun(1);
                }
                if (mez)
                {
                    tgt.StartCoroutine(tgt.DetermineMezmerize(num));
                }
            } 
            if (script.Target.sundered)
            {
                sunder = true;
            }
            if (script.Target.burning)
            {
                burn = true;
            }
            if (tgt)
            {
                if (tgt.chilling)
                {
                    chill = true;
                }
                if (tgt.mezmerized)
                {
                    mez = true;
                }
                if (tgt.stunned)
                {
                    stun = true;
                }
            }
            yield return new WaitForEndOfFrame();
        }
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
            while (script)
            {
                if (script.Target && !prevTargs.Exists(delegate (HealthDefence h)
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
        List<uint> IDs = new List<uint>();
        string layer = "";
        layer = LayerMask.LayerToName(
            script.gameObject.layer);
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
        while (script)
        {

            if (script.Target && !IDs.Contains(script.Target.netId.Value))
            {
                script.chill_strength -= chill;
                script.chill_strength -= burn;
                script.mezmerize_strength -= mez;
                script.sunder_strength -= sunder;

                UnitHealthDefence tgt = script.Target as UnitHealthDefence;

                burn = script.Target.burn_resistance /= 2;
                sunder = tgt.sunder_resistance /= 2;
                if (tgt)
                {
                    chill = tgt.chill_resistance /= 2;
                    mez = tgt.mezmerize_resistance /= 2;
                }
              
                script.chill_strength += burn;
                script.sunder_strength += sunder;
                if (tgt)
                {
                    script.chill_strength += chill;
                    script.mezmerize_strength += mez;
                }
                IDs.Add(script.Target.netId.Value);
            }
            yield return new WaitForEndOfFrame();
        }
    }

    private static IEnumerator Infect(Gun gun, BulletScript script)
    {
        List<uint> IDs = new List<uint>();
        IDs.Add(script.netId.Value);
        while (script)
        {
            float radius = script.GetComponent<BoxCollider>().size.z;
            Collider[] bullet_colliders = Physics.OverlapSphere(script.gameObject.transform.position,
                radius,
                LayerMask.GetMask(
                    LayerMask.LayerToName(script.gameObject.layer)),
                QueryTriggerInteraction.Collide);
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
        if (trb)
        {
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
        else
        {
            rb.velocity = Vector3.zero;
        }
    }

    private static IEnumerator Cloud(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        BoxCollider collider = script.gameObject.GetComponent<BoxCollider>();
        collider.size = new Vector3(collider.size.x, collider.size.y * .5f, collider.size.z);
        NetworkMethods.Instance.RpcSetScale(script.gameObject, new Vector3(4, 4, 4));
        script.coroutines_running--;
        yield return null;
    }

    protected override string GunDesc()
    {
        return "Emits a slow but large," + " \n lingering projectile.";
    }

    public override string GetBaseName()
    {
        return "Haze";
    }

    public override void SetBaseStats(string _layer = "Ally")
    {
        upper_bound_damage = 8;
        lower_bound_damage = 6;
        layer = LayerMask.NameToLayer(_layer + "Attack");
        home_layer = LayerMask.NameToLayer(_layer + "Homing");
        color = SpawnManager.GetTeamColor(
            LayerMask.NameToLayer(_layer));
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
        coord_radius = 5f;
    }

    public override string GetImagePreviewString()
    {
        return "HazeImage";
    }

}
