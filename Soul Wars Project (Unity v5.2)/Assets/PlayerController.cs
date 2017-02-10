using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : GenericController {
    private Vector3 total_move;
	public GameObject Shield;
    public GameObject Gun;
    public Gun gun;//The Equipped weapon that the player currently wields
    public List<Gun> equipped_weapons;
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
            gun = equipped_weapons[value];
            equipped_weapons[_main_weapon_index].current_reference.GetComponent<Renderer>().enabled = false;
            gun.current_reference.GetComponent<Renderer>().enabled = true;
            _main_weapon_index = value;
        }
    }
    public float speed = 30f;
    private Transform str;
    private float moveHorizontal;
    private float moveVertical;
	private Rigidbody rb;
	private Transform tr;
    private bool switching = false;
    private bool defending = false;
    private float turn = 0;
    public Canvas cooldown_canvas;
    private Canvas cooldown_canvas_show;
    public bool equip_action = true;
	void Start () 
    {
		rb = GetComponent<Rigidbody>();
		tr = GetComponent<Transform> ();
        str = Shield.GetComponent<Transform>();
        gun = Gun.GetComponent<Gun>();
        gun.current_reference = Gun;
        //Giving the gun its own item image object,and setting it to be the 1st equipped weapon
        gun._item_image = Instantiate(gun.item_image,new Vector3(-2.55f,0,-3.75f), gun.item_image.transform.rotation) as GameObject;
        gun._item_image.GetComponentInChildren<ItemImage>().item_script = gun;
        gun._item_image.GetComponentInChildren<ItemImage>().Item_ = gun.asset_reference;
        gun._item_image.GetComponentInChildren<BoxCollider>().center = Vector2.zero;
        GameObject weapons_bar = GameObject.FindGameObjectWithTag("Weapons");
        gun._item_image.transform.SetParent(weapons_bar.transform);
        equipped_weapons.Add(gun);
        max_weapon_num = 2;
	}
	void Update() 
    {
        if (equip_action)
        {
            if (Input.GetMouseButtonDown(1) && switching != true)
            {
                switching = true;
            }
            else if (Input.GetMouseButtonDown(0) && gun.HasReloaded())
            {
                cooldown_canvas_show = Instantiate(cooldown_canvas, gun._item_image.transform.position + new Vector3(.25f, 0, 0), gun._item_image.transform.rotation) as Canvas;
                cooldown_canvas_show.transform.SetParent(gun._item_image.transform);
                gun.Shoot();
                StartCoroutine(Cooldown(cooldown_canvas_show, gun));
            }
            else if (Input.GetMouseButton(2) && max_weapon_num > 1)
            {
                int p = 1;
                if (Input.GetMouseButton(0))
                {
                    for (int i = 1; equipped_weapons[max_weapon_num - i] == null; i++) { p = i; }
                    if (main_weapon_index > 0)
                    {
                        main_weapon_index = main_weapon_index - p;
                    }
                    else
                    {
                        main_weapon_index = max_weapon_num - p;
                    }
                }
                else if (Input.GetMouseButton(1))
                {

                    if (main_weapon_index < max_weapon_num - 2)
                    {
                        for (int i = 1; equipped_weapons[main_weapon_index + i] == null; i++) { p = i; }
                        main_weapon_index = main_weapon_index + p;
                    }
                    else
                    {
                        for (int i = 0; equipped_weapons[i] == null; i++) { p = i; }
                        main_weapon_index = 0 + p;
                    }
                }

            }
        }
        //else { equip_action = true; }

            if (switching)
            {
                turn += 10;
                if (turn == 90)
                {
                    turn = 0;
                    switching = false;
                }
                if (!defending)
                {
                    str.RotateAround(tr.position, Vector3.up, -10f);

                    speed -= .005f;
                    if (turn == 0)
                    {
                        defending = true;
                        if (Shield.GetComponent<HealthDefence>().regeneration == false)
                        {
                            Shield.GetComponent<HealthDefence>().shield_collider.enabled = true;
                        }

                    }
                }
                else
                {
                    str.RotateAround(tr.position, Vector3.up, 10f);

                    if (turn == 0)
                    {
                        defending = false;
                        Shield.GetComponent<HealthDefence>().shield_collider.enabled = false;
                    }
                    speed += .005f;
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

    IEnumerator Cooldown(Canvas cooldown,Gun arg_gun)
    {

        int seconds = (int)arg_gun.reload_time;
        Text cooldown_text = cooldown.GetComponentInChildren<Text>();
        cooldown_text.color = Color.yellow;
        if (seconds > 0)
        {
            cooldown_text.text = seconds.ToString();
        }
        else
        {
            cooldown_text.text = arg_gun.reload_time.ToString();
        }
        yield return new WaitForSeconds(arg_gun.reload_time - (float)seconds);
        while (!arg_gun.HasReloaded())
        {
            --seconds;
            yield return new WaitForSeconds(1);
            cooldown_text.text = seconds.ToString();
        }
        Destroy(cooldown.gameObject);
    }
}