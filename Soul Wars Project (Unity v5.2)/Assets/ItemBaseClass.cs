
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
            transform.parent = null;//For it to detach from enemy and not be destroyed
            gameObject.AddComponent<Rigidbody>();//For it to drop on ground
            gameObject.AddComponent<BoxCollider>();//For it to stay on ground
            /*Indication for players to know it's dropped*/
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
    
}

