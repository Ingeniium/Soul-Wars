
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerController : GenericController
{
    public string player_name//Name of the player to display
    {
        get { return _player_name; }
        set
        {
            _player_name = value;
            name_display_show.GetComponentInChildren<Text>().text = value;
        }
    }
    private string _player_name;
    public GameObject name_display;//Prefab of below
    public GameObject name_display_show;//Object that displays name
    public static PlayerController Client;//Static reference to the playercontroller that's connected client-side
    public Text mod_text;//Text object that displays mods of units hovered-over by the mouse
    public Rigidbody rb;//Rigidbody attached to the object
    public Canvas cooldown_canvas;//Prefab of below
    private Canvas cooldown_canvas_show;//Object that shows the cooldown of a gun
    public Camera cam;//Prefab of below
    public Camera cam_show//Camera for client-side viewing
    {
        get { return _cam_show; }
        private set
        {
            _cam_show = value;
        }
    }
    private Camera _cam_show;
    public bool equip_action = true;//Whether player can act or not.Useful when dealing with ui buttons
    public Canvas player_interface;//Prefab of below
    public Canvas player_interface_show;//The Object that's responsible for displaying things like health and shield bars
    public HealthDefence HP;//Health defence object

    [Command]
    void CmdSetShield()
    {
        RpcSetShield();
    }

    [ClientRpc]
    void RpcSetShield()
    {
        StartCoroutine(SetShield());
    }

    [Command]
    public void CmdApplyGunAbilities(GameObject obj,int index)
    {
        obj.GetComponent<Gun>().RpcApplyGunAbilities(index);
    }


    IEnumerator SetShield()
    {
        while (GetComponentsInChildren<HealthDefence>().Length < 2) //Waits until the shield is created on server
        {
            yield return new WaitForEndOfFrame();
        }
        Shield = GetComponentsInChildren<HealthDefence>()[1].gameObject;//Assigns reference to the shield(using the fact that it's a child)
        HealthDefence SP = Shield.GetComponent<HealthDefence>();
        SP.scale_factor = 2.5f;//Rescales obj
        SP.Controller = this;//Makes a not of who owns it
        /*This changes the shield bar from a local shield bar object to the main shield bar
         provided that the player who owns it is the one who is connected locally.
         Since player_interface_show would only be instantiated if Start() was executed
         locally,the follwing check would be true only if the owner is playing locally on 
         that instance of the game running.*/
        if (player_interface_show)
        {
            SP.health_bar_show = player_interface_show.GetComponentsInChildren<Slider>()[3].gameObject as GameObject;
            SP.hp_string = SP.health_bar_show.GetComponentInChildren<Text>();
            SP.hp_string.text = "<b>" + SP.HP + "</b>";
            SP.hp_bar = SP.health_bar_show.GetComponentInChildren<Slider>().GetComponent<RectTransform
                >();
            SP.maxWidth = SP.hp_bar.rect.width;
        }

    }

    void Awake()
    {
        name_display_show = Instantiate(name_display) as GameObject;
        HP = GetComponent<HealthDefence>();
        HP.Controller = this;
    }

    void Start()
    {
        /*The code that follows operates locally on ONE client.*/
        if (!isLocalPlayer)
        {
            return;
        }
        else
        {
            cam_show = Instantiate(cam) as Camera;
            if (cam_show.GetComponent<AudioListener>())
            {
                Destroy(cam_show.GetComponent<AudioListener>());
            }

            player_interface_show = Instantiate(player_interface) as Canvas;
            player_interface_show.worldCamera = cam_show;
            cam_show.GetComponent<PlayerFollow>().Player = this;

            Client = this;

            rb = GetComponent<Rigidbody>();
            if (cam_show && cam_show.enabled)
            {
                cam_show.transform.rotation = cam.transform.rotation;
            }

            HP.health_bar_show = player_interface_show.GetComponentsInChildren<Slider>()[1].gameObject as GameObject;
            HP.hp_string = HP.health_bar_show.GetComponentInChildren<Text>();
            HP.hp_string.text = "<b>" + HP.HP + "</b>";
            HP.hp_bar = HP.health_bar_show.GetComponentInChildren<Slider>().GetComponent<RectTransform
                >();
            HP.maxWidth = HP.hp_bar.rect.width;
            mod_text = player_interface_show.GetComponentInChildren<Text>();
            NetworkMethods.Instance.CmdSpawn("Bronze Shield",
                gameObject,
                new Vector3(.87f, .134f, 0),
                new Quaternion(0, 0, 0, 0));
            CmdSetShield();
            CmdSyncShieldPos();
        }
        StartCoroutine(SetNameDisplay());

    }


    IEnumerator SetNameDisplay()
    {
        yield return new WaitForSeconds(.3f);
        CmdNameChange(true, _player_name);
    }


    [Command]
    void CmdShoot()
    {
        try
        {
            if (main_gun.HasReloaded())
            {
                main_gun.Shoot();
            }
        }
        catch (System.NullReferenceException e)
        {


        }
    }

    [Command]
    public void CmdSetGun(GameObject g, int _level, uint _points, int _experience, int _next_level, int[] indeces)
    {
        g.GetComponent<Gun>().RpcSetGun(_level, _points, _experience, _next_level, indeces);
    }

    [Command]
    public void CmdDestroy(GameObject g)
    {
        NetworkServer.Destroy(g);
    }


    [Command]
    void CmdNameChange(bool again, string name)
    {
        RpcNameChange(again, name);
    }


    [ClientRpc]
    void RpcNameChange(bool again, string name)
    {
        name_display_show.GetComponentInChildren<Text>().text = name;
        name_display_show.GetComponent<HPbar>().Object = this.gameObject;
        if (again)
        {
            Client.CmdNameChange(false, name);
        }
    }

    void Update()
    {
        if (!isLocalPlayer || !equip_action)
        {
            return;
        }
        if ((Input.inputString.Contains("q") || (Input.GetMouseButton(0) && Input.inputString.Contains("")) && !blocking))
        {
            int index = weapons.FindIndex(delegate (Gun g)
            {
                return (g && g.button == Input.inputString);
            });
            if (index != -1)
            {
                CmdEquipGun(weapons[index].gameObject);
                StartCoroutine(weapons[index].item_image_show.GetComponentInChildren<ItemImage>().Cooldown(weapons[index].reload_time));
                CmdShoot();
            }
        }
       else if (Input.GetMouseButton(1) && !blocking)
       {
            CmdStartShieldBlocking();         
       }
       else if (!Input.GetMouseButton(1) && blocking)                                
       {
            CmdEndShieldBlocking();
       }

            
    }
        
	
	
	// Update is called once per frame
	void FixedUpdate ()
    {
        if (isLocalPlayer)
        {
            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");
            Vector3 total_move = new Vector3(moveHorizontal, 0, -1 * moveVertical);
            rb.velocity = speed * total_move;
        }
    }
    /*Unfortunately,shields and guns on client-only players move like they're lagging behind,
     so the following functions are designed to somewhat mitigate the problem by removing their 
     child status.This sync soultion is also the reason why they no longer have netwwork 
     transforms*/
    [Command]
    void CmdSyncShieldPos() 
    {
        RpcSyncShieldPos();
    }

    [ClientRpc]
    void RpcSyncShieldPos()
    {
        StartCoroutine(SyncShieldPos());
    }

    IEnumerator SyncShieldPos()
    {
        while(!Shield)
        {
            yield return new WaitForEndOfFrame();
        }
        Shield.GetComponent<NetworkTransform>().enabled = false;
        Shield.transform.parent = null;
        while(this)
        {
            yield return new WaitForFixedUpdate();
            float x;
            float z;
            float rot;
            if(!blocking)
            {
                x = transform.right.x;
                z = transform.right.z;
                rot = 0;
            }
            else
            {
                x = transform.forward.x;
                z = transform.forward.z;
                rot = 90;
            }
            Vector3 proj = new Vector3(x + transform.position.x,
            transform.position.y,
            z + transform.position.z);
            Shield.transform.rotation = transform.rotation * Quaternion.Euler(0, rot, 0);
            Shield.transform.position = proj;
            Vector3 dif = Shield.transform.position - transform.position;
            dif /= 2;
            Shield.transform.position -= dif;
        }
    }
    
    [Command]
    public void CmdSyncGunPos()
    {
        RpcSyncGunPos();
    }

    [ClientRpc]
    void RpcSyncGunPos()
    {
        StartCoroutine(SyncGunPos());
    }

    public IEnumerator SyncGunPos()
    {       
         Gun[] children = GetComponentsInChildren<Gun>();
         foreach (Gun g in children)
         {
            g.transform.SetParent(null);
            g.GetComponent<NetworkTransform>().enabled = false;
         }
         while (this)
         {
             yield return new WaitForFixedUpdate();
             foreach (Gun g in children)
             {
                 if (g)
                 {
                     Vector3 proj = new Vector3(transform.forward.x + transform.position.x,
                     transform.position.y,
                     transform.forward.z + transform.position.z);
                     g.transform.rotation = transform.rotation;
                     g.transform.position = proj;
                     Vector3 dif = g.transform.position - transform.position;
                     dif /= 2;
                     g.transform.position -= dif;
                 }
             }
         }
    }
    

  
}