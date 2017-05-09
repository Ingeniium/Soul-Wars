﻿using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class PlayerController : GenericController {
    private Vector3 total_move;
    public static PlayerController Client;
    private bool shooting = false;
    public Gun gun;//The Equipped weapon that the player currently wields
    public List<Gun> equipped_weapons = new List<Gun>();
    public static List<uint> PlayerIDList = new List<uint>();

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
	private Rigidbody rb;
	private Transform tr;
    public Canvas cooldown_canvas;
    private Canvas cooldown_canvas_show;
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
         
            PlayerIDList.Add(netId.Value);
            cam_show.GetComponent<PlayerFollow>().Player = this;
                      
            
    }


	void Start() 
    {
        if (netId.Value == 3)
        {
            CmdEnemySpawn(SpawnManager.EnemySpawnPoints[0].transform.position + SpawnManager.EnemySpawnPoints[0].spawn_direction * 3);
        }
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
            Shield.GetComponent<HealthDefence>().Controller = this;
            if (cam_show.enabled)
            {
                tr.position = SpawnManager.AllySpawnPoints[0].transform.position + SpawnManager.AllySpawnPoints[0].spawn_direction;
                cam_show.transform.position = tr.position + new Vector3(0, 15, 0);
                cam_show.transform.rotation = cam.transform.rotation;
            }
            gun = Gun.GetComponent<Gun>();
            equipped_weapons.Add(gun);
            max_weapon_num = 2;
            HP.health_bar_show = hpbar_show.GetComponentsInChildren<Slider>()[1].gameObject as GameObject;
            HP.hp_string = HP.health_bar_show.GetComponentInChildren<Text>();
            HP.hp_bar = HP.health_bar_show.GetComponentInChildren<Slider>().GetComponent<RectTransform
                >();
            HealthDefence SP = Shield.GetComponent<HealthDefence>();
            SP.health_bar_show = hpbar_show.GetComponentsInChildren<Slider>()[3].gameObject as GameObject;
            SP.hp_string = SP.health_bar_show.GetComponentInChildren<Text>();
            SP.hp_bar = SP.health_bar_show.GetComponentInChildren<Slider>().GetComponent<RectTransform
                >();
            gun.client_user = this;
            if (!gun.set)
            {
                gun.current_reference = Gun;
                //Giving the gun its own item image object,and setting it to be the 1st equipped weapon
                gun._item_image = Instantiate(gun.item_image, gun.weapons_bar.transform.position, gun.item_image.transform.rotation) as GameObject;
                gun._item_image.GetComponentInChildren<ItemImage>().item_script = gun;
                gun._item_image.GetComponentInChildren<RectTransform>().sizeDelta *= 2;
                gun._item_image.transform.parent = gun.weapons_bar.transform;

            }
            
        }
        
	}
    
    [Command]
    void CmdEnemySpawn(Vector3 pos)
    {
        GameObject Enemy = Instantiate(Resources.Load("Dummy"),pos, Quaternion.identity) as GameObject;
        NetworkServer.Spawn(Enemy);   
    }

    [Command]
    void CmdShoot(Vector3 pos,Quaternion rot)
    {
        GameObject g = Instantiate(Resources.Load("Bullet"), pos, rot) as GameObject;      
        NetworkServer.SpawnWithClientAuthority(g,connectionToClient);
        RpcShoot(g);
    }


    [ClientRpc]
    void RpcShoot(GameObject g)
    {
            try
            {
                gun.Shoot(g);
                shooting = false;
            }
            catch (System.NullReferenceException e)
            {
                Debug.Log(gun != null);
            }
          
    }

    IEnumerator WaitToApplyGunAbilities()
    {
        while (shooting)
        {
            yield return new WaitForEndOfFrame();
        }
        gun.ApplyGunAbilities();
    }
   
    [Command]
    public void CmdDestroy(GameObject g)
    {
        NetworkServer.Destroy(g);
    }

    [Command]
    public void CmdWaitForRespawn(GameObject g)
    {
        RpcWaitForRespawn(g);
    }

    [ClientRpc]
    void RpcWaitForRespawn(GameObject g)
    {
        StartCoroutine(SpawnManager.WaitForRespawn(g.GetComponent<HealthDefence>()));
    }


    [Command]
    void CmdAssignAuthority(NetworkIdentity Id)
    {
        Id.AssignClientAuthority(connectionToClient);
    }

    [Command]
    public void CmdCreateName(string name,GameObject ga)
    {
        GameObject g = Resources.Load("NameDisplay") as GameObject;
        GameObject G = Instantiate(g);
        NetworkServer.Spawn(G);
        G.GetComponentInChildren<Text>().text = name;
        G.GetComponent<HPbar>().Object = ga;
        
    }

	void Update() 
    {
        if (!isLocalPlayer || !equip_action)
        {
            return;
        }
        else
        {
            if (Input.GetMouseButtonDown(0) && gun.HasReloaded() && !shield_collider.enabled)
            {
                cooldown_canvas_show = Instantiate(cooldown_canvas, gun._item_image.transform.position + new Vector3(.25f, 0, 0), gun._item_image.transform.rotation) as Canvas;
                cooldown_canvas_show.transform.SetParent(gun._item_image.transform);
                shooting = true;
                StartCoroutine(WaitToApplyGunAbilities());
                CmdShoot(gun.barrel_end.position, gun.transform.rotation);
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
        if (isLocalPlayer)
        {
            moveHorizontal = Input.GetAxis("Horizontal");
            moveVertical = Input.GetAxis("Vertical");
            total_move = new Vector3(moveHorizontal, 0, -1 * moveVertical);
            rb.AddForce(total_move * speed);
        }
    }

  
}