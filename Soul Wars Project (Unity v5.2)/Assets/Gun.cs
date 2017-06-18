using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
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
    public Gun median;//for copy transfer
	public Transform barrel_end;//Where bullets actually SPAWN from
    public Color color;//color assigned to the BULLETS
    public int layer;//Collision layer of the bullet
    public int home_layer;//Collision layer of the bullet's homing device
    private int level = 0;//Level of the current gun
    private int next_lvl = 40;//experience to next level
    /*Note that level and experience is limited to one script and not entire class
     (scripts are copied,but value isn't static) to allow player option of choosing
     different gun abilities within same type of gun*/
    public int experience
    {
        get { return _experience; }
        set 
        {
            if (level == 5)
            {
                return;
            }
            else if (value >= next_lvl)
            {
                _experience = 0;
                level += 1;
                next_lvl = level * 30 + next_lvl;
                level_up_indication = Instantiate(client_user.cooldown_canvas, _item_image.transform.position + new Vector3(-.25f, 0, 0), client_user.cooldown_canvas.transform.rotation) as Canvas;
                level_up_indication.GetComponentInChildren<Text>().text = "!";
                level_up_indication.GetComponentInChildren<Text>().color = Color.green;
                level_up_indication.transform.SetParent(_item_image.transform);
                points += 1;
            }
            else
            {
                _experience = value;
            }
        }
    }
    private int _experience = 0;
    public uint points = 0;//points to spend on abilities unlocked by gun level ups
    protected Canvas level_up_indication;
    protected List<int> claimed_gun_ability = new List<int>();//For determining which abilities have already been chosen
    protected delegate IEnumerator Gun_Abilities(Gun gun,BulletScript script);
    protected Gun_Abilities Claimed_Gun_Mods;//Abilities which have already been chosen
    /*Below are added to names of weapons as a result of getting abilities*/
    protected List<string> prefixes = new List<string>();
    protected List<string> suffixes = new List<string>();
    protected string last_suffix = "";

    protected override void OnClientUserChange()    {        weapons_bar = _client_user.hpbar_show.GetComponentInChildren<VerticalLayoutGroup>().gameObject;
        GunLevelUp = client_user.hpbar_show.GetComponentInChildren<MenuDisplay>().
                    Guntable.gameObject;    }

    

    public bool HasReloaded()
    {
        return (Time.time > next_time);       
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
            weapon_fire.GetComponent<Rigidbody>().AddForce(dir * projectile_speed, ForceMode.Impulse);
           // weapon_fire.transform.rotation = Quaternion.Euler(weapon_fire.transform.rotation.x, weapon_fire.transform.rotation.y, weapon_fire.transform.rotation.z);
        }
    }
    
    
    public virtual void Shoot()
    {
        bullet = Instantiate(Bullet, barrel_end.position, barrel_end.rotation) as GameObject;
        NetworkServer.SpawnWithClientAuthority(bullet,client_user.connectionToClient);
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
        script.gameObject.layer = layer;
        script.knockback_power = knockback_power;
        /*Homing script values passed for homing toggle*/
        //.homer.layer = home_layer;
        script.home_speed = home_speed;
        script.home_radius = home_radius;
        script.homes = homes;
        script.can_pierce = can_pierce;
        script.gun_reference = this;//For gaining exp 
        next_time = Time.time + reload_time;
        if (Claimed_Gun_Mods != null)//Apply chosen gun_abilities to each bullet
        {
           foreach (Gun_Abilities g in Claimed_Gun_Mods.GetInvocationList()) { script.StartCoroutine(g(this,script)); }   
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
        current_reference = gameObject;
    }

    public override IEnumerator PrepareItemForUse()
    {

        if (in_inventory)
        {
                _item_image.transform.SetParent(weapons_bar.transform);     
                PlayerController.Client.CmdSpawnItem(GetBaseName(),new Vector3(0.21f,.11f,.902f),new Quaternion(0,0,0,0),true);
                while (!client_user.pass_over)
                {
                    yield return new WaitForEndOfFrame();
                }
                Gun median = client_user.pass_over.GetComponent<Gun>();
                int[] index = new int[claimed_gun_ability.Count()];
                for (int i = 0; i < claimed_gun_ability.Count(); i++)
                {
                    index[i] = claimed_gun_ability[i];
                }
                client_user.CmdSetGun(client_user.pass_over, level, points, experience, next_lvl, index);
                while (!median.current_reference)
                {
                    yield return new WaitForEndOfFrame();
                }
                _item_image.GetComponentInChildren<ItemImage>().item_script = median;
                median._item_image = _item_image;
                if (inv)
                {
                    inv.RemoveItem(ref _item_image);
                }
                if (median.index > -1)
                {
                    median.EquipAtSlot(median.index);
                }
                else
                {
                    int i = PlayerController.Client.equipped_weapons.IndexOf(null);
                    /*If there isn't an empty slot within equipped weapons, assign the newly instanced gun
                    to the index of the gun which the Player is currently wielding,sending the previous weapon
                    to the Inventory by storing the Item Image*/
                    if (i == -1)
                    {
                        median.EquipAtSlot(PlayerController.Client.main_weapon_index);
                    }
                    /*Otherwise,disable rendering of the newly created instance and put it somewhere
                     in which there is an empty slot*/
                    else
                    {
                        median.current_reference.GetComponent<Renderer>().enabled = false;
                        PlayerController.Client.equipped_weapons[i] = median;
                        median.index = i;
                    }
                }
                median = null;
                
            Destroy(this);
            
        }
    }
    public override void Options()
    {
        item_options_show = Instantiate(item_options, _item_image.transform.position + new Vector3(0, 2, 2), item_options.transform.rotation) as Canvas;
        item_options_show.GetComponent<HPbar>().Object = _item_image.gameObject;
        Button[] buttons = item_options_show.GetComponentsInChildren<Button>();
        for (int i = buttons.Length - 3; i > PlayerController.Client.max_weapon_num; i--)
        {
            //Adjust the location of the Allocate LvlUP points button
            buttons[buttons.Length - 2].transform.localPosition = buttons[buttons.Length - 1].transform.localPosition;
            //Adjust the location of the Drop Item button
            buttons[buttons.Length - 1].transform.localPosition = buttons[i].transform.localPosition;
            Destroy(buttons[i].gameObject);
        }
        for (int i = 0; i < buttons.Length - 2; i++)
        {
            //print(i.ToString());//For some reason,the value of 'i' changes from here
            int temp = i;
            if (index != i)
            {
                buttons[i].onClick.AddListener(delegate
                {
                    //print("int i is " + i.ToString());//to here
                    EquipAtSlot(temp);
                    _item_image.GetComponentInChildren<ItemImage>().option_showing = false;
                    PlayerController.Client.equip_action = true;
                    if (item_options_show)
                    {
                        Destroy(item_options_show.gameObject);
                    }

                });
            }
            else
            {
                buttons[buttons.Length - 2].transform.localPosition = buttons[buttons.Length - 1].transform.localPosition;
                buttons[buttons.Length - 1].transform.localPosition = buttons[i].transform.localPosition;
                Destroy(buttons[i].gameObject);
            }
        }
        
        buttons[buttons.Length - 2].onClick.AddListener(delegate 
        {
            if (PlayerController.Client.equipped_weapons.Count > 1)//Can't drop a weapon if its the only one you have
            {
                DropItem();
            }
            _item_image.GetComponentInChildren<ItemImage>().option_showing = false;
            PlayerController.Client.equip_action = true;
            if (item_options_show)
            {
                Destroy(item_options_show.gameObject);
            }
        });

        buttons[buttons.Length - 1].onClick.AddListener(delegate
        {
            BringUpLevelUpTable();
            PlayerController.Client.equip_action = true;
            _item_image.GetComponentInChildren<ItemImage>().option_showing = false;
            if (item_options_show)
            {
                Destroy(item_options_show.gameObject);
            }
        });
        
            
        

        
    }

   

    public void EquipAtSlot(int Index)
    {
        
            if (PlayerController.Client.equipped_weapons[Index])//if there's already equipped gun in slot
            {
                PlayerController.Client.equipped_weapons[Index].index = -1;//Secondhand indicator that it isn't equipped by a player
                inv.InsertItem(ref PlayerController.Client.equipped_weapons[Index]._item_image);//Put item in inventory
                /*transfer the script from the weapon to the item image before destroying the weapon instance*/
                CopyComponent<Gun>(PlayerController.Client.equipped_weapons[Index].current_reference.GetComponent<Gun>(), PlayerController.Client.equipped_weapons[Index]._item_image);
                PlayerController.Client.equipped_weapons[Index]._item_image.GetComponentInChildren<ItemImage>().item_script = PlayerController.Client.equipped_weapons[Index]._item_image.GetComponent<Gun>();
                Destroy(PlayerController.Client.equipped_weapons[Index].current_reference);
            }
            PlayerController.Client.equipped_weapons[index] = null;
            index = Index;
            if (Index == PlayerController.Client.main_weapon_index)//Set it up to be main gun if the index is equivalent to the current equipped gun
            {
                client_user.CmdEquipGun(current_reference);
                current_reference.GetComponent<Renderer>().enabled = true;
            }
            PlayerController.Client.equipped_weapons[Index] = this;
     }

    public override XElement RecordValuesToSaveFile()
    {
        XElement element = new XElement(GetBaseName());
        element.SetAttributeValue("Index",index);
        XElement value_setter = new XElement("Level",level.ToString());
        element.Add(value_setter);
        value_setter = new XElement("Points",points.ToString());
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
        if (element.Descendants("MethodIndex").Any())
        {
            int num;
            foreach (XElement e in element.Elements("MethodIndex").Elements("Index"))
            {
                num = Int32.Parse(e.Value);
                SetGunNameAddons(num);
                Claimed_Gun_Mods += ClassGunMods(num);
                claimed_gun_ability.Add(num);
            }
        }
        SetBaseStats();
    }
    

    protected abstract Gun_Abilities ClassGunMods(int index);//For getting a derived class's Gun_ability delegates
    protected abstract string ClassGunAbilityNames(int index);//For getting a derived class's Gun_ability string names 
    protected abstract void SetGunNameAddons(int index);//For getting a derived class's prefixes/suffixes
    protected abstract string GunAbilityDesc(int index);//For getting a derived class's descriptions
    
    /*Functions below are virtual as to allow derived classes their own static variables for keeping track of assignment*/
    protected abstract bool AreGunLevelUpButtonsAssignedForClass();

    protected void Start()
    {
       
        unit_reference = GetComponent<GenericController>();
        if (!unit_reference)
        {
            unit_reference = GetComponentInParent<GenericController>();
        }
    }
    
    

 }
