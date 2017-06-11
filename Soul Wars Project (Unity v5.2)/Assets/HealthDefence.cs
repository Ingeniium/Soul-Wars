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
            RpcDisplayHP();
            if (value >= maxHP)
            {
                _HP = maxHP;
                regeneration = false;
                if (type == Type.Shield)
                {
                    if (shield_collider)
                    {
                        shield_collider.enabled = true;
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
                        StartCoroutine(SpawnManager.WaitForRespawn(this));
                        break;
                    case Type.Shield:
                        if (shield_collider)
                        {
                            shield_collider.enabled = false;
                        }
                        hp_string.text = "<b>" + HP + "</b>";
                        regeneration = true;
                        StartCoroutine(Regeneration());
                        break;

                    case Type.Spawn_Point:
                        if (gameObject.layer == 9)
                        {
                            gameObject.layer = 8;
                            RpcChangeLayer(gameObject.layer);
                            SpawnManager s = GetComponent<SpawnManager>();
                            RpcChangeColor(gameObject, Color.red);
                            RpcChangeColor(s.stand.gameObject, Color.red);
                            SpawnManager.EnemySpawnPoints.Add(s);
                            SpawnManager.AllySpawnPoints.Remove(s);
                        }
                        else
                        {
                            gameObject.layer = 9;
                            RpcChangeLayer(gameObject.layer);
                            SpawnManager s = GetComponent<SpawnManager>();
                            RpcChangeColor(s.gameObject, new Color32(52, 95, 221, 225));
                            RpcChangeColor(s.stand, new Color32(52, 95, 221, 225));
                            SpawnManager.AllySpawnPoints.Add(s);
                            SpawnManager.EnemySpawnPoints.Remove(s);
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
    [SyncVar] bool chilling;
    [SyncVar] bool burning;
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
    public float scale_factor;
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
    public int exp_rate = 1;
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
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * scale_factor, transform.localScale.z * scale_factor);
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
    }

    public IEnumerator DetermineChill(double chill)
    {
        double net_chill = chill - chill_resistance;
        if (net_chill > 0 && rand.NextDouble() < net_chill * 8 && type == HealthDefence.Type.Unit && !chilling)
        {
            chilling = true;
            float original = Controller.speed;
            Controller.speed = (100 - (float)net_chill * 400) * (.01f * Controller.speed);
            float time = (float)(net_chill * 150);
            float next_time = Time.time + time;
            RpcUpdateAilments("\r\n <color=cyan>Chill</color> ", time);
            while (Time.time < next_time || HP == 0)
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
        if (net_burn > 0 && rand.NextDouble() < net_burn * 4 && type == HealthDefence.Type.Unit && !burning)
        {
            burning = true;
            int num = (damage / 3) + (int)net_burn * 100;
            float time = (float)(net_burn * 200);
            float next_time = Time.time + time;
            RpcUpdateAilments("\r\n <color=orange>Burn</color> ", time);
            while (Time.time < next_time || HP == 0)
            {
                HP -= num;
                RpcDisplayHPChange(new Color(255, 150,0), num);
                yield return new WaitForSeconds(1);
            }
            burning = false;
        }
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

    [ClientRpc]
    public void RpcChangeLayer(int layer)
    {
        gameObject.layer = layer;
    }

    [ClientRpc]
    public void RpcChangeColor(GameObject g,Color color)
    {
        g.GetComponent<Renderer>().material.color = color;
    }

    [ClientRpc]
    void RpcDisplayHP()
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
        hp_string.text = "<b>" + HP + "</b>";
        float n = _HP * 1.0f;
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
