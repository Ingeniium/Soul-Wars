
using UnityEngine;
using UnityEngine.Networking;
using System.Xml.Linq;
using System.Collections.Generic;
public abstract class Item : NetworkBehaviour,IRecordable
{
    /*Note that the following method (CopyComponent) WAS NOT CREATED BY ME!
     * THE CREDIT GOES TO Shaffe
     */ // http://answers.unity3d.com/questions/458207/copy-a-component-at-runtime.html
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
    public Canvas drop_canvas;
    protected Canvas drop_canvas_show;//Canvas for showing the name of dropped items upon mouse hovering
    protected bool dropped = false;
    public bool in_inventory = false;
    public GameObject item_image;
    public GameObject item_image_show;//Reference to item image instance
    protected static System.Random rand = new System.Random();
	public delegate void Options();
    public PlayerController _client_user;//this being protected actually causes a null ref error in bulletscript
    public PlayerController client_user
    {
        get { return _client_user; }
        set
        {
            _client_user = value;
            if (_client_user != null && _client_user.hpbar_show)
            {
                OnClientUserChange();
            }
        }
    }

    protected abstract void OnClientUserChange();
    public abstract string GetImagePreviewString();

    public abstract string GetBaseName();

    public abstract void PrepareItemForUse();//Creates and sets up gameobject
    public abstract List<string> GetOptionsStrings();//Sets up options string
    public abstract Options GetOptionsFuncs();//Sets up options funcs

    public void OnPointerEnter()
    {
        /*if (dropped)//Shows item name on while on the ground
        {
            drop_canvas_show = Instantiate(drop_canvas, transform.position + new Vector3(0, 0, .5f), drop_canvas.transform.rotation) as Canvas;
            drop_canvas_show.GetComponentInChildren<Text>().text = name;
        }*/
}

public void OnPointerClick()
    {
           /* if (dropped)
            {
                try
                {
                    in_inventory = true;
                    RetrieveItem();
                    if (drop_canvas_show)
                    {
                        Destroy(drop_canvas_show.gameObject);
                    }
                    PlayerController.Client.CmdDestroy(gameObject);
                }
                catch (System.Exception e)
                {
                    if (_item_image)
                    {
                        Destroy(_item_image);
                    }
                    if (drop_canvas_show)
                    {
                        Destroy(drop_canvas_show.gameObject);
                    }
                }
            }
            */
           
        
    }

    public void OnPointerExit()
    {
        if (drop_canvas_show)
        {
            Destroy(drop_canvas_show.gameObject);
        }
    }

    public abstract XElement RecordValuesToSaveFile();
    public abstract void RecordValuesFromSaveFile(XElement element);
    public abstract void SetBaseStats();
    
}

