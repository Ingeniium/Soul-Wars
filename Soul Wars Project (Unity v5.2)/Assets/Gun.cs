using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections;
public abstract class Item : MonoBehaviour
{
    public static T CopyComponent<T>(T original, GameObject destination) where T : Component
    {
        System.Type type = original.GetType();
        Component copy = destination.AddComponent(type);
        System.Reflection.FieldInfo[] fields = type.GetFields();
        foreach (System.Reflection.FieldInfo field in fields)
        {
            field.SetValue(copy, field.GetValue(original));
        }
        return copy as T;
    }
    public static PlayerController Player;//Reference to the Player
    protected static Inventory inv;//Reference to the Inventory obj
    protected static GameObject weapons_bar;//Reference to the weapons bar
    protected static Transform main_bar_tr;//Reference to the transform of the Player Bar Canvas
    protected string name;
    public Canvas drop_canvas;
    protected Canvas drop_canvas_show;//Canvas for showing the name of dropped items upon mouse hovering
    protected bool dropped = false;
    public bool in_inventory = false;
    public Canvas item_options;
    protected Canvas item_options_show;//Canvas for showing item options upon right click
    public GameObject item_image;
    public GameObject _item_image;//Reference to item image instance
    public GameObject asset_reference;//Reference to the prefab for instantiating/destruction at runtime
    public GameObject current_reference;//Reference to the current object script works for
    protected int index;//Index for equipment
    public void DropItem(ref double chance)
    {
        System.Random rand= new System.Random();
        if (rand.NextDouble() <= chance)
        {
            gameObject.layer = 0;
            transform.parent = null;
            gameObject.AddComponent<Rigidbody>();//For it to drop on ground
            gameObject.AddComponent<BoxCollider>();//For it to stay on ground
            //Indication for players to know it's dropped
            drop_canvas_show = Instantiate(drop_canvas, transform.position + new Vector3(0,0,.5f), drop_canvas.transform.rotation) as Canvas;
            drop_canvas_show.GetComponentInChildren<Text>().text = name;
            Destroy(drop_canvas_show.gameObject, 1f);
            //for code checking state
            dropped = true;
            index = -1;
        }
    }
    protected void DropItem()//Overload for when when a player decides to drop item
    {
        GameObject item = null;
        dropped = true;
        index = -1;
        if (current_reference)
        {
            item = current_reference;
        }
        else
        {
            item = Instantiate(asset_reference, transform.position, transform.rotation) as GameObject;
            CopyComponent<Item>(this, item);
        }
        item.layer = 0;
        item.transform.parent = null;
        item.AddComponent<Rigidbody>();
        item.AddComponent<BoxCollider>();
        item.GetComponent<Renderer>().enabled = true;
        drop_canvas_show = Instantiate(drop_canvas, transform.position + new Vector3(0, 0, .5f), drop_canvas.transform.rotation) as Canvas;
        drop_canvas_show.GetComponentInChildren<Text>().text = name;
        Destroy(drop_canvas_show.gameObject, 1f);
        if (_item_image)
        {
            Destroy(_item_image.gameObject);
        }
      
    }

    protected void RetrieveItem()//Used for picking items off ground
    {
        //Create and set a respective item image object
       _item_image = Instantiate(item_image, item_image.transform.position, item_image.transform.rotation) as GameObject;
       CopyComponent<Item>(this,_item_image);//This is done due to the destruction of the actual gameobject;
       _item_image.GetComponentInChildren<ItemImage>().item_script = _item_image.GetComponent<Item>();
       _item_image.GetComponentInChildren<ItemImage>().Item_ = asset_reference;
        inv.InsertItem(ref _item_image);//Put it in inventory
       _item_image.GetComponent<Item>()._item_image = _item_image;
    }


    public abstract void PrepareItemForUse();//Creates and sets up gameobject
    public abstract void Options();//Sets up options

    void OnMouseEnter()
    {
        if (dropped)
        {
            drop_canvas_show = Instantiate(drop_canvas, transform.position + new Vector3(0, 0, .5f), drop_canvas.transform.rotation) as Canvas;
            drop_canvas_show.GetComponentInChildren<Text>().text = name;
        }
    }

    void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (dropped)
            {
                in_inventory = true;
                RetrieveItem();
                if (drop_canvas_show)
                {
                    Destroy(drop_canvas_show.gameObject);
                }
                Destroy(gameObject);
            }
            
           
        }
    }

    void OnMouseExit()
    {
        if (drop_canvas_show)
        {
            Destroy(drop_canvas_show.gameObject);
        }
    }
    
}

public class Gun : Item {
    private static GameObject GunLevelUp;
	public GameObject Bullet;
	protected GameObject bullet;
    public float reload_time = 2.33f;
	public float next_time = -1f;
    public float home_speed;//Angular turning speed of the bullet
	public float home_radius = 5.0f;
    public bool homes = true;//whether the bullets will have homing capabilities
	public int damage = 2;
    public double crit_chance = .1f;
	public Transform barrel_end;
    public Color color;//color assigned to the BULLETS
    public int layer;//Collision layer of the bullet
    public int home_layer;//Collision layer of the bullet's homing device
    private int level = 0;//Level of the current gun
    private int next_lvl = 20;
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
    private readonly static string[] gun_ability_names = new string[3]{"Marksman","Sniper","Drone"};
    private bool[] claimed_gun_ability = new bool[3]{false,false,false};//For determining which abilities have already been chosen
    private delegate IEnumerator Gun_Abilities(BulletScript script);
    private static Gun_Abilities[] Gun_Mods = new Gun_Abilities[3];//This class's pool of gun_abilities
    private Gun_Abilities Claimed_Gun_Mods;//Abilities which have already been chosen
    private static bool assigned_buttons = false;//For determing if the gun table buttons have been assigned their respective function
    
