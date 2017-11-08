using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class HealthDefence : NetworkBehaviour {
    private Color Original_Color;
    [SyncVar] public int maxHP;//Maximum HP a player can have
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
                        Controller.StartCoroutine(SpawnManager.WaitForRespawn(this));
                        break;
                    case Type.Shield:
                        if (shield_collider)
                        {
                            NetworkMethods.Instance.RpcSetEnabled(gameObject, "Collider", false);
                        }
                        if (hp_string)
                        {
                            hp_string.text = "<b>" + HP + "</b>";
                        }
                        regeneration = true;
                        NetworkMethods.Instance.RpcSetColor(gameObject, Color.red);
                        StartCoroutine(Regeneration());
                        break;

                    case Type.Spawn_Point:
                        SpawnManager s = GetComponent<SpawnManager>();
                        damage_counter_list.Sort(delegate (ValueGroup<int,int> lhs, ValueGroup<int,int> rhs)
                        {
                            if(lhs.value > rhs.value)
                            {
                                return -1;
                            }
                            else
                            {
                                return 1;
                            }
                        });
                        int new_layer = damage_counter_list[0].index;
                        s.RpcChangeTeam(new_layer);
                        damage_counter_list.Clear();
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
    [SyncVar] public double sunder_resistance;
    public bool chilling
    {
        get { return _chilling; }
        private set
        {
            _chilling = value;
        }
    }
    public bool burning
    {
        get { return _burning; }
        private set
        {
            _burning = value;
        }
    }
    public bool mezmerized
    {
        get { return _mezmerized; }
        private set
        {
            _mezmerized = value;
        }
    }
    public bool stunned
    {
        get { return _stunned; }
        private set
        {
            _stunned = value;
        }
    }
    public bool sundered
    {
        get { return _sundered; }
        private set
        {
            _sundered = value;
        }
    }
    bool _chilling;
    bool _burning;
    bool _mezmerized;
    bool _stunned;
    bool _sundered;
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
    private List<ValueGroup<int, int>> damage_counter_list = new List<ValueGroup<int, int>>();
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
        if (Controller && Controller is PlayerController && isServer)
        {
            PlayersAlive.Instance.Players.Add(Controller.netId.Value);
        }
    }

    public void UpdateDamageCounter(int damage,int layer)
    {
        if(layer == LayerMask.NameToLayer("Invincible"))
        {
            return;
        }
        int index = damage_counter_list.FindIndex(delegate (ValueGroup<int,int> v)
        {
            return (v.index == layer);
        });
        if(index == -1)
        {
            damage_counter_list.Add(new ValueGroup<int, int>(layer, damage));
        }
        else
        {
            damage_counter_list[index] = new ValueGroup<int, int>(layer,
                damage_counter_list[index].value + damage);
        }
    }

    public IEnumerator DetermineChill(double chill)
    {
        double net_chill = chill - chill_resistance;
        if (net_chill > 0 && type == Type.Unit && !chilling && rand.NextDouble() < net_chill * 8)
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
        if (net_burn > 0 && !burning && rand.NextDouble() < net_burn * 6)
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
        if (net_mez > 0 && !mezmerized && type == Type.Unit && rand.NextDouble() < net_mez * 6)
        {
            mezmerized = true;
            foreach (Gun gun in Controller.weapons)
            {
                if (gun)
                {
                    gun.mez_threshold = (int)(net_mez * 100) / 2;
                }
            }
            float time = (float)(net_mez * 150);
            float next_time = Time.time + time;
            RpcUpdateAilments("\r\n <color=purple>Mezmerize</color> ", time);
            while (Time.time < next_time && HP != 0)
            {
                yield return new WaitForEndOfFrame();
            }
            foreach(Gun gun in Controller.weapons)
            {
                if (gun)
                {
                    gun.mez_threshold = 0;
                }
            }
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

    public IEnumerator DetermineSunder(double sunder, int damage)
    {
        double net_sunder = sunder - sunder_resistance;
        if (net_sunder > 0 && !sundered && rand.NextDouble() < net_sunder * 4)
        {
            sundered = true;
            int num = ((int)net_sunder * 100 + damage) / 5;
            defence -= num;
            float time = (float)(net_sunder * 200);
            float next_time = Time.time + time;
            RpcUpdateAilments("\r\n <color=brown>Sunder</color>", time);
            while (Time.time < next_time && HP != 0)
            {
                yield return new WaitForEndOfFrame();
            }
            defence += num;
            sundered = false;
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
            StopAllCoroutines();
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
