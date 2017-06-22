using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;



public class EnemyInitialization : NetworkBehaviour
{
    public int num_mods;
    public int mod_chance;
 
    static System.Random rand = new System.Random();
    /*With each roll,you have a 60% chance of an enemy getting a mod*/
    private static Dictionary<int, Action<AIController>> Mods = new Dictionary<int, Action<AIController>>()
    {
       {0,AddChillResistance},
       {1,AddBurnResistance},
       {2,AddMezmerizeResistance},
       {3,AddMezmerizeStrength},
       {4,AddPotentialDamage},
       {5,AddDefence},
       {6,AddCritical},
       {7,AddExp},
       {8,AddHealth},
       {9,AddPrecision},
       {10,AddBurnStrength},
       {11,AddChillStrength},
       {12,AddAttackSpeed},
       {13,AddProjectileSpeed},
       {14,AddLevel},
       {15,AddLevel},
       {16,AddLevel},
    };

    [ServerCallback]
    void Awake()
    {
        if (mod_chance > 0)
        {
            int num = (1700 / mod_chance) - Mods.Count;
            for(int i = 0;i < num;i++)
            {
                Mods.Add(Mods.Count, null);
            }
        }
    }

    [ServerCallback]
     void Start()
    {
        GameObject Enemy;
        GameObject Weapon;
        GameObject Shield;
        foreach (EnemyGroup e in GetComponents<EnemyGroup>())
        {
            Enemy = Instantiate(e.Enemy, e.pos, Quaternion.identity) as GameObject;
            Weapon = Instantiate(e.Gun, e.pos, Quaternion.identity) as GameObject;
            Shield = Instantiate(Resources.Load("Bronze Shield"), e.pos, Quaternion.identity) as GameObject;
            AIController Unit = Enemy.GetComponentInChildren<AIController>();
            Unit.Shield = Shield;
            Unit.Gun = Weapon;
            Unit.gtr = Weapon.GetComponent<Transform>();
            Unit.gun = Weapon.GetComponent<Gun>();
            Unit.GetComponentInParent<HealthDefence>().Controller = Unit;
            Unit.gun.SetBaseStats();
            Unit.gun.barrel_end = Weapon.transform.GetChild(0);
            Unit.shoot_delay = Unit.shoot_constant / (Unit.gun.reload_time * 4) - Unit.gun.reload_time;
            NetworkServer.Spawn(Enemy);
            NetworkServer.Spawn(Weapon);
            NetworkServer.Spawn(Shield);
            StartCoroutine(WaitForMethodsRef(Enemy,Weapon,Shield));
            AssignRandomMods(Unit);
        }
    }

    [ServerCallback]
    IEnumerator WaitForMethodsRef(GameObject Enemy,GameObject Weapon,GameObject Shield)
    {
        while (!NetworkMethods.Instance)
        {
            yield return new WaitForEndOfFrame();
        }
        NetworkMethods.Instance.RpcSetParent(Weapon, Enemy, new Vector3(.004f, .005f, .794f), new Quaternion(0, 0, 0, 0));
        NetworkMethods.Instance.RpcSetParent(Shield, Enemy, new Vector3(.87f, .134f, 0), new Quaternion(0, 0, 0, 0));
        Shield.GetComponent<HealthDefence>().scale_factor = 3f;
    }

    void AssignRandomMods(AIController Unit)
    {
        for (int i = 0; i < num_mods; i++)
        {
           int n = rand.Next(Mods.Count - 1);
           Mods[n](Unit); 
        }
        ModDisplay display = Unit.GetComponentInParent<ModDisplay>();
        if (display.Mods.Count == 0)
        {
            display.Mods.Add("\r\n No Mods");
        }
       
    }

    private static void AddChillResistance(AIController AI)
    {
        HealthDefence HP = AI.GetComponentInParent<HealthDefence>();
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        if (HP.chill_resistance < .03)
        {
            display.Mods.Add("\r\n Weak Chill Resistance");
        }
        else if (HP.chill_resistance < .06)
        {
            display.Mods.Add("\r\n Moderate Chill Resistance");
            display.Mods.Remove("\r\n Weak Chill Resistance");
        }
        else if (HP.chill_resistance < .09)
        {
            display.Mods.Add("\r\n Strong Chill Resistance");
            display.Mods.Remove("\r\n Moderate Chill Resistance");
        }
        else if (HP.chill_resistance < .12)
        {
            display.Mods.Add("\r\n Great Chill Resistance");
            display.Mods.Remove("\r\n Strong Chill Resistance");
        }
        HP.chill_resistance += .03;
        
    }

