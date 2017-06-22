using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class HealthDefence : NetworkBehaviour {
    private Color Original_Color;
    [SyncVar] public int maxHP;
    public int HP
    {
        get { return _HP; }
        set
        {
            _HP = value;
            RpcDisplayHP(value);
            if (value >= maxHP)
            {
                _HP = maxHP;
                regeneration = false;
                if (type == Type.Shield)
                {
                    if (shield_collider)
                    {
                        NetworkMethods.Instance.RpcSetEnabled(gameObject, "Collider", true);
                        NetworkMethods.Instance.RpcSetColor(gameObject, Original_Color);
                    }
                    StopCoroutine(Regeneration());
                }
            }
            else if (value <= 0)
            {
                _HP = 0;
                RpcClearAilments();
                if (hp_string)
                {
                    hp_string.text = "<b>" + HP + "</b>";
                }
                if ((Controller is PlayerController) != true)
                {
                    Destroy(health_bar_show.gameObject);
                }
                switch (type)
                {
                    case Type.Unit:
                        if (has_drops)
                        {
                            GetComponentInChildren<AIController>().gun.DropItem(ref gun_drop_chance);
                        }
                        if (Controller is PlayerController)
                        {
                            PlayersAlive.Instance.Players.Remove(netId.Value);
                        }
                        StartCoroutine(SpawnManager.WaitForRespawn(this));
                        break;
                    case Type.Shield:
                        if (shield_collider)
                        {
                            NetworkMethods.Instance.RpcSetEnabled(gameObject, "Collider", false);
                        }
                        hp_string.text = "<b>" + HP + "</b>";
                        regeneration = true;
                        NetworkMethods.Instance.RpcSetColor(gameObject, Color.red);
                        StartCoroutine(Regeneration());
                        break;

                    case Type.Spawn_Point:
                        if (gameObject.layer == 9)
                        {
                            NetworkMethods.Instance.RpcSetLayer(gameObject, 8);
                            SpawnManager s = GetComponent<SpawnManager>();
                            NetworkMethods.Instance.RpcSetColor(gameObject, Color.red);
                            NetworkMethods.Instance.RpcSetColor(s.stand, Color.red);
                            s.RpcMakeEnemy();
                        }
                        else
                        {
                            NetworkMethods.Instance.RpcSetLayer(gameObject,9);
                            SpawnManager s = GetComponent<SpawnManager>();
                            NetworkMethods.Instance.RpcSetColor(gameObject, new Color32(52, 95, 221, 225));
                            NetworkMethods.Instance.RpcSetColor(s.stand, new Color32(52, 95, 221, 225));
                            s.RpcMakeAlly();
                        }
                        _HP = maxHP;
                        break;
                }
            }
            

        }
    
    }
    [SyncVar] public int _HP;
    [SyncVar] public int defence;
    [SyncVar] public double crit_resistance = 0;
    [SyncVar] public float knockback_resistance = 2.5f;
    [SyncVar] public double chill_resistance;
    [SyncVar] public double burn_resistance;
    [SyncVar] public double mezmerize_resistance;
    [SyncVar] bool chilling;
    [SyncVar] bool burning;
    [SyncVar] bool mezmerized;
    [SyncVar] bool stunned;
    public float standing_power
    {
        get { return _standing_power; }
        set
        {
            _standing_power = value;
            if (_standing_power <= 0)
            {
                _standing_power = max_standing_power;
                StartCoroutine(SpawnManager.Blink(gameObject, .75f));
            }
        }
    }
    
    private float _standing_power;
    public float max_standing_power = 10;
    public Rigidbody rb;
    public GenericController Controller;
    private float _scale_factor;
    public float scale_factor
    {
        get { return _scale_factor; }
        set
        {
            _scale_factor = value;
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * scale_factor, transform.localScale.z * scale_factor);
        }
    }
    public float sec_till_regen;
    public Collider shield_collider;
    public bool regeneration = false;
    public GameObject health_bar;
    public GameObject health_bar_show;
    public Text hp_string;
    public RectTransform hp_bar;
    public float maxWidth;
    public bool has_drops;
    public bool has_exp = true;
    public float exp_rate = .5f;
    public double gun_drop_chance;
    public GameObject ailment_display;
    private GameObject ailment_display_show;
    private Text ailment_text;
    private List<string> ailments = new List<string>();
    public GameObject health_change_canvas;
    public GameObject health_change_show;
    static System.Random rand = new System.Random();
    public Type type = Type.Unit;
    public enum Type
    {
        Unit = 0,
        Shield = 1,
        Spawn_Point = 2
    }
	// Use this for initialization
	void Awake () 
    {
        if (type == Type.Shield)
        {
            shield_collider = GetComponent<BoxCollider>();
        }
        else
        {
            ailment_display_show = Instantiate(ailment_display) as GameObject;
            ailment_display_show.GetComponent<HPbar>().Object = gameObject;
            ailment_text = ailment_display_show.GetComponentInChildren<Text>();
        }
        Original_Color = gameObject.GetComponent<Renderer>().material.color;
        _HP = maxHP;
        
	}

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        _standing_power = max_standing_power;
        if (hp_string)
        {
            hp_string.text = "<b>" + HP + "</b>";
        }
        if (hp_bar)
        {
            maxWidth = hp_bar.rect.width;
        }
        if (Controller && Controller is PlayerController && isServer)
        {
            PlayersAlive.Instance.Players.Add(Controller.netId.Value);
        }
    }

    public IEnumerator DetermineChill(double chill)
    {
        double net_chill = chill - chill_resistance;
        if (net_chill > 0 && type == HealthDefence.Type.Unit && !chilling && rand.NextDouble() < net_chill * 8)
        {
            chilling = true;
            float original = Controller.speed;
            Controller.speed = (100 - (float)net_chill * 800) * (.01f * Controller.speed);
            float time = (float)(net_chill * 200);
            float next_time = Time.time + time;
            RpcUpdateAilments("\r\n <color=cyan>Chill</color> ", time);
            while (Time.time < next_time && HP != 0)
            {
                yield return new WaitForEndOfFrame();
            }
            Controller.speed = original;
            chilling = false;
        }
    }

    public IEnumerator DetermineBurn(double burn,int damage)
    {
        double net_burn = burn - burn_resistance;
        if (net_burn > 0 && !burning && type == HealthDefence.Type.Unit && rand.NextDouble() < net_burn * 4)
        {
            burning = true;
            int num = (damage / 3) + (int)net_burn * 100;
            float time = (float)(net_burn * 200);
            float next_time = Time.time + time;
            RpcUpdateAilments("\r\n <color=orange>Burn</color> ", time);
            while (Time.time < next_time && HP != 0)
            {
                HP -= num;
                RpcDisplayHPChange(new Color(255, 150,0), num);
                yield return new WaitForSeconds(1);
            }
            burning = false;
        }
    }

    public IEnumerator DetermineMezmerize(double mez)
    {
        double net_mez = mez - mezmerize_resistance;
        if (net_mez > 0 && !mezmerized && type == HealthDefence.Type.Unit && rand.NextDouble() < net_mez * 6)
        {
            mezmerized = true;
            Controller.gun.mez_threshold = (int)(net_mez * 100) / 2;
            float time = (float)(net_mez * 150);
            float next_time = Time.time + time;
            RpcUpdateAilments("\r\n <color=purple>Mezmerize</color> ", time);
            while (Time.time < next_time && HP != 0)
            {
                yield return new WaitForEndOfFrame();
            }
            Controller.gun.mez_threshold = 0;
            mezmerized = false;
        }
    }

    public void DetermineStun(float time)
    {
        if (Controller && !stunned)
        {
            RpcUpdateAilments("\r\n <color=yellow>Stun</color>", time);
            if (Controller is PlayerController)
            {
                RpcStun(time);
            }
            else
            {
                StartCoroutine(Stun(time));
            }
        }
    }

    [ClientRpc]
    void RpcStun(float time)
    {
        StartCoroutine(Stun(time));
    }

    [ClientRpc]
    public void RpcUpdateAilments(string s, float time)
    {
        StartCoroutine(UpdateAilments(s, time));
    }

    [ClientRpc]
    public void RpcClearAilments()
    {
        if (ailment_display_show)
        {
            ailments.Clear();
            ailment_text.text = "";
        }
    }

    IEnumerator UpdateAilments(string s, float time)
    {
        ailments.Add(s);
        ailment_text.text = "";
        foreach (string t in ailments)
        {
            ailment_text.text += t;
        }
        yield return new WaitForSeconds(time);
        ailments.Remove(s);
        ailment_text.text = "";
        foreach (string t in ailments)
        {
            ailment_text.text += t;
        }
    }


    IEnumerator Stun(float time)
    {
        Controller.enabled = false;
        stunned = true;
        yield return new WaitForSeconds(time);
        stunned = false;
        if(HP > 0)
        {
            Controller.enabled = true;
        }
    }


    [ClientRpc]
    void RpcDisplayHP(int val)//Takes an int,for synvar _HP isnt synced in time with the Rpc call
    {
        if (health_bar_show == null)
        {
            health_bar_show = Instantiate(health_bar, transform.position + new Vector3(0, 1, 1), health_bar.transform.rotation) as GameObject;
            health_bar_show.GetComponent<HPbar>().Object = gameObject;
            health_bar_show.GetComponent<HPbar>().offset = health_bar_show.transform.position - gameObject.transform.position;
            hp_string = health_bar_show.GetComponentInChildren<Text>();
            Slider[] r = health_bar_show.GetComponentsInChildren<Slider>();
            maxWidth = r[0].GetComponent<RectTransform>().rect.width;
            hp_bar = r[1].GetComponent<RectTransform>();
            Destroy(health_bar_show.gameObject, 5f);
        }
        hp_string.text = "<b>" + val + "</b>";
        float n = val * 1.0f;
        n /= maxHP * 1.0f;
        hp_bar.sizeDelta = new Vector2(maxWidth * n, hp_bar.rect.height);
    }

    [ClientRpc]
    public void RpcDisplayHPChange(Color color,int num)
    {
        health_change_show = Instantiate(health_change_canvas, gameObject.transform.position + new Vector3(0, 0, 1), Quaternion.Euler(90, 0, 0)) as GameObject;
        if (type != HealthDefence.Type.Shield)
        {
            health_change_show.GetComponentInChildren<Text>().text = "-" + num;
            health_change_show.GetComponentInChildren<Text>().color = color;
        }
        else
        {
            health_change_show.GetComponentInChildren<Text>().text = "*BLOCKED*";
            health_change_show.GetComponentInChildren<Text>().color = Color.black;
        }
        Destroy(health_change_show, 1f);
    }
    
    IEnumerator Regeneration()
    {
        while(regeneration == true)
        {
            yield return new WaitForSeconds(sec_till_regen);
            HP++;
        }
    }
}
