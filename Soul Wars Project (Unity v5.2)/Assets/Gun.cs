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
    public static PlayerController Player;
    protected static Inventory inv;
    protected static GameObject weapons_bar;
    protected string name;
    public Canvas drop_canvas;
    protected Canvas drop_canvas_show;
    protected bool dropped = false;
    public bool in_inventory = false;
    public Canvas item_options;
    protected Canvas item_options_show;
    public GameObject item_image;
    public GameObject _item_image;
    public GameObject asset_reference;
    public GameObject current_reference;
    protected int index;
    public void DropItem(ref double chance)
    {
        System.Random rand= new System.Random();
        if (rand.NextDouble() <= chance)
        {
            gameObject.layer = 0;
            transform.parent = null;
            gameObject.AddComponent<Rigidbody>();
            gameObject.AddComponent<BoxCollider>();
            drop_canvas_show = Instantiate(drop_canvas, transform.position + new Vector3(0,0,.5f), drop_canvas.transform.rotation) as Canvas;
            drop_canvas_show.GetComponentInChildren<Text>().text = name;
            Destroy(drop_canvas_show.gameObject, 1f);
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
       _item_image = Instantiate(item_image, item_image.transform.position, item_image.transform.rotation) as GameObject;
       CopyComponent<Item>(this,_item_image);//This is done due to the destruction of the actual gameobject;
       _item_image.GetComponentInChildren<ItemImage>().item_script = _item_image.GetComponent<Item>();
       _item_image.GetComponentInChildren<ItemImage>().Item_ = asset_reference;
        inv.InsertItem(ref _item_image);
       _item_image.GetComponent<Item>()._item_image = _item_image;
    }


    public abstract void PrepareItemForUse();
    //public abstract void ReuseItem();
    public abstract void Options();

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
	public GameObject Bullet;
	protected GameObject bullet;
	public float reload_time = 2.33f;
	public float next_time = -1f;
    public float home_speed;
	public float home_radius = 5.0f;
	public int damage = 2;
	public Transform barrel_end;
    public Color color;
    public int layer;
    public int home_layer;
    public virtual void Shoot()
    {
        bullet = Instantiate(Bullet, barrel_end.position, gameObject.transform.localRotation) as GameObject;
        ReadyWeaponForFire(ref bullet);
        bullet.GetComponent<Rigidbody>().AddForce(barrel_end.forward, ForceMode.Impulse);
    }

    protected void ReadyWeaponForFire(ref GameObject weapon_fire)
    {
        weapon_fire.GetComponent<Renderer>().material.color = color;
        BulletScript script = weapon_fire.GetComponent<BulletScript>();
        script.damage = damage;
        script.home_radius = home_radius;
        script.gameObject.layer = layer;
        script.home.layer = home_layer;
        script.home_speed = home_speed;
        next_time = Time.time + reload_time;
        
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
        for (int i = buttons.Length - 2; i > Player.max_weapon_num; i--)
        {
            buttons[buttons.Length - 1].transform.localPosition = buttons[i].transform.localPosition;
            Destroy(buttons[i].gameObject);
        }
        for (int i = 0; i < buttons.Length - 1; i++)
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
                buttons[buttons.Length - 1].transform.localPosition = buttons[i].transform.localPosition;
                Destroy(buttons[i].gameObject);
            }
        }
        buttons[buttons.Length - 1].onClick.AddListener(delegate 
        { 
            DropItem();
            _item_image.GetComponentInChildren<ItemImage>().option_showing = false;
            Item.Player.equip_action = true;
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
                if(Player.equipped_weapons[index] != null)
                {
                    Player.equipped_weapons[index] = null;
                }
            }
            index = Index;
            if (Player.equipped_weapons[Index])
            {
                Player.equipped_weapons[Index].index = -1;
                inv.InsertItem(ref Player.equipped_weapons[Index]._item_image);
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
       
    

    void Start()
    {
        name = "Basic \n";
        if (Player == null)
        {
            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
            inv = GameObject.FindGameObjectWithTag("Inventory").GetComponent<Inventory>();
            weapons_bar = GameObject.FindGameObjectWithTag("Weapons");
        }
    }
    
 }
