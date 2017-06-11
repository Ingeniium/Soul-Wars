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
    private readonly static string[] gun_name_addons = new string[3] { "Precision", "Archery", null };
    private readonly static string[] gun_ability_desc = new string[3] {
        "Hunter" + "\n Causes bullets that aren't" + "\n homing in on a target to" + "\n to reroute to another flurry " + "\n bullet's target.",
        "Archer" + "\n Bullets have a 30%" + "\n chance to penetrate its target.",
        null
    };
    /*This class's pool of gun_abilities.Use of a static container of static methods requiring explicit this
     pointers are used for onetime,pre-Awake() initialization of delegates*/

    private static List<Gun_Abilities> Gun_Mods = new List<Gun_Abilities>()//This class's pool of gun_abilities
    {
        Hunter,
        Archer,
        null
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
        string layer = null;
        if (script.gameObject.layer == 13)
        {
            layer = "AllyAttack";
        }
        else
        {
            layer = "EnemyAttack";
        }
        Collider[] bullet_colliders = Physics.OverlapSphere(script.gameObject.transform.position, 10,
            LayerMask.GetMask(layer), QueryTriggerInteraction.Collide);
        foreach (Collider col in bullet_colliders)
        {
            /*Check if its a flurry bullet that isn't isn't homing*/
            BulletScript b = col.GetComponent<BulletScript>();
            if (b && b.gun_reference is Flurry && !b.homer.GetComponent<HomingScript>().homing)
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
        if (rand.NextDouble() < .30)
        {
            script.can_pierce = true;
        }
        script.coroutines_running--;
        yield return null;
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
        return (GunTable.buttons[0].method == Hunter);
    }

    protected override string GunDesc()
    {
        return "Shoots 3 homing arrows.";
    }

    protected override string GetBaseName()
    {
        return "Flurry";
    }

    public override void SetBaseStats()
    {
        upper_bound_damage = 7;
        lower_bound_damage = 4;
        asset_reference = Resources.Load("Flurry") as GameObject;
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
        projectile_speed = 5;
        knockback_power = 5;
        crit_chance = .05;
        reload_time = 1f;
        home_speed = 1.5f;
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
        return "FlurryImage";
    }

}
