using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : GenericController {
    private Vector3 total_move;
    public Gun gun;//The Equipped weapon that the player currently wields
    public List<Gun> equipped_weapons = new List<Gun>();
    public int max_weapon_num
    {
        get { return _max_weapon_num; }
        set
        {
            _max_weapon_num = value;
            GameObject weapons_bar = GameObject.FindGameObjectWithTag("Weapons");
            RectTransform r;
            int n = equipped_weapons.Count;
            /*if there's more weapons equipped than the current maximum,put them in inventory
            /Note,later on in development,a need to halt the operation or dropped items upon changing when the inventory
             becomes full itself*/

            if (n > value)
            {
                for (int i = value; i < n; i++)
                {
                    GameObject.FindGameObjectWithTag("Inventory").GetComponent<Inventory>().InsertItem(ref equipped_weapons[i]._item_image);
                }
                equipped_weapons.RemoveRange(value,n - (value - 1));
            }
            else if (n < value)
            {
                for (int i = n; i < value; i++)
                {
                    equipped_weapons.Add(null);
                }
            }

            equipped_weapons.ForEach(delegate(Gun arg_gun) { if (arg_gun) { arg_gun._item_image.gameObject.transform.SetParent(null); } });
            weapons_bar.transform.localScale = new Vector3(.10f * value, weapons_bar.transform.localScale.y, weapons_bar.transform.localScale.z);
            float s = 0;
            equipped_weapons.ForEach(delegate(Gun arg_gun) 
            {
                if (arg_gun)
                {
                    n--;
                    arg_gun._item_image.gameObject.transform.SetParent(weapons_bar.transform);
                    r = arg_gun._item_image.gameObject.GetComponent<RectTransform>();
                    r.anchorMin = new Vector2(1, 0);
                    r.anchorMax = new Vector2(1, 0);
                    r.sizeDelta = new Vector2(1, 1);
                    r.anchoredPosition3D = new Vector3(-209f - 125 * n + 100 * s, 229, 4.6f);
                    s++;
                }
            });
        }
    }
    private int _max_weapon_num;
    private int _main_weapon_index = 0;
    public int main_weapon_index
    {
        get { return _main_weapon_index; }
        set
        {
            if (equipped_weapons[value] != null && _main_weapon_index != value)
            {
                gun = equipped_weapons[value];
                if (equipped_weapons[_main_weapon_index] != null)
                {
                    equipped_weapons[_main_weapon_index].current_reference.GetComponent<Renderer>().enabled = false;
                }
                gun.current_reference.GetComponent<Renderer>().enabled = true;
                _main_weapon_index = value;
            }
            else
            {
                _main_weapon_index = value;
            }
        }
    }
    private float moveHorizontal;
    private float moveVertical;
	private Rigidbody rb;
	private Transform tr;
    public Canvas cooldown_canvas;
    private Canvas cooldown_canvas_show;
    private Canvas not_homing;
    public bool equip_action = true;
	void Start() 
    {
        shield_collider = Shield.GetComponent<BoxCollider>();
		rb = GetComponent<Rigidbody>();
		tr = GetComponent<Transform> ();
        gun = Gun.GetComponent<Gun>();
        
        if (!gun.set)
        {
            gun.current_reference = Gun;
            //Giving the gun its own item image object,and setting it to be the 1st equipped weapon
            gun._item_image = Instantiate(gun.item_image, new Vector3(-2.55f, 0, -3.75f), gun.item_image.transform.rotation) as GameObject;
            gun._item_image.GetComponentInChildren<ItemImage>().item_script = gun;
            gun._item_image.GetComponentInChildren<ItemImage>().Item_ = gun.asset_reference;
            gun._item_image.GetComponentInChildren<BoxCollider>().center = Vector2.zero;
            gun._item_image.transform.parent = GameObject.FindGameObjectWithTag("Weapons").transform;
        }
        equipped_weapons.Add(gun);
        max_weapon_num = 2;
	}

	void Update() 
    {
        if (equip_action)
        {
            if (Input.GetMouseButtonDown(0) && gun.HasReloaded() && !shield_collider.enabled)
            {
                cooldown_canvas_show = Instantiate(cooldown_canvas, gun._item_image.transform.position + new Vector3(.25f, 0, 0), gun._item_image.transform.rotation) as Canvas;
                cooldown_canvas_show.transform.SetParent(gun._item_image.transform);
                gun.Shoot();
                StartCoroutine(Cooldown.NumericalCooldown(cooldown_canvas_show, gun.reload_time));
                
            }
            else if (Input.GetMouseButton(1) && !shield_collider.enabled)
            {
                StartShieldBlocking();
            }
            if (Input.GetMouseButtonUp(1))
            {
                EndShieldBlocking();
            }

            
            if (Input.GetKeyDown("q") && max_weapon_num > 1)
            {
                do
                {
                    if (main_weapon_index == 0)
                    {
                        main_weapon_index = equipped_weapons.Count - 1;
                    }
                    else
                    {
                        main_weapon_index -= 1;
                    }
                } while (equipped_weapons[main_weapon_index] == null);
            }
            else if (Input.GetKeyDown("e") && max_weapon_num > 1)
            {
                do
                {
                    if (main_weapon_index == equipped_weapons.Count - 1)
                    {
                        main_weapon_index = 0;
                    }
                    else
                    {
                        main_weapon_index += 1;
                    }
                } while (equipped_weapons[main_weapon_index] == null);
            }

            if (Input.GetKeyDown("space"))
            {
                ToggleGunHoming(gun);
                if (!gun.homes)
                {
                    not_homing = Instantiate(cooldown_canvas, gun._item_image.transform.position, cooldown_canvas.transform.rotation) as Canvas;
                    Text t = not_homing.GetComponentInChildren<Text>();
                    t.text = "NH";
                    t.color = Color.black;
                    not_homing.transform.parent = gun._item_image.transform;
                }
                else if (not_homing)
                {
                    Destroy(not_homing.gameObject);
                }
            }
        }
        
	}
	
	// Update is called once per frame
	void FixedUpdate ()
    {
		moveHorizontal = Input.GetAxis ("Horizontal");
		moveVertical = Input.GetAxis ("Vertical");
        total_move = new Vector3(moveHorizontal,0,-1*moveVertical);
        rb.AddForce(total_move * speed);
    }

  
}