    private static void AddBurnResistance(AIController AI)
    {
        HealthDefence HP = AI.GetComponentInParent<HealthDefence>();
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        if (HP.burn_resistance < .03)
        {
            display.Mods.Add("\r\n Weak Burn Resistance");
        }
        else if (HP.burn_resistance < .06)
        {
            display.Mods.Add("\r\n Moderate Burn Resistance");
            display.Mods.Remove("\r\n Weak Burn Resistance");
        }
        else if (HP.burn_resistance < .09)
        {
            display.Mods.Add("\r\n Strong Burn Resistance");
            display.Mods.Remove("\r\n Moderate Burn Resistance");
        }
        else if (HP.burn_resistance < .12)
        {
            display.Mods.Add("\r\n Great Burn Resistance");
            display.Mods.Remove("\r\n Strong Burn Resistance");
        }
        HP.burn_resistance += .03;
    }

    private static void AddMezmerizeResistance(AIController AI)
    {
        HealthDefence HP = AI.GetComponentInParent<HealthDefence>();
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        if (HP.mezmerize_resistance < .03)
        {
            display.Mods.Add("\r\n Weak Mezmerize Resistance");
        }
        else if (HP.mezmerize_resistance < .06)
        {
            display.Mods.Add("\r\n Moderate Mezmerize Resistance");
            display.Mods.Remove("\r\n Weak Mezmerize Resistance");
        }
        else if (HP.mezmerize_resistance < .09)
        {
            display.Mods.Add("\r\n Strong Mezmerize Resistance");
            display.Mods.Remove("\r\n Moderate Mezmerize Resistance");
        }
        else if (HP.mezmerize_resistance < .12)
        {
            display.Mods.Add("\r\n Great Mezmerize Resistance");
            display.Mods.Remove("\r\n Strong Mezmerize Resistance");
        }
        HP.mezmerize_resistance += .03;
    }


    private static void AddChillStrength(AIController AI)
    {
        Gun gun = AI.gun;
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        if (gun.chill_strength < .03)
        {
            display.Mods.Add("\r\n Weak Chill Strength");
        }
        else if (gun.chill_strength < .06)
        {
            display.Mods.Add("\r\n Moderate Chill Strength");
            display.Mods.Remove("\r\n Weak Chill Strength");
        }
        else if (gun.chill_strength < .09)
        {
            display.Mods.Add("\r\n Strong Chill Strength");
            display.Mods.Remove("\r\n Moderate Chill Strength");
        }
        else if (gun.chill_strength < .12)
        {
            display.Mods.Add("\r\n Great Chill Strength");
            display.Mods.Remove("\r\n Strong Chill Strength");
        }
        gun.chill_strength += .03;

    }

    private static void AddBurnStrength(AIController AI)
    {
        Gun gun = AI.gun;
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        if (gun.burn_strength < .03)
        {
            display.Mods.Add("\r\n Weak Burn Strength");
        }
        else if (gun.burn_strength < .06)
        {
            display.Mods.Add("\r\n Moderate Burn Strength");
            display.Mods.Remove("\r\n Weak Burn Strength");
        }
        else if (gun.burn_strength < .09)
        {
            display.Mods.Add("\r\n Strong Burn Strength");
            display.Mods.Remove("\r\n Moderate Burn Strength");
        }
        else if (gun.burn_strength < .12)
        {
            display.Mods.Add("\r\n Great Burn Strength");
            display.Mods.Remove("\r\n Strong Burn Strength");
        }
        gun.burn_strength += .03;
    }

    private static void AddMezmerizeStrength(AIController AI)
    {
        Gun gun = AI.gun;
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        if (gun.mezmerize_strength < .03)
        {
            display.Mods.Add("\r\n Weak Mezmerize Strength");
        }
        else if (gun.mezmerize_strength < .06)
        {
            display.Mods.Add("\r\n Moderate Mezmerize Strength");
            display.Mods.Remove("\r\n Weak Mezmerize Strength");
        }
        else if (gun.mezmerize_strength < .09)
        {
            display.Mods.Add("\r\n Strong Mezmerize Strength");
            display.Mods.Remove("\r\n Moderate Mezmerize Strength");
        }
        else if (gun.mezmerize_strength < .12)
        {
            display.Mods.Add("\r\n Great Mezmerize Strength");
            display.Mods.Remove("\r\n Strong Mezmerize Strength");
        }
        gun.mezmerize_strength += .03;
    }

            

