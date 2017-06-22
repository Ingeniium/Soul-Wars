using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Blaster : Gun
{
    private readonly static string[] gun_ability_names = new string[12] { "Brigadier", "Gunslinger", null,
                                                                         null, null, null,
                                                                         null, null, null,
                                                                         null, null, null,};                           
    private readonly static string[] gun_name_addons = new string[12] { "Punishment", "The Force", null,
                                                                         null, null, null,
                                                                         null, null, null,
                                                                         null, null, null,};
    private readonly static string[] gun_ability_desc = new string[12] {
        "Brigadier" + "\n Does 150% damage to" + "\n spawn points and shields.",
        "Gunslinger" + "\n Causes a 2 second stun.",
        null,

        null,
        null,
        null,

        null,
        null,
        null,

        null,
        null,
        null,
    };
    /*This class's pool of gun_abilities.Use of a static container of static methods requi"Markring explicit this
    pointers are used for onetime,pre-Awake() initialization of delegates*/
    private static List<Gun_Abilities> Gun_Mods = new List<Gun_Abilities>()//This class's pool of gun_abilities
    {
        Brigadier,
        Gunslinger,
         null,
        null,
        null,

        null,
        null,
        null,

        null,
        null,
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
        return (GunTable.buttons[0].method == Brigadier);
    }

    protected override string GunDesc()
    {
        return "Shoots a fast,large bullet";
    }

    protected override string GetBaseName()
    {
        return "Blaster";
    }

    public override void SetBaseStats()
    {
        upper_bound_damage = 35;
        asset_reference = Resources.Load("Blaster") as GameObject;
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
        GameObject g = Resources.Load("Drop Item Name Box") as GameObject;
        drop_canvas = g.GetComponent<Canvas>();
        g = Resources.Load("WeaponOptions") as GameObject;
        item_options = g.GetComponent<Canvas>();
        Bullet = Resources.Load("CanonBall") as GameObject;
    }

    public override string GetImagePreviewString()
    {
        return "BlasterImage";
    }
}