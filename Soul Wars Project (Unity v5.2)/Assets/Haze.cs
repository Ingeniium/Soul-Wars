using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Haze : Gun
{
    private readonly static string[] gun_ability_names = new string[12] { "Fog", "Conflagration", null,
                                                                         null,"Fume",null,
                                                                         null,"Engulf",null,
                                                                         null,"Cloud",null};
    private readonly static string[] gun_name_addons = new string[12] { "Fog", "Conflagration", null,
                                                                        null, "Toxic",null,
                                                                        null, "Ominous",null,
                                                                        null, "Cloudy", null};
    private readonly static string[] gun_ability_desc = new string[12] {
        "Fog" + "\n Adds +10 Chill strength to bullets.",
        "Conflagration" + "\n Adds +10 Burn strenght to bullets.",
        null,

        null,
        "Fume" + "\n Adds one to two points of damage" + "\n to the bullet upon hitting" + "\n new targets.",
        null,

        null,
        "Engulf" + "\n Causes bullets to stick to their first target.",
        null,

        null,
        "Cloud" + "\n Doubles the size of the bullets.",
        null
    };
    /*This class's pool of gun_abilities.Use of a static container of static methods requi"Markring explicit this
     pointers are used for onetime,pre-Awake() initialization of delegates*/
    private static List<Gun_Abilities> Gun_Mods = new List<Gun_Abilities>()//This class's pool of gun_abilities
    {
        Fog,
        Conflagration,
        null,

        null,
        Fume,
        null,

        null,
        Engulf,
        null,

        null,
        Cloud,
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
            while(script)
            {
                if(script.Target && !prevTargs.Exists(delegate(HealthDefence h)
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

    private static IEnumerator Engulf(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        while (!script.Target)
        {
            yield return new WaitForEndOfFrame();
        }
        HealthDefence curTarg = script.Target;
        script.coroutines_running--;
        while (script)
        {
            NetworkMethods.Instance.RpcSetPosition(script.gameObject,new Vector3(curTarg.transform.position.x,script.transform.position.y,curTarg.transform.position.z));
            yield return new WaitForSeconds(.2f);
        }
    }

    private static IEnumerator Cloud(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        BoxCollider collider = script.gameObject.GetComponent<BoxCollider>();
        collider.size = new Vector3(collider.size.x, collider.size.y * .5f, collider.size.z);
        NetworkMethods.Instance.RpcSetScale(script.gameObject,new Vector3(4,4,4));
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
            color = Color.red;
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
   