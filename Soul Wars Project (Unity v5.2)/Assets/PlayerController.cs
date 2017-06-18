using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class PlayerController : GenericController {
    public string player_name
    {
        get { return _player_name; }
        set
        {
            _player_name = value;
            NameDisplayShow.GetComponentInChildren<Text>().text = value;
        }
    }
    [SyncVar] public bool loaded = false;
    private string _player_name;
    public GameObject NameDisplay;
    public GameObject NameDisplayShow;
    private Vector3 total_move;
    public static PlayerController Client;
    private bool shooting = false;
    public Gun gun;//The Equipped weapon that the player currently wields
    public List<Gun> equipped_weapons = new List<Gun>();
    public static List<uint> PlayerIDList = new List<uint>();
    public GameObject pass_over;//reference to cmd created objects

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
                    equipped_weapons[_main_weapon_index]._item_image.GetComponentInChildren<RectTransform>().sizeDelta /= 2;
                    gun._item_image.GetComponentInChildren<RectTransform>().sizeDelta *= 2;
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
    public Rigidbody rb;
	private Transform tr;
    public Canvas cooldown_canvas;
    public Canvas cooldown_canvas_show;
    private Canvas not_homing;
    public Camera cam;
    public Camera cam_show
    {
        get { return _cam_show; }
        private set
        {
            _cam_show = value;
        }
    }
    private Camera _cam_show;
    public bool equip_action = true;
    public Canvas hpbar;
    public Canvas hpbar_show;
    public HealthDefence HP;

    void Awake()
    {
 
            cam_show = Instantiate(cam) as Camera;
            if (cam_show.GetComponent<AudioListener>())
            {
                Destroy(cam_show.GetComponent<AudioListener>());
            }
            HP = GetComponent<HealthDefence>();
            HP.Controller = this;
            hpbar_show = Instantiate(hpbar) as Canvas;
            hpbar_show.worldCamera = cam_show;
            NameDisplayShow = Instantiate(NameDisplay) as GameObject;
            PlayerIDList.Add(netId.Value);
            cam_show.GetComponent<PlayerFollow>().Player = this;
                      
    }


	void Start() 
    {
       
        max_weapon_num = 2;
        shield_collider = Shield.GetComponent<BoxCollider>();
        
        if (!isLocalPlayer)
        {
            cam_show.enabled = false;
            return;
        }
        else
        {
            Client = this;
            shield_collider = Shield.GetComponent<BoxCollider>();
            rb = GetComponent<Rigidbody>();
            tr = GetComponent<Transform>();
            if (cam_show.enabled)
            {
                cam_show.transform.rotation = cam.transform.rotation;
            }
            
            HP.health_bar_show = hpbar_show.GetComponentsInChildren<Slider>()[1].gameObject as GameObject;
            HP.hp_string = HP.health_bar_show.GetComponentInChildren<Text>();
            HP.hp_bar = HP.health_bar_show.GetComponentInChildren<Slider>().GetComponent<RectTransform
                >();
            HealthDefence SP = Shield.GetComponent<HealthDefence>();
            SP.health_bar_show = hpbar_show.GetComponentsInChildren<Slider>()[3].gameObject as GameObject;
            SP.hp_string = SP.health_bar_show.GetComponentInChildren<Text>();
            SP.hp_bar = SP.health_bar_show.GetComponentInChildren<Slider>().GetComponent<RectTransform
                >();
            StartCoroutine(SetNameDisplay());
        }
        
	}

    IEnumerator SetNameDisplay()
    {
        yield return new WaitForSeconds(.3f);
        CmdNameChange(true,_player_name);
    }



    [Command]
    public void CmdSpawnItem(string asset_reference,Vector3 pos,Quaternion rot,bool child)
    {
        GameObject obj = Resources.Load(asset_reference) as GameObject;
        pass_over = Instantiate(obj, pos, rot) as GameObject;
        NetworkServer.SpawnWithClientAuthority(pass_over, connectionToClient);
        if (child)
        {
            NetworkMethods.Instance.RpcSetParent(pass_over, gameObject, pos,rot);
        }
        pass_over.GetComponent<Item>().client_user = this;
        RpcSetPassOver(pass_over);
    }

    [Command]
    public void CmdEquipGun(GameObject g)
    {
        RpcEquipGun(g);
    }

    [ClientRpc]
     void RpcEquipGun(GameObject g)
    {
        Gun = g;
        gun = g.GetComponent<Gun>();
    }

    [ClientRpc]
    void RpcSetPassOver(GameObject g)
    {
        pass_over = g;
        pass_over.GetComponent<Item>().client_user = this;
    }

    [Command]
    void CmdShoot()
    {
        try
        {
            if (gun.HasReloaded() && !shield_collider.enabled)
            {
                gun.Shoot();
            }
        }
        catch(System.NullReferenceException e)
        {
            
        
        }
    }

    [Command]
    public void CmdSetGun(GameObject g,int _level,uint _points, int _experience,int _next_level, int[] indeces)
    {
        g.GetComponent<Gun>().RpcSetGun(_level, _points, _experience, _next_level, indeces);
    }
   
    [Command]
    public void CmdDestroy(GameObject g)
    {
        NetworkServer.Destroy(g);
    }

   
    [Command]
    void CmdNameChange(bool again,string name)
    {
        RpcNameChange(again,name);
    }


    [ClientRpc]
    void RpcNameChange(bool again,string name)
    {
        NameDisplayShow.GetComponentInChildren<Text>().text = name;
        NameDisplayShow.GetComponent<HPbar>().Object = this.gameObject;
        if (again)
        {
            PlayerController.Client.CmdNameChange(false,name);
        }
    }

	void Update() 
    {
        if (!isLocalPlayer || !equip_action)
        {
            return;
        }
        else
        {
           
            if (Input.GetMouseButtonDown(0) && !cooldown_canvas_show)
            {
                cooldown_canvas_show = Instantiate(cooldown_canvas, gun._item_image.transform.position + new Vector3(.25f, 0, 0), gun._item_image.transform.rotation) as Canvas; 
                cooldown_canvas_show.transform.SetParent(gun._item_image.transform);
                StartCoroutine(Cooldown.NumericalCooldown(cooldown_canvas_show, gun.reload_time));
                CmdShoot();
                
            }
            else if (Input.GetMouseButton(1) && !shield_collider.enabled)
            {
                if (Gun == null)
                {
                    Gun = pass_over;
                }
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
        if (isLocalPlayer)
        {
            moveHorizontal = Input.GetAxis("Horizontal");
            moveVertical = Input.GetAxis("Vertical");
            total_move = new Vector3(moveHorizontal, 0, -1 * moveVertical);
            rb.velocity = speed * total_move;
        }
    }

  
}