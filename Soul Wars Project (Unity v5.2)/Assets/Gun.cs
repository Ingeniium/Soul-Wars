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
	private float next_time;//Next firing time
    [SyncVar] public float home_speed;//Angular turning speed of the bullet
	[SyncVar] public float home_radius;
    public bool homes = true;//whether the bullets will have homing capabilities
	[SyncVar] public int lower_bound_damage = 7;
    [SyncVar] public int upper_bound_damage = 15;
    [SyncVar] public float knockback_power;//How much knockback force and knockback stun is done 
    [SyncVar] public float projectile_speed;
    [SyncVar] public float range;
    [SyncVar]  public double crit_chance = .1f;
	public Transform barrel_end;//Where bullets actually SPAWN from
    public Color color;//color assigned to the BULLETS
    public int layer;//Collision layer of the bullet
    public int home_layer;//Collision layer of the bullet's homing device
    private int level = 0;//Level of the current gun
    private int next_lvl = 20;//experience to next level
    /*Note that level and experience is limited to one script and not entire class
     (scripts are copied,but value isn't static) to allow player option of choosing
     different gun abilities within same type of gun*/
    public int experience
    {
        get { return _experience; }
        set 
        {
            if (value >= next_lvl)
            {
                _experience = 0;
                level += 1;
                next_lvl = (level + 20 + level / 4) * 2;
                level_up_indication = Instantiate(Player.cooldown_canvas, _item_image.transform.position + new Vector3(-.25f, 0, 0), Player.cooldown_canvas.transform.rotation) as Canvas;
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

    public void OnApplicationPause()
    {
    }
    

    public virtual void Shoot()
    {
        bullet = Instantiate(Bullet, barrel_end.position, gameObject.transform.rotation) as GameObject;
        NetworkServer.SpawnWithClientAuthority(bullet, client_user.connectionToClient);
        ReadyWeaponForFire(ref bullet);
        bullet.GetComponent<Rigidbody>().AddForce(barrel_end.forward * projectile_speed, ForceMode.Impulse);
    }

    public virtual void Shoot(GameObject g)
    {
        bullet = g;
        ReadyWeaponForFire(ref bullet);
        bullet.GetComponent<Rigidbody>().AddForce(barrel_end.forward * projectile_speed, ForceMode.Impulse);
    }
   
    protected void ReadyWeaponForFire(ref GameObject weapon_fire)
    {
        //Make bullet the designated color
        weapon_fire.GetComponent<Renderer>().material.color = color;
        BulletScript script = weapon_fire.GetComponent<BulletScript>();
        //Pass values on to bullet
        script.crit_chance = crit_chance;
        script.upper_bound_damage = upper_bound_damage;
        script.lower_bound_damage = lower_bound_damage;
        script.gameObject.layer = layer;
        script.knockback_power = knockback_power;
        /*Homing script values passed for homing toggle*/
        script.home.layer = home_layer;
        script.home_speed = home_speed;
        script.home_radius = home_radius;
        script.homes = homes;
        script.gun_reference = this;//For gaining exp 
        next_time = Time.time + reload_time;
        if (Claimed_Gun_Mods != null)//Apply chosen gun_abilities to each bullet
        {
           foreach (Gun_Abilities g in Claimed_Gun_Mods.GetInvocationList()) { script.StartCoroutine(g(this,script)); }   
        }
        
    }

    public void ApplyGunAbilities()
    {
        if (Claimed_Gun_Mods != null && bullet != null)
        {
            BulletScript script = bullet.GetComponent<BulletScript>();
            foreach (Gun_Abilities g in Claimed_Gun_Mods.GetInvocationList()) { script.StartCoroutine(g(this, script)); }
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
            foreach (string s in suffixes)
            {
                phrase += s + " ";
            }
        }

        phrase += last_suffix;

        return phrase;
    }

    public override string ToString()
    {
        return string.Format(GetName() + "\n" + GunDesc() + "\n" + "Level : {0}" + "\n" + "Experience : {1}/{2} " + "\n" + "Level Up Points : {3}" + "\n" + " Homing : {4} " + "\n" + " Damage : {5} - {6}" + " \n" + " Reload Time : {7}",level,experience,next_lvl,points, home_radius, lower_bound_damage, upper_bound_damage, reload_time);                
    }

    public override void PrepareItemForUse()
    {
       
        GameObject prev_Gun = PlayerController.Client.Gun;
        if (prev_Gun == null)
        {
            throw new System.NullReferenceException("It looks like prev_Gun wasn't set properly after destruction");
        }
        if (in_inventory)
        {
               _item_image.transform.SetParent(weapons_bar.transform);     
                GameObject Gun = Instantiate(asset_reference, prev_Gun.transform.position, prev_Gun.transform.rotation) as GameObject;
                Gun.transform.SetParent(PlayerController.Client.gameObject.transform);
                CopyComponent<Gun>(this, Gun);
                Gun gun = Gun.GetComponent<Gun>();
                _item_image.GetComponentInChildren<ItemImage>().item_script = gun;
                gun.current_reference = Gun;
                gun.color = prev_Gun.GetComponent<Gun>().color;
                gun.layer = 13;
                gun.home_layer = 10;
                gun.barrel_end = Gun.GetComponentInChildren<Transform>();
                gun.in_inventory = false;
                if (inv)
                {
                    inv.RemoveItem(ref _item_image);
                }
                if (gun.index > -1)
                {
                    gun.EquipAtSlot(gun.index);
                }
                else
                {
                    int i = PlayerController.Client.equipped_weapons.IndexOf(null);
                    /*If there isn't an empty slot within equipped weapons, assign the newly instanced gun
                    to the index of the gun which the Player is currently wielding,sending the previous weapon
                    to the Inventory by storing the Item Image*/
                    if (i == -1)
                    {
                        gun.EquipAtSlot(PlayerController.Client.main_weapon_index);
                    }
                    /*Otherwise,disable rendering of the newly created instance and put it somewhere
                     in which there is an empty slot*/
                    else
                    {
                        gun.current_reference.GetComponent<Renderer>().enabled = false;
                        PlayerController.Client.equipped_weapons[i] = gun;
                        gun.index = i;
                    }
                }
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
        if (!PlayerController.Client.equipped_weapons[Index] && index != 1)//equipped weapons cant be assigned to an empty slot
            {
                 return;
            }
            else if (PlayerController.Client.equipped_weapons[Index])//if there's already equipped gun in slot
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
                PlayerController.Client.gun = this;
                PlayerController.Client.Gun = current_reference;
                //PlayerController.Client.GetComponent<NetworkTransformChild>().target = current_reference.transform;
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
            foreach (XElement e in element.Elements("MethodIndex"))
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
