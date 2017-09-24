using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Strike : Gun {
    private readonly static string[] gun_ability_names = new string[12] 
    {
        "Marksman", "Sniper", null,
        "Accelerate", "Camofluage", null,
        "Momentum", "Barrier", null,
        "Arcane Quiver", "Mythril Quiver", null
    };
    private readonly static string[] gun_name_addons = new string[12] 
    {
        "Marksmanship","Accuracy",null,
        "Accelerating", "Hidden", null,
        "Mighty", "Adamant", null,
        "Arcane", "Mythril", null
    };
    private readonly static string[] gun_ability_desc = new string[12] 
    {
        "Marksman" + "\n Can grant up to" + "\n 30% crit chance based on" + "\n how little the bullet turns.",
        "Sniper" + "\n Adds a seconds of" + "\n cooldown to the enemy's" + "\n current gun.",
         null,

        "Accelerate" + "\n Gradually increases speed while" + "\n the bullet isn't homing on a target.",
        "Camouflage" + "\n 30% chance to make bullets" + "\n less visible to enemies.",
        null,

        "Momentum" + "\n Increases damage as bullet" +" \n displacement increases.",
        "Barrier" + "\n Allows collisions between" +"\n enemy and ally bullets.",
        null,

        "Arcane Quiver" + "\n 20% chance for a chosen" + "\n gun ability to reapply itself," +"\n doubling its effectiveness" + "\n each reapplication.",
        "Mythril Quiver" + "\n +10 Mezmerize power.",
        null
    };
    /*This class's pool of gun_abilities.Use of a static container of static methods requiring explicit this
     pointers are used for onetime,pre-Awake() initialization of delegates*/
    private static List<Gun_Abilities> Gun_Mods = new List<Gun_Abilities>()//This class's pool of gun_abilities
    {
        {Marksman},
        {Sniper},
         null,

        {Accelerate},
        {Camouflage},
        null,

        {Momentum},
        {Barrier},
        null,

        {ArcaneQuiver},
        {MythrilQuiver},
        null
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
        if (script.legit_target == false || script.Target.type != HealthDefence.Type.Unit)//Check if target even is valid
        {
            script.coroutines_running--;
            yield return null;
        }
        else
        {
            script.Target.RpcUpdateAilments("\r\n <color=yellow>+ 1 sec cooldown on gun </color>", 1);
            Gun targ = script.Target.Controller.main_gun;
            if (targ.HasReloaded())
            {
                targ.next_time = Time.time + 1f;
            }
            else
            {
                targ.next_time += 1f;
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
                rb.velocity = rb.velocity.normalized *
                    (rb.velocity.magnitude +
                    Math.Abs(
                    Vector3.Distance(start_pos, script.transform.position)) / 10);
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
        if (!script.Target)
        {
            int i = 0;
            foreach (Gun_Abilities g in gun.Claimed_Gun_Mods.GetInvocationList())
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
        if (gun.layer == 13)
        {
            script.gameObject.layer = 6;
        }
        else if(gun.layer == 14)
        {
            script.gameObject.layer = 7;
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
            else if (gun.layer == 14)
            {
                rend.material.color = new Color(origin.r, origin.g, origin.b, .2f);
            }
        }
        script.coroutines_running--;
        yield return null;
    }
    
    protected override string ClassGunAbilityNames(int index)
    {
        return gun_ability_names[index];
    }

    protected override string GunAbilityDesc(int index)
    {
        return gun_ability_desc[index];
    }

    public override Gun_Abilities ClassGunMods(int index)
    {
        return Gun_Mods[index];
    }

    protected override void SetGunNameAddons(int index) 
    {
        Debug.Log(index);
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
        return "Launches a powerful arrow.";
    }

    public override string GetBaseName()
    {
        return "Strike";
    }

    public override void SetBaseStats()
    {
        upper_bound_damage = 15;
        lower_bound_damage = 7;
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
        range = 10f;
        projectile_speed = 5;
        knockback_power = 5;
        crit_chance = .05;
        reload_time = 1f;
        home_speed = 2.5f;
        home_radius = 3f;
        homes = true;
        /*Resources.Load seems to only work for getting prefabs as only game objects.*/
        Bullet = Resources.Load("Bullet") as GameObject;
    }

    public override string GetImagePreviewString()
    {
        return "StrikeImage";
    }



}