    private static void AddPrecision(AIController AI)
    {
        Gun gun = AI.gun;
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        int num = gun.upper_bound_damage - gun.lower_bound_damage;
        if (num > 1)
        {
            display.Mods.Add("\r\n Precise");
            gun.lower_bound_damage += (int)((float)num * .5f);
            if (gun.lower_bound_damage > gun.upper_bound_damage)
            {
                gun.lower_bound_damage = gun.upper_bound_damage;
            }
        }
        else
        {
            if(!display.Mods.Contains("\r\n Very Precise"))
            {
               display.Mods.Add("\r\n Very Precise");
               display.Mods.Remove("\r\n Precise");
               gun.lower_bound_damage = gun.upper_bound_damage;
            }
        }
    }

    private static void AddPotentialDamage(AIController AI)
    {
        Gun gun = AI.gun;
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        gun.upper_bound_damage += 2;
        if (display.Mods.Contains("\r\n Weak Potential Damage Bonus"))
        {
            display.Mods.Add("\r\n Moderate Potential Damage Bonus");
            display.Mods.Remove("\r\n Weak Potential Damage Bonus");
        }
        else if (display.Mods.Contains("\r\n Moderate Potential Damage Bonus"))
        {
            display.Mods.Add("\r\n Strong Potential Damage Bonus");
            display.Mods.Remove("\r\n Moderate Potential Damage Bonus");
        }
        else if (display.Mods.Contains("\r\n Strong Potential Damage Bonus"))
        {
            display.Mods.Add("\r\n Great Potential Damage Bonus");
            display.Mods.Remove("\r\n Strong Potential Damage Bonus");
        }
        else if(!display.Mods.Contains("\r\n Great Potential Damage Bonus"))
        {
            display.Mods.Add("\r\n Weak Potential Damage Bonus");
        }

    }

    private static void AddProjectileSpeed(AIController AI)
    {
        Gun gun = AI.gun;
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        gun.projectile_speed += 1;
        if (display.Mods.Contains("\r\n Weak Projectile Speed Bonus"))
        {
            display.Mods.Add("\r\n Moderate Projectile Speed Bonus");
            display.Mods.Remove("\r\n Weak Projectile Speed Bonus");
        }
        else if (display.Mods.Contains("\r\n Moderate Potential Damage Bonus"))
        {
            display.Mods.Add("\r\n Strong Projectile Speed Bonus");
            display.Mods.Remove("\r\n Moderate Projectile Speed Bonus");
        }
        else if (display.Mods.Contains("\r\n Strong Projectile Speed Bonus"))
        {
            display.Mods.Add("\r\n Great Projectile Speed Bonus");
            display.Mods.Remove("\r\n Strong Projectile Speed Bonus");
        }
        else if (!display.Mods.Contains("\r\n Great Projectile Speed Bonus"))
        {
            display.Mods.Add("\r\n Weak Projectile Speed Bonus");
        }
    }

    private static void AddLevel(AIController AI)
    {
        Gun gun = AI.gun;
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        int n = rand.Next(1);
        if (!gun.claimed_gun_ability.Contains(n))
        {
            gun.level += 1;
            gun.Claimed_Gun_Mods += gun.ClassGunMods(n);
            gun.claimed_gun_ability.Add(n);
            if (display.Mods.Contains("\r\n Level One Weapon"))
            {
                display.Mods.Add("\r\n Level Two Weapon");
                display.Mods.Remove("\r\n Level One Weapon");
            }
            else if (display.Mods.Contains("\r\n Level Two Weapon"))
            {
                display.Mods.Add("\r\n Level Three Weapon");
                display.Mods.Remove("\r\n Level Two Weapon");
            }
            else if (display.Mods.Contains("\r\n Level Three Weapon"))
            {
                display.Mods.Add("\r\n Level Four Weapon");
                display.Mods.Remove("\r\n Level Three Weapon");
            }
            else if (!display.Mods.Contains("\r\n Level Four Weapon"))
            {
                display.Mods.Add("\r\n Level One Weapon");
            }
        }
    }

    private static void AddDefence(AIController AI)
    {
        HealthDefence HP = AI.GetComponentInParent<HealthDefence>();
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        HP.defence += 1;
        if (display.Mods.Contains("\r\n Weak Defence Bonus"))
        {
            display.Mods.Add("\r\n Moderate Defence  Bonus");
            display.Mods.Remove("\r\n Weak Defence Bonus");
        }
        else if (display.Mods.Contains("\r\n Moderate Defence Bonus"))
        {
            display.Mods.Add("\r\n Strong Defence Bonus");
            display.Mods.Remove("\r\n Moderate Defence Bonus");
        }
        else if (display.Mods.Contains("\r\n Strong Defence Bonus"))
        {
            display.Mods.Add("\r\n Great Defence Bonus");
            display.Mods.Remove("\r\n Strong Defence Bonus");
        }
        else if (!display.Mods.Contains("\r\n Great Defence Bonus"))
        {
            display.Mods.Add("\r\n Weak Defence Bonus");
        }
    }

