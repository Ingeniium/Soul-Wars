using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections;

public abstract class Gun : Item {
    private static GameObject GunLevelUp;//Reference to the table used for getting gun abilities
	public GameObject Bullet;
	protected GameObject bullet;
    public float reload_time;//How long it takes to fire each shot
	public float next_time;//Next firing time
    public float home_speed;//Angular turning speed of the bullet
	public float home_radius;
    public bool homes = true;//whether the bullets will have homing capabilities
	public int damage = 2;
    public float projectile_speed;
    public float range;
    public double crit_chance = .1f;
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
    protected bool[] claimed_gun_ability = new bool[3]{false,false,false};//For determining which abilities have already been chosen
    protected delegate IEnumerator Gun_Abilities(BulletScript script);
    protected Gun_Abilities Claimed_Gun_Mods;//Abilities which have already been chosen
    
    public virtual void Shoot()
    {
        bullet = Instantiate(Bullet, barrel_end.position, gameObject.transform.rotation) as GameObject;
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
        script.damage = damage;
        script.gameObject.layer = layer;
        if (homes)//Only pass these values if bullet does indeed have homing ability
        {
            script.home.layer = home_layer;
            script.home_speed = home_speed;
            script.home_radius = home_radius;
            script.homes = true;
        }
        script.gun_reference = this;//For gaining exp 
        next_time = Time.time + reload_time;
        if (Claimed_Gun_Mods != null)//Apply chosen gun_abilities to each bullet
        {
            foreach (Gun_Abilities g in Claimed_Gun_Mods.GetInvocationList()) { StartCoroutine(g(script)); }
        }

    }

    public override string ToString()
    {
        return string.Format(name + "Launches a powerful arrow.\n Homing : {0} Damage : {1} \n Reload Time : {2}", home_radius, damage, reload_time);                
    }

