using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System;

public abstract partial class Gun : Item {
    public GameObject GunLevelUp;//Reference to the table used for getting gun abilities
    public GameObject weapons_bar;//Reference to the weapons bar
	public GameObject Bullet;
	public GameObject bullet;
    [SyncVar] public float reload_time;//How long it takes to fire each shot
	[SyncVar] public float next_time;//Next firing time
    [SyncVar] public float home_speed;//Angular turning speed of the bullet
	[SyncVar] public float home_radius;
    public bool homes = true;//whether the bullets will have homing capabilities
	[SyncVar] public int lower_bound_damage = 7;
    [SyncVar] public int upper_bound_damage = 15;
    [SyncVar] public float knockback_power;//How much knockback force and knockback stun is done 
    [SyncVar] public float projectile_speed;
    [SyncVar] public float range;
    [SyncVar] public double crit_chance = .1f;
    [SyncVar] public bool can_pierce;
    [SyncVar] public double chill_strength;
    [SyncVar] public double burn_strength;
    [SyncVar] public double mezmerize_strength;
    [SyncVar] public double sunder_strength;
    public Gun median;//for copy transfer
	public Transform barrel_end;//Where bullets actually SPAWN from
    [SyncVar] public Color color;//color assigned to the BULLETS
    public int layer;//Collision layer of the bullet
    public int home_layer;//Collision layer of the bullet's homing device
    [SyncVar] public int level = 0;//Level of the current gun
    [SyncVar] public int next_lvl = 40;//experience to next level
    protected static readonly int[] next_lvl_exp = new int[]
    {
        40,
        70,
        130,
        210,
        330
    };

    /*Note that level and experience is limited to one script and not entire class
     (scripts are copied,but value isn't static) to allow player option of choosing
     different gun abilities within same type of gun*/
    public int experience
    {
        get { return _experience; }
        set 
        {
            if (level >= 5)
            {
                return;
            }
            else if (value >= next_lvl)
            {
                _experience = 0;
                level += 1;
                if (level < 5)
                {
                    next_lvl = next_lvl_exp[level];
                }
                RpcLevelupIndication();
                points += 1;
            }
            else
            {
                _experience = value;
            }
        }
    }
    [SyncVar] public int _experience = 0;
    [SyncVar] public uint points = 0;//points to spend on abilities unlocked by gun level ups
    public int mez_threshold;
    protected Canvas level_up_indication;
    public List<int> claimed_gun_ability = new List<int>();//For determining which abilities have already been chosen
    public delegate IEnumerator Gun_Abilities(Gun gun,BulletScript script);
    public Gun_Abilities Claimed_Gun_Mods;//Abilities which have already been chosen
    /*Below are added to names of weapons as a result of getting abilities*/
    protected List<string> prefixes = new List<string>();
    protected List<string> suffixes = new List<string>();
    protected string last_suffix = "";
    protected string _button = "";
    public string button
    {
        get { return _button; }
        set
        {
            if (!buttons.Contains(value))
            {
                if (buttons.Contains(_button))
                {
                    buttons.Remove(_button);
                }
                _button = value;
                buttons.Add(_button);
                if (_button != "")
                {
                    item_image_show.GetComponentInChildren<ItemImage>().AddSetting("<color=white><size=7>" + _button + "</size></color>", 0);
                }
                else
                {
                     item_image_show.GetComponentInChildren<ItemImage>().AddSetting("<color=white><size=5>" + "Left \n" + " Mouse" + "</size></color>", 0); 
                }
            }
        }
    }
    public static List<String> buttons = new List<string>();   

    [ClientRpc]
    protected void RpcLevelupIndication()
    {
        if (client_user && PlayerController.Client.netId == client_user.netId && !level_up_indication)
        {
            level_up_indication = Instantiate(client_user.cooldown_canvas, item_image_show.transform.position + new Vector3(-.25f, 0, 0), client_user.cooldown_canvas.transform.rotation) as Canvas;
            level_up_indication.GetComponentInChildren<Text>().text = "!";
            level_up_indication.GetComponentInChildren<Text>().color = Color.green;
            level_up_indication.transform.SetParent(item_image_show.transform);
        }
    }

