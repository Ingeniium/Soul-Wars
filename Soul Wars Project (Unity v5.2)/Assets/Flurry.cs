using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Flurry : Gun
{
    [SyncVar] public int num_bullets = 3;
    public List<ValueGroup[]> TrioList = new List<ValueGroup[]>();
    bool targ_recorded;
    private readonly static string[] gun_ability_names = new string[3] { "Hunter", "Archer", null };
    private readonly static string[] gun_name_addons = new string[3] { null, null, null };
    private readonly static string[] gun_ability_desc = new string[3] {
        null,
        null,
        null
    };
    /*This class's pool of gun_abilities.Use of a static container of static methods requiring explicit this
     pointers are used for onetime,pre-Awake() initialization of delegates*/

    private static List<Gun_Abilities> Gun_Mods = new List<Gun_Abilities>()//This class's pool of gun_abilities
    {
        null,
        null,
        null
    };

    private static IEnumerator Hunter(Gun gun, BulletScript script)
    {
        script.coroutines_running++;
        Flurry Gun = gun as Flurry;
        while (script.has_collided == false && Gun.targ_recorded == false )//Wait for Collision
        {
            yield return new WaitForFixedUpdate();
        }
        if (script.legit_target == false)//Check if target even is valid
        {
            script.coroutines_running--;
            yield return null;
        }
        ValueGroup[] array = Gun.TrioList.Find(delegate(ValueGroup[] arr)
        {
            return (Array.Exists(arr,delegate(ValueGroup v)
            {
                return (v.index == script.netId.Value);
            }));
        });

        foreach (ValueGroup v in array)
        {
            if (v.index !=(int)script.netId.Value && v.index != 0)
            {
                
            }

        }
       
    }

    IEnumerator TrackBullet(GameObject g)
    {
        BulletScript script = g.GetComponent<BulletScript>();
        while (!script.has_collided)
        {
            yield return new WaitForEndOfFrame();
        }
        

    }

    public override void Shoot()
    {
        base.Shoot();
        TrioList.Add(new ValueGroup[num_bullets]);
        TrioList[TrioList.Count - 1][0].index = (int)bullet.GetComponent<NetworkIdentity>().netId.Value;
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
            NetworkServer.SpawnWithClientAuthority(bullet, client_user.connectionToClient);
            TrioList[TrioList.Count - 1][i].index = (int)bullet.GetComponent<NetworkIdentity>().netId.Value;
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
        range = 10;
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
