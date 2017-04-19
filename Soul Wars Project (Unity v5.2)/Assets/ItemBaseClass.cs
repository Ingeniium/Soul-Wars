
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Xml.Linq;
using System.Linq;
public abstract class Item : NetworkBehaviour,IRecordable
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
    public bool set = false;
    public static PlayerController Player;//Reference to the Player
    protected GenericController unit_reference;
    public Inventory inv;//Reference to the Inventory obj
    //protected static Transform main_bar_tr;//Reference to the transform of the Player Bar Canvas
    public Canvas drop_canvas;
    protected Canvas drop_canvas_show;//Canvas for showing the name of dropped items upon mouse hovering
    protected bool dropped = false;
    public bool in_inventory = false;
    public Canvas item_options;
    protected Canvas item_options_show;//Canvas for showing item options upon right click
    public GameObject item_image;
    public GameObject _item_image;//Reference to item image instance
    public GameObject current_reference;//Reference to the current object script works for
    protected int index;//Index for equipment
    public GameObject asset_reference;
    protected static System.Random rand = new System.Random();
    protected PlayerController _client_user;
    public PlayerController client_user
    {
        get { return _client_user; }
        set
        {
            if (value != null)
            {
                _client_user = value;
                inv = _client_user.hpbar_show.GetComponentInChildren<Inventory>();
                OnClientUserChange();
            }
        }
    }

    protected abstract void OnClientUserChange();
    public abstract string GetImagePreviewString();

    protected virtual string GetBaseName()
    {
        return ToString();
    }

    public void DropItem(ref double chance)
    {
        if (rand.NextDouble() <= chance)
        {
            GameObject item = Instantiate(current_reference, current_reference.transform.position, current_reference.transform.rotation) as GameObject;
            Item script = item.GetComponent<Item>();
            script.unit_reference = null;
            item.layer = 0;
            item.transform.parent = null;//For it to detach from enemy and not be destroyed
            item.AddComponent<Rigidbody>();//For it to drop on ground
            item.AddComponent<BoxCollider>();//For it to stay on ground
            /*Indication for players to know it's dropped*/
            script.drop_canvas_show = Instantiate(drop_canvas, transform.position + new Vector3(0,0,.5f), drop_canvas.transform.rotation) as Canvas;
            script.drop_canvas_show.GetComponentInChildren<Text>().text = GetBaseName();
            Destroy(script.drop_canvas_show.gameObject, 1f);
            //for code checking state
            script.dropped = true;
            script.index = -1;
            chance = 0;
        }
    }
    protected void DropItem()//Overload for when when a player decides to drop item
    {
        GameObject item = null;
        dropped = true;
        index = -1;
        unit_reference = null;
        /*If respective gameobject exists, drop that instead of creating another instance
         and copying the script to it*/
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
        item.GetComponent<Renderer>().enabled = true;//Make sure it's visible(in case of it being dropped while equipped as sub weapon)
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
        unit_reference = Player;
       _item_image = Instantiate(item_image, item_image.transform.position, item_image.transform.rotation) as GameObject;
       CopyComponent<Item>(this,_item_image);//This is done due to the destruction of the actual gameobject;
       _item_image.GetComponentInChildren<ItemImage>().item_script = _item_image.GetComponent<Item>();
        inv.InsertItem(ref _item_image);//Put it in inventory
       _item_image.GetComponent<Item>()._item_image = _item_image;
    }


    public abstract void PrepareItemForUse();//Creates and sets up gameobject
    public abstract void Options();//Sets up options

    void OnMouseEnter()
    {
        if (dropped)//Shows item name on while on the ground
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

    public abstract XElement RecordValuesToSaveFile();
    public abstract void RecordValuesFromSaveFile(XElement element);
    protected abstract void SetBaseStats();
    
}