    public override void PrepareItemForUse()
    {
       
        GameObject prev_Gun = Player.Gun;
        if (prev_Gun == null)
        {
            throw new System.NullReferenceException("It looks like prev_Gun wasn't set properly after destruction");
        }
        if (in_inventory)
        {
                _item_image.transform.parent = weapons_bar.transform;
                _item_image.transform.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(-29f, 230);
                GameObject Gun = Instantiate(asset_reference, prev_Gun.transform.position, prev_Gun.transform.rotation) as GameObject;
                Gun.transform.SetParent(Player.gameObject.transform);
                CopyComponent<Gun>(this, Gun);
                Gun gun = Gun.GetComponent<Gun>();
                _item_image.GetComponentInChildren<ItemImage>().item_script = gun;
                gun.current_reference = Gun;
                gun.color = prev_Gun.GetComponent<Gun>().color;
                gun.layer = 13;
                gun.home_layer = 10;
                gun.barrel_end = Gun.GetComponentInChildren<Transform>();
                gun.in_inventory = false;
                inv.RemoveItem(ref _item_image);
            
            int i = Player.equipped_weapons.IndexOf(null);
            /*If there isn't an empty slot within equipped weapons, assign the newly instanced gun
            to the index of the gun which the Player is currently wielding,sending the previous weapon
            to the Inventory by storing the Item Image*/
            if (i == -1)
            {
                gun.EquipAtSlot(Player.main_weapon_index);
            }
            /*Otherwise,disable rendering of the newly created instance and put it somewhere
             in which there is an empty slot*/
            else
            {
                gun.current_reference.GetComponent<Renderer>().enabled = false;
                Player.equipped_weapons[i] = gun;
                gun.index = i;
            }
            Destroy(this);
            
        }
    }
    public override void Options()
    {
        item_options_show = Instantiate(item_options, _item_image.transform.position + new Vector3(0, 2, 2), item_options.transform.rotation) as Canvas;
        item_options_show.GetComponent<HPbar>().Object = _item_image.gameObject;
        Button[] buttons = item_options_show.GetComponentsInChildren<Button>();
        for (int i = buttons.Length - 3; i > Player.max_weapon_num; i--)
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
                    Item.Player.equip_action = true;
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
            DropItem();
            _item_image.GetComponentInChildren<ItemImage>().option_showing = false;
            Item.Player.equip_action = true;
            if (item_options_show)
            {
                Destroy(item_options_show.gameObject);
            }
        });

        buttons[buttons.Length - 1].onClick.AddListener(delegate
        {
            BringUpLevelUpTable();
            Item.Player.equip_action = true;
            _item_image.GetComponentInChildren<ItemImage>().option_showing = false;
            if (item_options_show)
            {
                Destroy(item_options_show.gameObject);
            }
        });
        
            
        

        
    }

   

    protected void EquipAtSlot(int Index)
    {
            if (index > -1 && index < Player.equipped_weapons.Count)
            {
                if(Player.equipped_weapons[index] != null)//Make sure the that the previous slot is empty
                {
                    Player.equipped_weapons[index] = null;
                }
            }
            index = Index;
            if (Player.equipped_weapons[Index])//if there's already equipped gun in slot
            {
                Player.equipped_weapons[Index].index = -1;//Secondhand indicator that it isn't equipped by a player
                inv.InsertItem(ref Player.equipped_weapons[Index]._item_image);//Put item in inventory
                /*transfer the script from the weapon to the item image before destroying the weapon instance*/
                CopyComponent<Gun>(Player.equipped_weapons[Index].current_reference.GetComponent<Gun>(), Player.equipped_weapons[Index]._item_image);
                Player.equipped_weapons[Index]._item_image.GetComponentInChildren<ItemImage>().item_script = Player.equipped_weapons[Index]._item_image.GetComponent<Gun>();
                Destroy(Player.equipped_weapons[Index].current_reference);
            }
            if (Index == Player.main_weapon_index)//Set it up to be main gun if the index is equivalent to the current equipped gun
            {
                Player.gun = this;
                Player.Gun = current_reference;
                current_reference.GetComponent<Renderer>().enabled = true;
            }
            Player.equipped_weapons[Index] = this;
     }

    protected void BringUpLevelUpTable()
    {
        GunTable.gun_for_consideration = this;
        GunLevelUp.transform.SetParent(main_bar_tr);
        GunLevelUp.transform.localPosition = Vector3.zero;
    }

    protected static class GunTable
    {
        public static GunTableButton[] buttons;
        public static Gun gun_for_consideration
        {
            get { return _gun_for_consideration;}
            set
            {
                _gun_for_consideration = value;
                SetGunTable();
            }
        }
        private static Gun _gun_for_consideration;

        public static void InitGunTable()//For giving gameobject buttons a private class instance of GunTableButton
        {
            Button[] b = Gun.GunLevelUp.GetComponentsInChildren<Button>();
            for (int i = 0; i < b.Length - 1; i++)
            {
                b[i].gameObject.AddComponent<GunTableButton>();
            }
            buttons = Gun.GunLevelUp.GetComponentsInChildren<GunTableButton>();
            
            for(int i = 0;i < buttons.Length;i++)//Sets up GunTable Info
            {
                buttons[i].button = b[i];
                buttons[i].index = i;
            }
        }

        public static void SetGunTable()
        {
            foreach (GunTableButton g in buttons)
            {
                //Copy the names of the abilities to the strings of the buttons.
                g.GetComponentInChildren<Text>().text = gun_for_consideration.ClassGunAbilityNames(g.index);
                /*For each row(composed of 3 buttons),if the level is too low,disable the 
                next row of buttons,turning them grey.*/
                if (g.index > 3 * gun_for_consideration.level - 1)
                {
                    ColorBlock cb = g.button.colors;
                    cb.disabledColor = Color.grey;
                    g.button.colors = cb;
                    g.button.interactable = false;
                }
                /*Otherwise,check for whether it was already clamied.If so,
                disable that specific button,switching colors with it and its
                associated string*/
                else if (gun_for_consideration.claimed_gun_ability[g.index] == true)
                {
                    ColorBlock cb = g.button.colors;
                    cb.disabledColor = Color.yellow;
                    g.button.colors = cb;
                    g.button.interactable = false;
                }
                /*If neither of the conditions are true then proceed to
                make sure that the buttons is active */
                else if (g.button.interactable == false)
                {
                    g.button.interactable = true;
                }
            }
            if (gun_for_consideration.AreGunLevelUpButtonsAssignedForClass() == false)
            {
                for (int i = 0; i < buttons.Length - 1; i++)//Exclued "x" button
                {
                    int temp = i;
                    buttons[temp].method = gun_for_consideration.ClassGunMods(temp);                                    
                }
            }
            if (gun_for_consideration.points == 0 && gun_for_consideration.level_up_indication)
            {//Destroy indication when there's no points
                Destroy(gun_for_consideration.level_up_indication.gameObject);
            }


        }

    }


    protected class GunTableButton : MonoBehaviour
    {
        public Button button;
        public Gun_Abilities method;
        public int index;
        
        void OnMouseDown()
        {
            if (GunTable.gun_for_consideration.points > 0 && button.enabled && method != null)
            {
                /*Add delegate to gun abilities*/
                --GunTable.gun_for_consideration.points;
                GunTable.gun_for_consideration.Claimed_Gun_Mods += method;
                GunTable.gun_for_consideration.claimed_gun_ability[index] = true;
                /*Switch colors of text and button to show it has been taken*/
                ColorBlock cb = button.colors;
                cb.disabledColor = Color.yellow;
                button.colors = cb;
                button.GetComponentInChildren<Text>().color = Color.red;
                button.interactable = false;
                if (GunTable.gun_for_consideration.points == 0 && GunTable.gun_for_consideration.level_up_indication)
                {
                    Destroy(GunTable.gun_for_consideration.level_up_indication.gameObject);
                }
            }
        }
    }

    protected abstract Gun_Abilities ClassGunMods(int index);//For getting a derived classes Gun_ability delegates
    protected abstract string ClassGunAbilityNames(int index);//For getting a derived classes Gun_ability string names     

    
    /*Functions below are virtual as to allow derived classes their own static variables for keeping track of assignment*/
    protected abstract bool AreGunLevelUpButtonsAssignedForClass();
    /*Function below is for setting up complete list of gun_ability delegates
     ,for nonstatic ones can't be assigned compile time*/
    protected abstract void SetBaseGunAbilities();

    protected void Start()
    {
        if (Player == null)
        {
            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
            inv = GameObject.FindGameObjectWithTag("Inventory").GetComponent<Inventory>();
            main_bar_tr = GameObject.FindGameObjectWithTag("MainCanvas").transform;
        }
        if (GunLevelUp == null)
        {
            weapons_bar = GameObject.FindGameObjectWithTag("Weapons");
            GunLevelUp = GameObject.FindGameObjectWithTag("GunLevelUp");
            GunTable.InitGunTable();
        }
        if (ClassGunMods(0) == null)
        {
            SetBaseGunAbilities();
        }
       
    }
    
    

 }
