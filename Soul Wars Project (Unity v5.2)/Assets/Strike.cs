using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Strike : Gun
{

    protected override GunMod GetGunMod(int index)
    {
        return strike_mods[index];
    }

    private readonly static GunMod[] strike_mods = new GunMod[12]
    {
        new GunMod(Marksman,
            "Marksmanship",
            "Marksman" + "\n Can grant up to" + "\n 30% crit chance based on" + "\n how little the bullet turns."),
        new GunMod(Sniper,
            "Accuracy",
             "Sniper" + "\n Adds a seconds of" + "\n cooldown to the enemy's" + "\n current gun."),
        new GunMod(Scout,
            "Swiftness",
            "Scout" + "\n Adds 3 points of speed" + "\n for 3 seconds" + "\n to the gun's owner each time" + "\n they hit a target."),

        new GunMod(Accelerate,
            "Accelerating",
            "Accelerate" + "\n Gradually increases speed while" + "\n the bullet isn't homing on a target."),
        new GunMod(Camouflage,
            "Hidden",
            "Camouflage" + "\n 30% chance to make bullets" + "\n less visible to enemies."),
        new GunMod(Pressure,
            "Pressuring",
            "Pressure" + "\n Removes .25 seconds of remaining cooldown" + "\n upon hitting a target."),

        new GunMod(Momentum,
            "Mighty",
            "Momentum" + "\n Increases damage as bullet" +"\n displacement increases."),
        new GunMod(Barrier,
            "Adamant",
            "Barrier" + "\n Allows collisions and homing" +"\n between enemy and ally bullets."),
        new GunMod(Thirst,
            "Perceptive",
            "Thirst" + "\n Gradually increases the " +"\n homing radius, homing speed, and" +"\n lasting time while the bullet" + "\n isn't homing on a target.") ,

        new GunMod(ArcaneQuiver,
            "Arcane",
            "Arcane Quiver" + "\n 20% chance for a chosen" + "\n gun ability to reapply itself," +"\n doubling its effectiveness" + "\n each reapplication."),
        new GunMod(MythrilQuiver,
            "Mythril",
            "Mythril Quiver" + "\n +10 Mezmerize power."),
        new GunMod(BloodyQuiver,
            "Bloody",
            "Bloody Quiver" + "\n Restores a point of HP" + "\n upon hitting a target.")
    };
  
   
    /*Ability that grants extra crit chance based on how far the bullet
     deviates from its "original" path.Less deviation means more crit,the 
     maximum amount being +30%*/
    private static IEnumerator Marksman(Gun gun,BulletScript script)
    {
        script.coroutines_running++;
        Vector3 start_pos = script.transform.position;//Get its original position
        Vector3 start_forward = script.transform.forward;//Get where it's originally facing
        while (script.has_collided == false)//Wait for Collision
        {
            yield return new WaitForFixedUpdate();
        }
        if (script.legit_target == false)//Check if target even is valid
        {
            script.coroutines_running--;
            yield return null;
        }
        else
        {
            Vector3 current = script.transform.position - start_pos;//Get the vector representing distance traveled
            Vector3 path = Vector3.Project(current, start_forward);//Use current to project where the bullet would go if it didn't turn
            float ang = Vector3.Angle(path, current);
            if (ang < 30)//Award bonus only if deviation is less than 30 degrees
            {
                if (ang < 5)//If deviation is less than 5 degrees,go ahead and give full bonus
                {
                    ang = 0;
                }
                script.crit_chance += (30 - ang) * .01;
            }
            script.coroutines_running--;
            yield return null;
        }
    }

    private static IEnumerator Sniper(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        while (script.has_collided == false)//Wait for Collision
        {
            yield return new WaitForFixedUpdate();
        }
        if (script.legit_target == false || !(script.Target is UnitHealthDefence))//Check if target even is valid
        {
            script.coroutines_running--;
            yield break;
        }
        else
        {
            script.Target.RpcUpdateAilments("\r\n <color=yellow>+ 1 sec cooldown on gun </color>", 1);
            Gun targ = script.Target.GetComponentInChildren<GenericController>().main_gun;
            if (targ)
            {
                if (targ.HasReloaded())
                {
                    targ.next_time = Time.time + 1f;
                }
                else
                {
                    targ.next_time += 1f;
                }
            }
            script.coroutines_running--;
        }
    }

    private static IEnumerator Momentum(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        Vector3 start_pos = script.transform.position;
        while (!script.Target)
        {
            yield return new WaitForFixedUpdate();
        }
        if (script.legit_target == false)//Check if target even is valid
        {
            script.coroutines_running--;
            yield return null;
        }
        else
        {
            int bonus = (int)Math.Abs(
                Vector3.Distance(start_pos, script.transform.position));
            script.upper_bound_damage += bonus;
            script.lower_bound_damage += bonus;
            script.coroutines_running--;
        }
    }

    private static IEnumerator Accelerate(Gun gun, BulletScript script)
    {
        Vector3 start_pos = script.transform.position;
        Rigidbody rb = script.GetComponent<Rigidbody>();
        while (!script.homer)
        {
            yield return new WaitForEndOfFrame();
        }
        HomingScript hm = script.homer.GetComponent<HomingScript>();
        while(script)
        {
            if (!hm.homing)
            {
                rb.velocity = rb.velocity.normalized
                    * (rb.velocity.magnitude
                    + Math.Abs(
                    Vector3.Distance(start_pos, script.transform.position))
                    / 10);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    

    private static IEnumerator MythrilQuiver(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        script.mezmerize_strength += .1;
        script.coroutines_running--;
        yield return null;
    }

    public static IEnumerator ArcaneQuiver(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        Strike strike = gun as Strike;
        if (!script.Target)
        {
            int i = 0;
            foreach (Gun_Abilities g in strike.Claimed_Gun_Mods.GetInvocationList())
            {
                if (i <= gun.level - gun.mez_threshold && rand.NextDouble() < .20)
                {
                    script.StartCoroutine(g(gun, script));
                    i++;
                }
            }
        }
        script.coroutines_running--;
        yield return null;
    }

    private static IEnumerator Barrier(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        if(gun.client_user)
        {
            script.gameObject.layer = gun.client_user.gameObject.layer;
        }
        else if(gun.transform.parent)
        {
            script.gameObject.layer = gun.transform.parent.gameObject.layer;
        }
        script.coroutines_running--;
        yield return null;
    }

    private static IEnumerator Camouflage(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        Renderer rend = script.GetComponent<Renderer>();
        Color origin = rend.material.color;
        if (rand.NextDouble() < .3)
        {
            if (gun.layer == 13)
            {
                /*Undetectable ally layer added to make it seems like enemies
                 can't respond(block or dodge) it due to it not colliding with
                 defensive detection.*/
                script.gameObject.layer = 16;
                rend.material.color = new Color(origin.r, origin.g, origin.b, .3f);
            }
            else
            {
                rend.material.color = new Color(origin.r, origin.g, origin.b, .2f);
            }
        }
        script.coroutines_running--;
        yield return null;
    }

    private static IEnumerator BloodyQuiver(Gun gun,BulletScript script)
    {
        script.coroutines_running++;
        while(!script.Target)
        {
            yield return new WaitForEndOfFrame();
        }
        if (gun.client_user)
        {
            gun.client_user.GetComponent<HealthDefence>().HP += 1;
        }
        else
        {
            gun.GetComponentInParent<HealthDefence>().HP += 1;
        }
        script.coroutines_running--;
    }

    private static IEnumerator Scout(Gun gun,BulletScript script)
    {
        script.coroutines_running++;
        while (!script.Target)
        {
            yield return new WaitForEndOfFrame();
        }
        script.lasting_time += 3.2f;
        script.GetComponent<Rigidbody>().velocity = Vector3.zero;
        NetworkMethods.Instance.RpcSetEnabled(script.gameObject, "Collider", false);
        NetworkMethods.Instance.RpcSetEnabled(script.gameObject, "Renderer", false);
        script.coroutines_running--;
        script.can_bounce = true;
        if (gun.client_user && gun.client_user.speed < 27)
        {
            gun.client_user.speed += 3;
            gun.client_user.shield_speed += 3;
            yield return new WaitForSeconds(3);
            gun.client_user.speed -= 3;
            gun.client_user.shield_speed -= 3;
        }
        else
        {
            AIController AI = gun.GetComponentInParent<AIController>();
            if (AI && AI.speed < 32)
            {
                AI.speed += 4;
                AI.shield_speed += 4;
                yield return new WaitForSeconds(3);
                AI.speed -= 4;
                AI.shield_speed -= 4;
            }
        }
        script.can_bounce = false;

    }

    private static IEnumerator Pressure(Gun gun,BulletScript script)
    {
        script.coroutines_running++;
        while(!script.Target)
        {
            yield return new WaitForEndOfFrame();
        }
        gun.next_time -= .25f;
        script.coroutines_running--;
    }

    private static IEnumerator Thirst(Gun gun, BulletScript script)
    {
        Vector3 start_pos = script.transform.position;
        Rigidbody rb = script.GetComponent<Rigidbody>();
        while (!script.homer)
        {
            yield return new WaitForEndOfFrame();
        }
        HomingScript hm = script.homer.GetComponent<HomingScript>();
        SphereCollider col = hm.GetComponent<SphereCollider>();
        float original_radius = col.radius;
        while (script)
        {
            if (!hm.homing)
            {
                col.radius += original_radius * .1f;
                hm.home_speed += .1f;
                script.lasting_time += .001f;
            }
            yield return new WaitForFixedUpdate();
        }
    }

    protected override string GunDesc()
    {
        return "Launches a powerful arrow.";
    }

    public override string GetBaseName()
    {
        return "Strike";
    }

    public override void SetBaseStats(string _layer = "Ally")
    {
        upper_bound_damage = 15;
        lower_bound_damage = 7;
        layer = LayerMask.NameToLayer(_layer + "Attack");
        home_layer = LayerMask.NameToLayer(_layer + "Homing");
        color = SpawnManager.GetTeamColor(
            LayerMask.NameToLayer(_layer));
        range = 10f;
        projectile_speed = 7.5f;
        knockback_power = 5;
        crit_chance = .05;
        reload_time = 1f;
        home_speed = 2.5f;
        home_radius = 2f;
        coord_radius = 3f;
        homes = true;
        /*Resources.Load seems to only work for getting prefabs as only game objects.*/
        Bullet = Resources.Load("Bullet") as GameObject;
    }

    public override string GetImagePreviewString()
    {
        return "StrikeImage";
    }



}