    public virtual void Shoot()
    {
        bullet = Instantiate(Bullet, barrel_end.position, gameObject.transform.rotation) as GameObject;
        ReadyWeaponForFire(ref bullet);
        bullet.GetComponent<Rigidbody>().AddForce(barrel_end.forward, ForceMode.Impulse);
    }

    /*Ability that grants extra crit chance based on how far the bullet
     deviates from its "original" path.Less deviation means more crit,the 
     maximum amount being +30%*/
    private IEnumerator Marksman(BulletScript script)
    {
        script.coroutines_running++;
        Vector3 start_pos = script.transform.position;//Get its original position
        Vector3 start_forward = script.transform.forward;//Get where it's originally facing
        while (script.has_collided == false)//Wait for Collision
        {
            yield return new WaitForFixedUpdate();
        }
        if (script.legit_target == false)//Check if target even is valid
        {
            script.coroutines_running--;
            yield return null;
        }
        else 
        {
            Vector3 current = script.transform.position - start_pos;//Get the vector representing distance traveled
            Vector3 path = Vector3.Project(current, start_forward);//Use current to project where the bullet would go if it didn't turn
            float ang = Vector3.Angle(path, current);
            if (ang < 30)//Award bonus only if deviation is less than 30 degrees
            {
                if (ang < 5)//If deviation is less than 5 degrees,go ahead and give full bonus
                {
                    ang = 0;
                }
                script.crit_chance += (30 - ang) * .01;
            }
            print(ang.ToString());
            script.coroutines_running--;
            yield return null;
        }
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
        if (homes)
        {
            script.home.layer = home_layer;
            script.home_speed = home_speed;
            script.home_radius = home_radius;
            script.homes = true;
        }
        script.gun_reference = this;//For gaining exp 
        next_time = Time.time + reload_time;
        if (Claimed_Gun_Mods != null)
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
                //transfer the script from the weapon to the item image before destroying the weapon instance
                CopyComponent<Gun>(Player.equipped_weapons[Index].current_reference.GetComponent<Gun>(), Player.equipped_weapons[Index]._item_image);
                Player.equipped_weapons[Index]._item_image.GetComponentInChildren<ItemImage>().item_script = Player.equipped_weapons[Index]._item_image.GetComponent<Gun>();
                Destroy(Player.equipped_weapons[Index].current_reference);
            }
            if (Index == Player.main_weapon_index)
            {
                Player.gun = this;
                Player.Gun = current_reference;
                current_reference.GetComponent<Renderer>().enabled = true;
            }
            Player.equipped_weapons[Index] = this;
     }

    private void BringUpLevelUpTable()
    {
        GunTable.gun_for_consideration = this;
        GunLevelUp.transform.SetParent(main_bar_tr);
        GunLevelUp.transform.localPosition = Vector3.zero;
    }

    private static class GunTable
    {
        static GunTableButton[] buttons;
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

        public static void InitGunTable()
        {
            Button[] b = Gun.GunLevelUp.GetComponentsInChildren<Button>();
            for (int i = 0; i < b.Length - 1; i++)
            {
                b[i].gameObject.AddComponent<GunTableButton>();
            }
            buttons = Gun.GunLevelUp.GetComponentsInChildren<GunTableButton>();
            
            for(int i = 0;i < buttons.Length;i++)
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
                g.GetComponentInChildren<Text>().text = gun_ability_names[g.index];
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
                    buttons[temp].method = Gun.Gun_Mods[temp];                                    
                }
                gun_for_consideration.ButtonsAreAssigned();
            }
            if (gun_for_consideration.points == 0 && gun_for_consideration.level_up_indication)
            {
                Destroy(gun_for_consideration.level_up_indication.gameObject);
            }


        }

    }

    private class GunTableButton : MonoBehaviour
    {
        public Button button;
        public Gun_Abilities method;
        public int index;
        
        void OnMouseDown()
        {
            if (GunTable.gun_for_consideration.points > 0 && button.enabled && method != null)
            {
                --GunTable.gun_for_consideration.points;
                GunTable.gun_for_consideration.Claimed_Gun_Mods += method;
                GunTable.gun_for_consideration.claimed_gun_ability[index] = true;
                ColorBlock cb = button.colors;
                cb.disabledColor = Color.yellow;
                button.colors = cb;
                button.GetComponentInChildren<Text>().color = Color.red;
                button.interactable = false;
            }
        }
    }

       

    
    /*Functions below are virtual as to allow derived classes their own static variables for keeping track of assignment*/
    protected virtual bool AreGunLevelUpButtonsAssignedForClass() { return assigned_buttons; }
    protected virtual void ButtonsAreAssigned() { assigned_buttons = true; }

    void Start()
    {
        name = "Basic \n";
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
        if (Gun_Mods[0] == null)
        {
            Gun_Mods[0] = Marksman;
        }
      
       
    }
    
    

 }
