
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

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
    public Text mod_text;
    public GameObject pass_over;//reference to cmd created objects

    private float moveHorizontal;
    private float moveVertical;
    public Rigidbody rb;
    private Transform tr;
    public Canvas cooldown_canvas;
    private Canvas cooldown_canvas_show;
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


    IEnumerator SetShield()
    {
        while (GetComponentsInChildren<HealthDefence>().Length < 2)
        {
            yield return new WaitForEndOfFrame();
        }
        Shield = GetComponentsInChildren<HealthDefence>()[1].gameObject;
        HealthDefence SP = Shield.GetComponent<HealthDefence>();
        SP.scale_factor = 2.5f;
        SP.Controller = this;
        if (hpbar_show)
        {
            SP.health_bar_show = hpbar_show.GetComponentsInChildren<Slider>()[3].gameObject as GameObject;
            SP.hp_string = SP.health_bar_show.GetComponentInChildren<Text>();
            SP.hp_string.text = "<b>" + SP.HP + "</b>";
            SP.hp_bar = SP.health_bar_show.GetComponentInChildren<Slider>().GetComponent<RectTransform
                >();
            SP.maxWidth = SP.hp_bar.rect.width;
        }

    }

    void Awake()
    {
        NameDisplayShow = Instantiate(NameDisplay) as GameObject;
        HP = GetComponent<HealthDefence>();
        HP.Controller = this;
    }

    void Start()
    {

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
            HP = GetComponent<HealthDefence>();

            hpbar_show = Instantiate(hpbar) as Canvas;
            hpbar_show.worldCamera = cam_show;
            cam_show.GetComponent<PlayerFollow>().Player = this;

            Client = this;

            rb = GetComponent<Rigidbody>();
            tr = GetComponent<Transform>();
            if (cam_show && cam_show.enabled)
            {
                cam_show.transform.rotation = cam.transform.rotation;
            }

            HP.health_bar_show = hpbar_show.GetComponentsInChildren<Slider>()[1].gameObject as GameObject;
            HP.hp_string = HP.health_bar_show.GetComponentInChildren<Text>();
            HP.hp_string.text = "<b>" + HP.HP + "</b>";
            HP.hp_bar = HP.health_bar_show.GetComponentInChildren<Slider>().GetComponent<RectTransform
                >();
            HP.maxWidth = HP.hp_bar.rect.width;
            mod_text = hpbar_show.GetComponentInChildren<Text>();
            NetworkMethods.Instance.CmdSpawn("Bronze Shield",
                gameObject,
                new Vector3(.87f, .134f, 0),
                new Quaternion(0, 0, 0, 0));
            CmdSetShield();
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
        NameDisplayShow.GetComponentInChildren<Text>().text = name;
        NameDisplayShow.GetComponent<HPbar>().Object = this.gameObject;
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
            int index = Array.FindIndex(weapons, delegate (Gun g)
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
                StartShieldBlocking();
            }
            if (Input.GetMouseButtonUp(1) && blocking)                                
            {
                EndShieldBlocking();   
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