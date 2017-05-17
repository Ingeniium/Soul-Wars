using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Flurry : Gun
{
    [SyncVar] public int num_bullets = 3;
    private readonly static string[] gun_ability_names = new string[3] { "Hunter", "Archer", null };
    private readonly static string[] gun_name_addons = new string[3] { null, null, null };
    /*This class's pool of gun_abilities.Use of a static container of static methods requiring explicit this
     pointers are used for onetime,pre-Awake() initialization of delegates*/

    private static List<Gun_Abilities> Gun_Mods = new List<Gun_Abilities>()//This class's pool of gun_abilities
    {
        null,
        null,
        null
    };

    public override void Shoot()
    {
        base.Shoot();
        for (int i = 1; i < num_bullets; i++)
        {
            Quaternion rot;
            Vector3 pos;
            if (i % 2 != 0)
            {
                rot = GetComponentsInChildren<Transform>()[1].rotation;
                pos = GetComponentsInChildren<Transform>()[1].position;
            }
            else
            {
                rot = GetComponentsInChildren<Transform>()[2].rotation;
                pos = GetComponentsInChildren<Transform>()[2].position;
            }
            bullet = Instantiate(Bullet, pos, rot) as GameObject;
            NetworkServer.SpawnWithClientAuthority(bullet, client_user.connectionToClient);
            ReadyWeaponForFire(ref bullet);
            bullet.GetComponent<Rigidbody>().AddForce(bullet.transform.forward * projectile_speed, ForceMode.Impulse);
        }
    }

    public override void Shoot(GameObject g)
    {
        base.Shoot(g);
        for (int i = 1; i < num_bullets; i++)
        {
            Quaternion rot;
            Vector3 pos;
            if (i % 2 != 0)
            {
                rot = GetComponentsInChildren<Transform>()[1].rotation;
                pos = GetComponentsInChildren<Transform>()[1].position;
            }
            else
            {
                rot = GetComponentsInChildren<Transform>()[2].rotation;
                pos = GetComponentsInChildren<Transform>()[2].position;
            }
            bullet = Instantiate(Bullet, pos, rot) as GameObject;
            NetworkServer.Spawn(bullet);
            ReadyWeaponForFire(ref bullet);
            bullet.GetComponent<Rigidbody>().AddForce(bullet.transform.forward * projectile_speed, ForceMode.Impulse);
        }
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
        return "Shoots 3 homing arrows.";
    }

    protected override string GetBaseName()
    {
        return "Flurry";
    }

    protected override void SetBaseStats()
    {
        upper_bound_damage = 7;
        lower_bound_damage = 4;
        asset_reference = Resources.Load("Flurry") as GameObject;
        layer = 13;
        home_layer = 10;
        color = new Color(43, 179, 234);
        range = 5;
        projectile_speed = 5;
        knockback_power = 5;
        crit_chance = .1;
        reload_time = .5f;
        home_speed = 5f;
        home_radius = 1.5f;
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
        return "FlurryImage";
    }

}
