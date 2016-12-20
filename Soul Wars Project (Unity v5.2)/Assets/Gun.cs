using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections;
public abstract class Item : MonoBehaviour
{
    T CopyComponent<T>(T original, GameObject destination) where T : Component
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
    protected string name;
    public Canvas drop_canvas;
    protected Canvas drop_canvas_show;
    protected bool dropped = false;
    public GameObject item_image;
    protected GameObject _item_image;
    public GameObject asset_reference;
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
        }
    }
    protected void RetrieveItem()
    {
       _item_image = Instantiate(item_image, item_image.transform.position, item_image.transform.rotation) as GameObject;
       CopyComponent<Item>(this,_item_image);
       _item_image.GetComponentInChildren<ItemImage>().item_script = _item_image.GetComponent<Item>();
       _item_image.GetComponentInChildren<ItemImage>().Item = asset_reference;
       GameObject.FindGameObjectWithTag("Inventory").GetComponent<Inventory>().InsertItem(ref _item_image);
     
    }
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
        if (dropped)
        {
            print("retrieve");
            RetrieveItem();
            if (drop_canvas_show)
            {
                Destroy(drop_canvas_show.gameObject);
            }
            Destroy(gameObject);
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
	private GameObject bullet;
	public float reload_time = 1.0f;
	public float next_time = -1f;
	public float home_radius = 5.0f;
	public int damage = 2;
	public Transform barrel_end;
    public Color color;
    public int layer;
    public int home_layer;
    public virtual void Shoot()
    {
        bullet = Instantiate(Bullet, barrel_end.position, barrel_end.rotation) as GameObject;
        ReadyWeaponForFire(ref bullet);
        bullet.GetComponent<Rigidbody>().AddForce(barrel_end.forward, ForceMode.Impulse);//works
    }
    protected void ReadyWeaponForFire(ref GameObject weapon_fire)
    {
        weapon_fire.GetComponent<Renderer>().material.color = color;
        BulletScript script = weapon_fire.GetComponent<BulletScript>();
        script.damage = damage;
        script.home_radius = home_radius;
        script.gameObject.layer = layer;
        script.home.layer = home_layer;
        next_time = Time.time + reload_time;
    }
    public override string ToString()
    {
        return string.Format(name + "Launches a powerful arrow.\n Homing : {0} Damage : {1} \n Reload Time : {2}", home_radius, damage, reload_time);                
    }
    void Start()
    {
        name = "Basic \n";
    }
    
 }