     private static void AddHealth(AIController AI)
    {
        HealthDefence HP = AI.GetComponentInParent<HealthDefence>();
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        HP.maxHP += (int)((float)HP.maxHP *.2f);
         if(HP.maxHP > HP.HP)
         {
             HP.HP = HP.maxHP;
         }
        if (display.Mods.Contains("\r\n Weak Health Bonus"))
        {
            display.Mods.Add("\r\n Moderate Health Bonus");
            display.Mods.Remove("\r\n Weak Health Bonus");
        }
        else if (display.Mods.Contains("\r\n Moderate Health Bonus"))
        {
            display.Mods.Add("\r\n Strong Health Bonus");
            display.Mods.Remove("\r\n Moderate Health Bonus");
        }
        else if (display.Mods.Contains("\r\n Strong Health Bonus"))
        {
            display.Mods.Add("\r\n Great Health Bonus");
            display.Mods.Remove("\r\n Strong Health Bonus");
        }
        else if (!display.Mods.Contains("\r\n Great Health Bonus"))
        {
            display.Mods.Add("\r\n Weak Health Bonus");
        }
    }

     private static void AddExp(AIController AI)
     {
         HealthDefence HP = AI.GetComponentInParent<HealthDefence>();
         ModDisplay display = AI.GetComponentInParent<ModDisplay>();
         HP.exp_rate += .1f;
         if (display.Mods.Contains("\r\n Weak Experience Bonus"))
         {
             display.Mods.Add("\r\n Moderate Experience Bonus");
             display.Mods.Remove("\r\n Weak Experience Bonus");
         }
         else if (display.Mods.Contains("\r\n Moderate Experience Bonus"))
         {
             display.Mods.Add("\r\n Strong Experience Bonus");
             display.Mods.Remove("\r\n Moderate Experience Bonus");
         }
         else if (display.Mods.Contains("\r\n Strong Experience Bonus"))
         {
             display.Mods.Add("\r\n Great Experience Bonus");
             display.Mods.Remove("\r\n Strong Experience Bonus");
         }
         else if (!display.Mods.Contains("\r\n Great Experience Bonus"))
         {
             display.Mods.Add("\r\n Weak Experience Bonus");
         }
     }

       private static void AddCritical(AIController AI)
     {
        Gun gun = AI.gun;
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        gun.crit_chance += .03;
        if (display.Mods.Contains("\r\n Weak Critical Chance Bonus"))
        {
            display.Mods.Add("\r\n Moderate Critical Chance Bonus");
            display.Mods.Remove("\r\n Weak Critical Chance Bonus");
        }
        else if (display.Mods.Contains("\r\n Moderate Critical Chance Bonus"))
        {
            display.Mods.Add("\r\n Strong Critical Chance Bonus");
            display.Mods.Remove("\r\n Moderate Critical Chance Bonus");
        }
        else if (display.Mods.Contains("\r\n Strong Critical Chance Bonus"))
        {
            display.Mods.Add("\r\n Great Critical Chance Bonus");
            display.Mods.Remove("\r\n Strong Critical Chance Bonus");
        }
        else if (!display.Mods.Contains("\r\n Great Critical Chance Bonus"))
        {
            display.Mods.Add("\r\n Weak Critical Chance Bonus");
        }

    }

       private static void AddAttackSpeed(AIController AI)
       {
           Gun gun = AI.gun;
           ModDisplay display = AI.GetComponentInParent<ModDisplay>();
           if (gun.reload_time <= .5f)
           {
               AI.shoot_delay -= .25f;
           }
           else
           {
               gun.reload_time -= .25f;
           }
           if (display.Mods.Contains("\r\n Weak Attack Speed Bonus"))
           {
               display.Mods.Add("\r\n Moderate Attack Speed Bonus");
               display.Mods.Remove("\r\n Weak Attack Speed Bonus");
           }
           else if (display.Mods.Contains("\r\n Moderate Attack Speed Bonus"))
           {
               display.Mods.Add("\r\n Strong Attack Speed Bonus");
               display.Mods.Remove("\r\n Moderate Attack Speed Bonus");
           }
           else if (display.Mods.Contains("\r\n Strong Attack Speed Bonus"))
           {
               display.Mods.Add("\r\n Great Attack Speed Bonus");
               display.Mods.Remove("\r\n Strong Attack Speed Bonus");
           }
           else if (!display.Mods.Contains("\r\n Great Attack Speed Bonus"))
           {
               display.Mods.Add("\r\n Weak Attack Speed Bonus");
           }

       }

      

 




}
