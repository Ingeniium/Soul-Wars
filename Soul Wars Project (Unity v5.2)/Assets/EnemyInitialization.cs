using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;



public class EnemyInitialization : NetworkBehaviour
{
    public int num_class_mods;
    public int num_unique_mods;
    public int num_misc_mods;
    public double class_mod_chance;
    public double unique_mod_chance;
    public double misc_mod_chance;

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
       {14,AddSunderResistance},
       {15,AddSunderStrength},
       {16,AddShieldDefence},
       {17,AddShieldHealth},
       {18,AddCriticalResistance},
    };

    private List<Action<AIController>> DummyMods = new List<Action<AIController>>()
    {
        {AddHealth},
        {AddDefence},
        {AddShieldDefence},
        {AddShieldHealth},  
        {AddBurnStrength}
    };

    private List<Action<AIController>> CadetMods = new List<Action<AIController>>()
    {
        {AddHealth},
        {AddProjectileSpeed},
        {AddPrecision},
        {AddShieldHealth},  
        {AddChillStrength}
    };

    private List<Action<AIController>> FighterMods = new List<Action<AIController>>()
    {
        {AddCritical},
        {AddPrecision},
        {AddPotentialDamage},
        {AddHealth},  
        {AddSunderStrength}
    };

    private List<Action<AIController>> AccursedMods = new List<Action<AIController>>()
    {
        {AddMezmerizeResistance},
        {AddMezmerizeStrength}
    };

    private List<Action<AIController>> MiscMods = new List<Action<AIController>>()
    {
        {AddLevel},
        {AddLevel},
        {AddExp},
        {AddBurnResistance},
        {AddSunderResistance},
        {AddChillResistance},
        {AddMezmerizeResistance},
        {AddCriticalResistance},
    };

    

    [ServerCallback]
     void Start()
    {
        GameObject Enemy;
        GameObject Weapon;
        GameObject Shield;
        GameObject[] Weapons;
        foreach (EnemyGroup e in GetComponents<EnemyGroup>())
        {
            Enemy = Instantiate(e.Enemy, e.pos, Quaternion.identity) as GameObject;
            Shield = Instantiate(Resources.Load("Bronze Shield"), e.pos, Quaternion.identity) as GameObject;
            AIController Unit = Enemy.GetComponentInChildren<AIController>();
            Unit.Shield = Shield;
            Gun gun;
            Weapons = new GameObject[e.Gun.Length];
            int i = 0;
            foreach(GameObject g in e.Gun)
            {
                Weapon = Instantiate(g, e.pos, Quaternion.identity) as GameObject;
                NetworkServer.Spawn(Weapon);
                gun = Weapon.GetComponent<Gun>();
                gun.SetBaseStats();
                gun.barrel_end = Weapon.transform.GetChild(0);
                Weapons[i] = Weapon;
                Unit.weapons[i] = gun;
                Unit.attack_func_indexes[i] = (int)e.AttackSettings[i];
                i++;
            }
            Unit.main_gun = Weapons[0].GetComponent<Gun>();
            Unit.GetComponentInParent<HealthDefence>().Controller = Unit;          
            Unit.movement_func_index = e.movement_type;
            NetworkServer.Spawn(Enemy);
            NetworkServer.Spawn(Shield);
            StartCoroutine(WaitForMethodsRef(Enemy,Weapons,Shield));
            
        }
    }

    [ServerCallback]
    IEnumerator WaitForMethodsRef(GameObject Enemy,GameObject[] Gun, GameObject Shield)
    {
        while (!NetworkMethods.Instance)
        {
            yield return new WaitForEndOfFrame();
        }
        int i = 0;
        foreach(GameObject g in Gun)
        {
            NetworkMethods.Instance.RpcSetParent(g, Enemy, new Vector3(.004f, .005f, .794f), new Quaternion(0, 0, 0, 0));
            if(i != 0)
            {
                NetworkMethods.Instance.RpcSetEnabled(g, "Renderer", false);
            }
            i++;
        }
        NetworkMethods.Instance.RpcSetParent(Shield, Enemy, new Vector3(.87f, .134f, 0), new Quaternion(0, 0, 0, 0));
        NetworkMethods.Instance.RpcSetLayer(Shield, 8);
        Shield.GetComponent<HealthDefence>().scale_factor = 3f;
        AIController Unit = Enemy.GetComponentInChildren<AIController>();
        switch (Enemy.ToString())
        {
            case "Dummy2(Clone) (UnityEngine.GameObject)":
                {
                    AssignDummyMods(Unit, class_mod_chance, num_class_mods);
                    AssignCadetMods(Unit, unique_mod_chance, num_unique_mods);
                    AssignFighterMods(Unit, unique_mod_chance, num_unique_mods);
                    AssignAccursedMods(Unit, unique_mod_chance, num_unique_mods);
                    break;
                }
            case "Cadet(Clone) (UnityEngine.GameObject)":
                {
                    AssignCadetMods(Unit, class_mod_chance, num_class_mods);
                    AssignDummyMods(Unit, unique_mod_chance, num_unique_mods);
                    AssignFighterMods(Unit, unique_mod_chance, num_unique_mods);
                    AssignAccursedMods(Unit, unique_mod_chance, num_unique_mods);
                    break;
                }
            case "Fighter(Clone) (UnityEngine.GameObject)":
                {
                    AssignFighterMods(Unit, class_mod_chance, num_class_mods);
                    AssignDummyMods(Unit, unique_mod_chance, num_unique_mods);
                    AssignCadetMods(Unit, unique_mod_chance, num_unique_mods);
                    AssignAccursedMods(Unit, unique_mod_chance, num_unique_mods);
                    break;
                }
            case "Accursed(Clone) (UnityEngine.GameObject)":
                {
                    AssignAccursedMods(Unit, class_mod_chance, num_class_mods);
                    AssignDummyMods(Unit, unique_mod_chance, num_unique_mods);
                    AssignFighterMods(Unit, unique_mod_chance, num_unique_mods);
                    AssignCadetMods(Unit, unique_mod_chance, num_unique_mods);
                    break;
                }
        }
        AssignMiscMods(Unit);
    }


    void AssignMiscMods(AIController Unit)
    {
        
        for (int i = 0; i < num_misc_mods; i++)
        {
            if (rand.NextDouble() < misc_mod_chance)
            {
                int n = rand.Next(MiscMods.Count - 1);
                MiscMods[n](Unit);
            }
        }
        ModDisplay display = Unit.GetComponentInParent<ModDisplay>();
        if (display.Mods.Count == 0)
        {
            display.Mods.Add("\r\n No Mods");
        }
    }

    void AssignDummyMods(AIController Unit,double mod_chance,int num_mods)
    {

        for (int i = 0; i < num_mods; i++)
        {
            if (rand.NextDouble() < mod_chance)
            {
                int n = rand.Next(DummyMods.Count - 1);
                DummyMods[n](Unit);
            }
        }
    }

    void AssignCadetMods(AIController Unit, double mod_chance, int num_mods)
    {

        for (int i = 0; i < num_mods; i++)
        {
            if (rand.NextDouble() < mod_chance)
            {
                int n = rand.Next(CadetMods.Count - 1);
                CadetMods[n](Unit);
            }
        }
    }

    void AssignFighterMods(AIController Unit, double mod_chance, int num_mods)
    {

        for (int i = 0; i < num_mods; i++)
        {
            if (rand.NextDouble() < mod_chance)
            {
                int n = rand.Next(FighterMods.Count - 1);
                FighterMods[n](Unit);
            }
        }
    }

    void AssignAccursedMods(AIController Unit, double mod_chance, int num_mods)
    {

        for (int i = 0; i < num_mods; i++)
        {
            if (rand.NextDouble() < mod_chance)
            {
                int n = rand.Next(AccursedMods.Count - 1);
                AccursedMods[n](Unit);
            }
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

    private static void AddSunderResistance(AIController AI)
    {
        HealthDefence HP = AI.GetComponentInParent<HealthDefence>();
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        if (HP.sunder_resistance < .03)
        {
            display.Mods.Add("\r\n Weak Sunder Resistance");
        }
        else if (HP.sunder_resistance < .06)
        {
            display.Mods.Add("\r\n Moderate Sunder Resistance");
            display.Mods.Remove("\r\n Weak Sunder Resistance");
        }
        else if (HP.sunder_resistance < .09)
        {
            display.Mods.Add("\r\n Strong Sunder Resistance");
            display.Mods.Remove("\r\n Moderate Sunder Resistance");
        }
        else if (HP.sunder_resistance < .12)
        {
            display.Mods.Add("\r\n Great Sunder Resistance");
            display.Mods.Remove("\r\n Strong Sunder Resistance");
        }
        HP.sunder_resistance += .03;

    }


    private static void AddCriticalResistance(AIController AI)
    {
        HealthDefence HP = AI.GetComponentInParent<HealthDefence>();
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        if (HP.crit_resistance < .03)
        {
            display.Mods.Add("\r\n Weak Critical Resistance");
        }
        else if (HP.crit_resistance < .06)
        {
            display.Mods.Add("\r\n Moderate Critical Resistance");
            display.Mods.Remove("\r\n Weak Critical Resistance");
        }
        else if (HP.crit_resistance < .09)
        {
            display.Mods.Add("\r\n Strong Critical Resistance");
            display.Mods.Remove("\r\n Moderate Critical Resistance");
        }
        else if (HP.crit_resistance < .12)
        {
            display.Mods.Add("\r\n Great Critical Resistance");
            display.Mods.Remove("\r\n Strong Critical Resistance");
        }
        HP.crit_resistance += .03;

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
        Gun gun = AI.main_gun;
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
		foreach (Gun g in AI.weapons) 
		{
			g.chill_strength += .03;
		}

    }

    private static void AddSunderStrength(AIController AI)
    {
        Gun gun = AI.main_gun;
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        if (gun.sunder_strength < .03)
        {
            display.Mods.Add("\r\n Weak Sunder Strength");
        }
        else if (gun.sunder_strength < .06)
        {
            display.Mods.Add("\r\n Moderate Sunder Strength");
            display.Mods.Remove("\r\n Weak Sunder Strength");
        }
        else if (gun.sunder_strength < .09)
        {
            display.Mods.Add("\r\n Strong Sunder Strength");
            display.Mods.Remove("\r\n Moderate Sunder Strength");
        }
        else if (gun.sunder_strength < .12)
        {
            display.Mods.Add("\r\n Great Sunder Strength");
            display.Mods.Remove("\r\n Strong Sunder Strength");
        }
        foreach (Gun g in AI.weapons)
        {
            g.sunder_strength += .03;
        }

    }

    private static void AddBurnStrength(AIController AI)
    {
        Gun gun = AI.main_gun;
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
        foreach (Gun g in AI.weapons)
        {
            g.burn_strength += .03;
        }
    }

    private static void AddMezmerizeStrength(AIController AI)
    {
        Gun gun = AI.main_gun;
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
        foreach (Gun g in AI.weapons)
        {
            g.mezmerize_strength += .03;
        }
    }

            

    private static void AddPrecision(AIController AI)
    {
        Gun gun = AI.main_gun;
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        int num = gun.upper_bound_damage - gun.lower_bound_damage;
        if (num > 1)
        {
            display.Mods.Add("\r\n Precise");
            foreach (Gun g in AI.weapons)
            {
                g.lower_bound_damage += (int)((float)num * .5f);
                if (g.lower_bound_damage > g.upper_bound_damage)
                {
                    g.lower_bound_damage = g.upper_bound_damage;
                }
            }
        }
        else
        {
            if(!display.Mods.Contains("\r\n Very Precise"))
            {
               display.Mods.Add("\r\n Very Precise");
               display.Mods.Remove("\r\n Precise");
                foreach (Gun g in AI.weapons)
                {
                    g.lower_bound_damage = g.upper_bound_damage;
                }
            }
        }
    }

    private static void AddPotentialDamage(AIController AI)
    {
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();

        foreach (Gun g in AI.weapons)
        {
            g.upper_bound_damage += 2;
        }
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
      
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        foreach (Gun g in AI.weapons)
        {
            g.projectile_speed++;
        }
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
       
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        int n = rand.Next(1);
        foreach (Gun g in AI.weapons)
        {
            if (!g.claimed_gun_ability.Contains(n)
                && g.ClassGunMods(n) != null)
            {
                g.level += 1;
                g.Claimed_Gun_Mods += g.ClassGunMods(n);
                g.claimed_gun_ability.Add(n);
            }
        }
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

    private static void AddShieldDefence(AIController AI)
    {
        HealthDefence HP = AI.Shield.GetComponent<HealthDefence>();
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        HP.defence += 2;
        if (display.Mods.Contains("\r\n Weak Shield Defence Bonus"))
        {
            display.Mods.Add("\r\n Moderate Shield Defence Bonus");
            display.Mods.Remove("\r\n Weak Shield Defence Bonus");
        }
        else if (display.Mods.Contains("\r\n Moderate Shield Defence Bonus"))
        {
            display.Mods.Add("\r\n Strong Shield Defence Bonus");
            display.Mods.Remove("\r\n Moderate Shield Defence Bonus");
        }
        else if (display.Mods.Contains("\r\n Strong Shield Defence Bonus"))
        {
            display.Mods.Add("\r\n Great Shield Defence Bonus");
            display.Mods.Remove("\r\n Strong Shield Defence Bonus");
        }
        else if (!display.Mods.Contains("\r\n Great Shield Defence Bonus"))
        {
            display.Mods.Add("\r\n Weak Shield Defence Bonus");
        }
    }

   private static void AddShieldHealth(AIController AI)
    {
        HealthDefence HP = AI.Shield.GetComponent<HealthDefence>();
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        HP.maxHP += (int)((float)HP.maxHP *.3f);
         if(HP.maxHP > HP.HP)
         {
             HP.HP = HP.maxHP;
         }
        if (display.Mods.Contains("\r\n Weak Shield Health Bonus"))
        {
            display.Mods.Add("\r\n Moderate Shield Health Bonus");
            display.Mods.Remove("\r\n Weak Shield Health Bonus");
        }
        else if (display.Mods.Contains("\r\n Moderate Shield Health Bonus"))
        {
            display.Mods.Add("\r\n Strong Shield Health Bonus");
            display.Mods.Remove("\r\n Moderate Shield Health Bonus");
        }
        else if (display.Mods.Contains("\r\n Strong Shield Health Bonus"))
        {
            display.Mods.Add("\r\n Great Shield Health Bonus");
            display.Mods.Remove("\r\n Strong Shield Health Bonus");
        }
        else if (!display.Mods.Contains("\r\n Great Shield Health Bonus"))
        {
            display.Mods.Add("\r\n Weak Shield Health Bonus");
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
        ModDisplay display = AI.GetComponentInParent<ModDisplay>();
        foreach (Gun g in AI.weapons)
        {
            g.crit_chance += .03;
        }
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
           ModDisplay display = AI.GetComponentInParent<ModDisplay>();
          foreach (Gun g in AI.weapons)
          {
            if (g.reload_time <= .5f)
            {
                AI.shoot_delay -= .25f;
            }
            else
            {
                g.reload_time -= .25f;
            }
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