    protected override void OnClientUserChange()
    {
        weapons_bar = _client_user.hpbar_show.GetComponentInChildren<HorizontalLayoutGroup>().gameObject;
        GunLevelUp = UIHide.obj.gameObject;
    }

    

    public bool HasReloaded(float delay = 0)
    {
        return (Time.time > next_time + delay);       
    }

    [ClientRpc]
    protected void RpcSetLayer(GameObject weapon_fire)
    {
        if (weapon_fire)
        {
            BulletScript script = weapon_fire.GetComponent<BulletScript>();
            script.home.layer = home_layer;
            weapon_fire.GetComponent<Renderer>().material.color = new Color(color.r, color.g, color.b, weapon_fire.GetComponent<Renderer>().material.color.a);
            Renderer[] child_rends = weapon_fire.GetComponentsInChildren<Renderer>();
            if (child_rends != null)
            {
                foreach (Renderer r in child_rends)
                {
                    r.material.color = new Color(color.r, color.g, color.b, r.material.color.a);
                }
            }
        }
    }

    [ClientRpc]
    protected void RpcFire(Vector3 dir,GameObject weapon_fire)
    {
        if (weapon_fire)
        {
            weapon_fire.GetComponent<Rigidbody>().velocity = dir * projectile_speed; 
        }
        
    }
    
    
    public virtual void Shoot()
    {
        bullet = Instantiate(Bullet, barrel_end.position, barrel_end.rotation) as GameObject;
        NetworkServer.Spawn(bullet);
        ReadyWeaponForFire(ref bullet);
        RpcFire(barrel_end.forward,bullet);
    }

    public virtual void Shoot(GameObject g)
    {
        bullet = g;
        ReadyWeaponForFire(ref bullet);
        RpcFire(barrel_end.forward,g);
    }
   
    protected void ReadyWeaponForFire(ref GameObject weapon_fire)
    {

        RpcSetLayer(weapon_fire);
        BulletScript script = weapon_fire.GetComponent<BulletScript>();
        //Pass values on to bullet
        script.crit_chance = crit_chance;
        script.upper_bound_damage = upper_bound_damage;
        script.lower_bound_damage = lower_bound_damage;
        script.chill_strength = chill_strength;
        script.burn_strength = burn_strength;
        script.mezmerize_strength = mezmerize_strength;
        script.sunder_strength = sunder_strength;
        script.gameObject.layer = layer;
        script.knockback_power = knockback_power;
        /*Homing script values passed for homing toggle*/
        script.home_speed = home_speed;
        script.home_radius = home_radius;
        script.homes = homes;
        script.can_pierce = can_pierce;
        script.gun_reference = this;//For gaining exp 
        next_time = Time.time + reload_time;
        if (Claimed_Gun_Mods != null)//Apply chosen gun_abilities to each bullet
        {
            int i = 0;
           foreach (Gun_Abilities g in Claimed_Gun_Mods.GetInvocationList()) 
           {
               if (i <= level - mez_threshold)
               {
                   script.StartCoroutine(g(this, script));
                   i++;
               }
           }   
        }
        
    }


    protected abstract string GunDesc();//Description is based on derived type

    protected string GetName()
    {
        string phrase = "";
        if (prefixes.Count > 0)
        {
            foreach (string s in prefixes)
            {
                phrase += s + " ";
            }
        }

        phrase += GetBaseName() + " ";

        if (suffixes.Count > 0)
        {
            phrase += "of ";
            int i = 1;
            foreach (string s in suffixes)
            {
                phrase += s + " ";
                if (i < suffixes.Count)
                {
                    i++;
                    phrase += "and ";
                }
            }
        }

        phrase += last_suffix;

        return phrase;
    }

    public override string ToString()
    {
        return string.Format(GetName() + "\n" + GunDesc() + "\n" + "Level : {0}" + "\n" + "Experience : {1}/{2} " + "\n" + "Level Up Points : {3}" + "\n" + " Homing : {4} " + "\n" + " Damage : {5} - {6}" + " \n" + " Reload Time : {7}",level,experience,next_lvl,points, home_radius, lower_bound_damage, upper_bound_damage, reload_time);                
    }

