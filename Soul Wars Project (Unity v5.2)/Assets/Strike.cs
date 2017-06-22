using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Strike : Gun {
    private readonly static string[] gun_ability_names = new string[12] { "Marksman", "Sniper", null,
                                                                         null, null, null,
                                                                         null, null, null,
                                                                         null, null, null};
    private readonly static string[] gun_name_addons = new string[12] { "Marksmanship","Accuracy",null,
                                                                          null, null, null,
                                                                         null, null, null,
                                                                         null, null, null};
    private readonly static string[] gun_ability_desc = new string[12] {
        "Marksman" + "\n Can grant up to" + "\n 30% crit chance based on" + "\n how little the bullet turns.",
        "Sniper" + "\n Adds a seconds of" + "\n cooldown to the enemy's" + "\n current gun.",
         null,

        null,
        null,
        null,

        null,
        null,
        null,

        null,
        null,
        null
    };
    /*This class's pool of gun_abilities.Use of a static container of static methods requiring explicit this
     pointers are used for onetime,pre-Awake() initialization of delegates*/
    private static List<Gun_Abilities> Gun_Mods = new List<Gun_Abilities>()//This class's pool of gun_abilities
    {
        {Marksman},
        {Sniper},
         null,

        null,
        null,
        null,

        null,
        null,
        null,

        null,
        null,
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
            print(ang.ToString());
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
            Gun targ = script.Target.Controller.Gun.GetComponent<Gun>();
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

    protected override void SetGunNameAddons(int index)     {
        Debug.Log(index);
        if (index < 4 || index > 12)
        {
            suffixes.Add(gun_name_addons[index]);
        }
        else
        {
            prefixes.Add(gun_name_addons[index]);
        }    }

    protected override bool AreGunLevelUpButtonsAssignedForClass()
    {
        return (GunTable.buttons[0].method == Marksman);
    }

    protected override string GunDesc()
    {
        return "Launches a powerful arrow.";
    }

    protected override string GetBaseName()
    {
        return "Strike";
    }

    public override void SetBaseStats()
    {
        upper_bound_damage = 15;
        lower_bound_damage = 7;
        asset_reference = Resources.Load("Strike") as GameObject;
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
        GameObject g = Resources.Load("Drop Item Name Box") as GameObject;
        drop_canvas = g.GetComponent<Canvas>();
        g = Resources.Load("WeaponOptions") as GameObject;
        item_options = g.GetComponent<Canvas>();
        Bullet = Resources.Load("Bullet") as GameObject;
    }

    public override string GetImagePreviewString()
    {
        return "StrikeImage";
    }
}
