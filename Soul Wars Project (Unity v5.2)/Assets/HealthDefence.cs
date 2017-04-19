using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class HealthDefence : NetworkBehaviour {
    private Color Original_Color;
    public int maxHP;
    public int HP
    {
        get { return _HP; }
        set
        {
            _HP = value;
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
            float n = value * 1.0f;
            n /= maxHP * 1.0f;
            hp_bar.sizeDelta = new Vector2(maxWidth * n, hp_bar.rect.height);
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
                hp_string.text = "<b>" + HP + "</b>";
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
                        PlayerController.Client.CmdWaitForRespawn(gameObject);
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
                            SpawnManager s = GetComponent<SpawnManager>();
                            s.GetComponent<Renderer>().material.color = Color.red;
                            s.stand.GetComponent<Renderer>().material.color = Color.red;
                            s.stand.layer = 8;
                            SpawnManager.EnemySpawnPoints.Add(s);
                            SpawnManager.AllySpawnPoints.Remove(s);
                        }
                        else
                        {
                            gameObject.layer = 9;
                            SpawnManager s = GetComponent<SpawnManager>();
                            s.GetComponent<Renderer>().material.color = new Color32(52, 95, 221, 225);
                            s.stand.GetComponent<Renderer>().material.color = new Color32(52, 95, 221, 225);
                            s.stand.layer = 9;
                            SpawnManager.AllySpawnPoints.Add(s);
                            SpawnManager.EnemySpawnPoints.Remove(s);
                        }
                        break;
                }
            }
            

        }
    
    }
    [SyncVar] public int _HP;
    [SyncVar] public int defence;
    [SyncVar] public double crit_resistance = 0;
    [SyncVar] public float knockback_resistance = 2.5f;
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
            transform.localScale = new Vector3(transform.localScale.x,transform.localScale.y*scale_factor,transform.localScale.z*scale_factor);
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


    public void RestoreHP()
    {
        HP = maxHP;
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
