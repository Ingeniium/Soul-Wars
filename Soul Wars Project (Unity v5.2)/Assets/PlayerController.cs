
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerController : GenericController
{
    public string player_name//Property responsible for syncing name displays
    {
        get { return _player_name; }
        set
        {
            _player_name = value;
            if (!name_display_show)
            {
                name_display_show = Instantiate(name_display) as GameObject;
                HPbar bar = name_display_show.GetComponent<HPbar>();
                bar.Object = gameObject;
                bar.offset = new Vector3(0, 0, -5);
            }
            name_display_show.GetComponentInChildren<Text>().text = value;
            /*A call to sync is made only if the name is being set by the player himself.
              This ensures that at the start of the game,the names of others are correctly
              displayed,and other players know his name.*/
            if (Client && Client.netId == netId)
            {
                CmdSyncNameDisplay(value);
                CmdGetOtherNameDisplays();
            }
        }
    }
    [SyncVar] public string _player_name;//Name of the player to display
    public GameObject name_display;//Prefab of below
    private GameObject name_display_show;//Object that displays name
    public static PlayerController Client;//Static reference to the playercontroller that's connected client-side
    public static List<PlayerController> players = new List<PlayerController>(); //For syncing non syncvar playercontroller across all clients.Chosen over synclist<uint> as those can't be static.
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

    
    override protected void Start()
    {
        /*The code that follows operates locally on ONE client.*/
        base.Start();

        if (!isLocalPlayer)
        {
            return;
        }
        else
        {
            CmdAddPlayer();
            Client = this;
            CmdSyncChildTransforms();
            rb = GetComponent<Rigidbody>();
            InitializeCamera();
            InitializePlayerInterface();
            NetworkMethods.Instance.CmdSpawn("Bronze Shield",
                gameObject,
                new Vector3(.87f, .134f, 0),
                new Quaternion(0, 0, 0, 0));
            StartCoroutine(SetShield());
            players.RemoveNullM();
        }

    }

    protected override IEnumerator AddUnitToPlayersAlive()
    {
        while (!PlayersAlive.Instance)
        {
            yield return new WaitForEndOfFrame();
        }
        PlayersAlive.Instance.Players.Add(netId.Value);
    }
    

    [Command]
    void CmdSyncChildTransforms()
    {
        if (CustomNTC.first)
        {
            CustomNTC.first.RpcSyncNTCS();
        }

    }


    /*Code for instantiating and making the camera properly "follow"
     the player.*/
    void InitializeCamera()
    {
        cam_show = Instantiate(cam) as Camera;
        if (cam_show.GetComponent<AudioListener>())
        {
            Destroy(cam_show.GetComponent<AudioListener>());
        }
        player_interface_show = Instantiate(player_interface) as Canvas;
        player_interface_show.worldCamera = cam_show;
        cam_show.GetComponent<PlayerFollow>().Player = this;
        if (cam_show && cam_show.enabled)
        {
            cam_show.transform.rotation = cam.transform.rotation;
        }
    }

    /*Code for actually syncing health bar values to the actual health of the player,
     as well as setting up mod text to be displayed*/
    void InitializePlayerInterface()
    {
        UnitHealthDefence HP = GetComponent<UnitHealthDefence>();
        HP.health_bar_show = player_interface_show.GetComponentsInChildren<Slider>()[1].gameObject as GameObject;
        HP.hp_string = HP.health_bar_show.GetComponentInChildren<Text>();
        HP.hp_string.text = "<b>" + HP.HP + "</b>";
        HP.hp_bar = HP.health_bar_show.GetComponentInChildren<Slider>().GetComponent<RectTransform
            >();
        HP.maxWidth = HP.hp_bar.rect.width;
        mod_text = player_interface_show.GetComponentInChildren<Text>();
    }

    [Command]
    void CmdAddPlayer()
    {
        RpcAddPlayer();
    }

    [ClientRpc]
    void RpcAddPlayer()
    {
        players.RemoveNullM();
        players.Add(this);

    }

    [Command]
    void CmdRemovePlayer()
    {
        RpcRemovePlayer();
    }

    [ClientRpc]
    void RpcRemovePlayer()
    {
        players.Remove(this);
        if (name_display_show)
        {
            Destroy(name_display_show);
        }
        if (Shield)
        {
            Destroy(Shield);
        }
        foreach (Gun g in weapons)
        {
            if (g)
            {
                Destroy(g.gameObject);
            }
        }

    }

    /*For syncing the names of one game instance
     with other game instances.*/
    [Command]
    void CmdSyncNameDisplay(string name)
    {
        RpcSyncNameDisplay(name);
    }

    [ClientRpc]
    void RpcSyncNameDisplay(string name)
    {
        if (Client.netId != netId)
        {
            player_name = name;
        }
    }

    /*For getting the names of other players*/
    [Command]
    void CmdGetOtherNameDisplays()
    {
        RpcGetOtherNameDisplays();
    }

    [ClientRpc]
    void RpcGetOtherNameDisplays()
    {
        foreach (PlayerController p in players)
        {
            if (p && p.netId != Client.netId)
            {
                p.player_name = _player_name;
            }
        }
    }

    [Command]
    public void CmdApplyGunAbilities(GameObject obj, int index)
    {
        obj.GetComponent<Gun>().RpcApplyGunAbilities(index);
    }

    


    IEnumerator SetShield()
    {
        while (!GetComponentInChildren<ShieldHealthDefence>()) //Waits until the shield is created on server
        {
            yield return new WaitForEndOfFrame();
        }
        ShieldHealthDefence SP = GetComponentInChildren<ShieldHealthDefence>();
        Shield = SP.gameObject;
        SP.scale_factor = 2.5f;//Rescales obj
                               /*This changes the shield bar from a local shield bar object to the main shield bar
                                provided that the player who owns it is the one who is connected locally.
                                since player_interface_show would only be instantiated if Start() was executed
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
        CmdSetShieldIdOnServer(Shield);
    }

    [Command]
    void CmdSetShieldIdOnServer(GameObject obj)
    {
        Shield = obj;
        CustomNTC ntc = obj.GetComponent<CustomNTC>();
        ntc.parent = transform;
        ntc.local_position = new Vector3(1f, 0, 0);
    }

    /*The client's gun forward axis and rotation is
     * sent so the bullet's orientation is created 
     * with respect to the client's perspective.*/
    [Command]
    void CmdShoot(Vector3 forward,Vector3 pos,Quaternion rot)
    {
        try
        {
            if (main_gun.HasReloaded())
            {
                main_gun.Shoot(forward,pos,rot);
            }
        }
        catch (System.NullReferenceException e)
        {

        }
    }

    /*Since guns are chosen on client-side,this is needed to make stat changes on server*/
    [Command]
    public void CmdSetGun(GameObject g, int _level, uint _points, int _experience, int _next_level, int[] indeces)
    {
        Gun gun = g.GetComponent<Gun>();
        gun.RpcSetGun(_level, _points, _experience, _next_level, indeces);
        gun.client_user = this;
        CustomNTC ntc = gun.GetComponent<CustomNTC>();
        ntc.parent = transform;
        ntc.local_position = new Vector3(0, 0, .8f);
    }

    void Update()
    {
        if (!isLocalPlayer || !equip_action)
        {
            return;
        }
        int index = weapons.FindIndex(delegate (Gun g)
        {
            return (g
            && (Input.inputString.Contains(g.button)
            || (Input.GetMouseButton(0)
            && g.button == "LMB")));
        });
        if (index != -1 && !blocking)
        {
            CmdEquipGun(weapons[index].gameObject);
            StartCoroutine(weapons[index].item_image_show.GetComponentInChildren<ItemImage>().Cooldown(weapons[index].reload_time));
            CmdShoot(weapons[index].barrel_end.forward,weapons[index].barrel_end.position,weapons[index].barrel_end.rotation);
        }
        else if (Input.GetMouseButtonDown(1) && !blocking)
        {
            CmdStartShieldBlocking();
            blocking = true;
        }
        else if (Input.GetMouseButtonDown(1) && blocking)
        {
            blocking = false;
            CmdEndShieldBlocking();
        }


    }

    /*Velocity is manually synced instead of relying on the network transform 
      as the movement seems less laggy.*/
    IEnumerator SyncVelocity()
    {
        while (this)
        {
            yield return new WaitForFixedUpdate();
            if (!rb)
            {
                rb = GetComponent<Rigidbody>();
            }
            rb.velocity = move_dir;
        }
    }

    [Command]
    void CmdSendVelocity(Vector3 move)
    {
        move_dir = move;
    }

    [Command]
    void CmdSyncRotation(Quaternion rot)
    {
        RpcSyncRotation(rot);
    }

    [ClientRpc]
    void RpcSyncRotation(Quaternion rot)
    {
        if(Client.netId != netId)
        {
            transform.rotation = rot;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");
            Vector3 total_move = new Vector3(moveHorizontal, 0, -1 * moveVertical);
            rb.velocity = speed * total_move;
            //CmdSendVelocity(speed * total_move);
           // CmdSyncRotation(transform.rotation);
        }
    }

}