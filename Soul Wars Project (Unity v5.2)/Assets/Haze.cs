using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Haze : Gun
{
    private readonly static string[] gun_ability_names = new string[3] { "Fog", "Conflagration", null };
    private readonly static string[] gun_name_addons = new string[3] { "Fog", "Conflagration", null };
    private readonly static string[] gun_ability_desc = new string[3] {
        "Fog" + "\n Adds +5 Chill power to bullets.",
        "Conflagration" + "\n Adds +5 Burn power to bullets.",
        null
    };
    /*This class's pool of gun_abilities.Use of a static container of static methods requi"Markring explicit this
     pointers are used for onetime,pre-Awake() initialization of delegates*/
    private static List<Gun_Abilities> Gun_Mods = new List<Gun_Abilities>()//This class's pool of gun_abilities
    {
        Fog,
        Conflagration,
        null
    };

    private static IEnumerator Fog(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        script.chill_strength += .05;
        script.coroutines_running--;
        yield return null;
    }

    private static IEnumerator Conflagration(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        script.burn_strength += .05;
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

    protected override Gun_Abilities ClassGunMods(int index)
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

    protected override string GetBaseName()
    {
        return "Haze";
    }

    public override void SetBaseStats()
    {
        upper_bound_damage = 8;
        lower_bound_damage = 6;
        asset_reference = Resources.Load("Haze") as GameObject;
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
        GameObject g = Resources.Load("Drop Item Name Box") as GameObject;
        drop_canvas = g.GetComponent<Canvas>();
        g = Resources.Load("WeaponOptions") as GameObject;
        item_options = g.GetComponent<Canvas>();
        Bullet = Resources.Load("Cloud") as GameObject;
    }

    public override string GetImagePreviewString()
    {
        return "HazeImage";
    }

}
   