    [ClientRpc]
    public void RpcSetGun(int _level,uint _points,int _experience,int _next_lvl,int[] indeces)
    {
        SetBaseStats();
        level = _level;
        points = _points;
        next_lvl = _next_lvl;
        experience = _experience;
        foreach (int index in indeces)
        {
            claimed_gun_ability.Add(index);
            Claimed_Gun_Mods += ClassGunMods(index);
            SetGunNameAddons(index);
        }
        color = new Color32(52, 95, 221, 225);
        layer = 13;
        home_layer = 10;
        barrel_end = transform.GetChild(0);
        in_inventory = false;
    }

    public override void PrepareItemForUse()
    {
        in_inventory = false;
        if (item_image_show)
        {
            item_image_show.transform.SetParent(weapons_bar.transform);
            buttons.Add(button);
            int[] indeces = new int[claimed_gun_ability.Count];
            int n = 0;
            foreach (int i in claimed_gun_ability)
            {
                indeces[n] = i;
                n++;
            }
            client_user.CmdSetGun(gameObject, level, points, experience, next_lvl, indeces);
            for(int i = 0;i < client_user.weapons.Length;i++)
            {
                if(!client_user.weapons[i])
                {
                    client_user.weapons[i] = this;
                    break;
                }
            }
        }
    }

    public override Options GetOptionsFuncs()
    {
        Options options = delegate
        {
            BringUpLevelUpTable();
        };
        if (button != "")
        {
            options += delegate
            { 
                button = "";
            };
        }
        if (button != "q")
        {
            options += delegate
            {
                button = "q";
            };
        }
        if (button != "e")
        {
            options += delegate
            { 
                button = "e";
            };
        }
        if (button != "f")
        {
            options += delegate
            {
                button = "f";
            };
        }
        if (button != "c")
        {
            options += delegate
            {
                button = "c";
            };
        }
        return options;
    }

    public override List<string> GetOptionsStrings()
    {
        List<string> options = new List<string>();
        options.Add("Allocate Gun Points");
        if(button != "" )
        {
            options.Add("Set Button as Left Click");
        }
        if(button != "q")
        {
            options.Add("Set Button as Q");
        }
        if (button != "e")
        {
            options.Add("Set Button as E");
        }
        if (button != "f")
        {
            options.Add("Set Button as F");
        }
        if (button != "c")
        {
            options.Add("Set Button as C");
        }
        return options;
    }


   

    public override XElement RecordValuesToSaveFile()
    {
        XElement element = new XElement(GetBaseName());
        XElement value_setter = new XElement("Level",level.ToString());
        element.Add(value_setter);
        value_setter = new XElement("Points",points.ToString());
        element.Add(value_setter);
        value_setter = new XElement("Button", button);
        element.Add(value_setter);
        if (claimed_gun_ability.Count != 0)
        {
            value_setter = new XElement("MethodIndex");
            foreach (int i in claimed_gun_ability)
            {
                value_setter.Add(new XElement("Index", i.ToString()));
            }
            element.Add(value_setter);
        }
        return element;
    }

    public override void RecordValuesFromSaveFile(XElement element)
    {
        XElement value_retriever;
        if (element.Descendants("Level").Any())
        {
            value_retriever = element.Element("Level") as XElement;
            level = Int32.Parse(value_retriever.Value);
        }
        if (element.Descendants("Points").Any())
        {
            value_retriever = element.Element("Points") as XElement;
            points = UInt32.Parse(value_retriever.Value);
        }
        if(element.Descendants("Button").Any())
        {
            value_retriever = element.Element("Button") as XElement;
            button = value_retriever.Value;
        }
        if (element.Descendants("MethodIndex").Any())
        {
            int num;
            foreach (XElement e in element.Elements("MethodIndex").Elements("Index"))
            {
                num = Int32.Parse(e.Value);
                claimed_gun_ability.Add(num);
            }
        }
        SetBaseStats();
    }
    

    public abstract Gun_Abilities ClassGunMods(int index);//For getting a derived class's Gun_ability delegates
    protected abstract string ClassGunAbilityNames(int index);//For getting a derived class's Gun_ability string names 
    protected abstract void SetGunNameAddons(int index);//For getting a derived class's prefixes/suffixes
    protected abstract string GunAbilityDesc(int index);//For getting a derived class's descriptions
   
    
    

 }
