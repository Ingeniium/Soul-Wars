using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public abstract class HealthDefence : NetworkBehaviour
{
    [SyncVar] public int maxHP;//Maximum HP a player can have
    public int HP
    {
        get { return _HP; }
        set
        {
            if(!isServer)
            {
                return;
            }
            RpcDisplayHP(value);
            if (value >= maxHP)
            {
                _HP = maxHP;
                OnOverMaxHP();
            }
            else if (value <= 0)
            {
                _HP = 0;
                RpcClearAilments();
                RpcClearHPBar();
                if (hp_string)
                {
                    hp_string.text = "<b>" + HP + "</b>";
                }
                OnDeath();
            }
            else
            {
                _HP = value;
                OnNormalHPChange();
            }
        }
    }
    [SyncVar] public int _HP;
    [SyncVar] public int defence;
    [SyncVar] public double crit_resistance = 0;
    [SyncVar] public double sunder_resistance;
    [SyncVar] public double burn_resistance;
    public bool burning
    {
        get { return _burning; }
        private set
        {
            _burning = value;
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
    bool _burning;
    bool _sundered;
    public GameObject health_bar;
    public GameObject health_bar_show;
    public Text hp_string;
    public RectTransform hp_bar;
    public float maxWidth;
    public GameObject ailment_display;
    private GameObject ailment_display_show;
    private Text ailment_text;
    private List<string> ailments = new List<string>();
    public GameObject health_change_canvas;
    public GameObject health_change_show;
    protected static System.Random rand = new System.Random();
    private bool destroy_bar_on_death = true;

    public virtual void DetermineStatusEffects(double[] powers, int damage)
    {
        StartCoroutine(DetermineBurn(powers[0], damage));
        StartCoroutine(DetermineSunder(powers[1], damage));
    }

    protected abstract void OnDeath();
    protected virtual void OnOverMaxHP() { }
    protected virtual void OnNormalHPChange() { }

    virtual protected void Start()
    {
        if (health_bar_show)
        {
            destroy_bar_on_death = false;
        }
        ailment_display_show = Instantiate(ailment_display) as GameObject;
        ailment_display_show.GetComponent<HPbar>().Object = gameObject;
        ailment_text = ailment_display_show.GetComponentInChildren<Text>();
        HP = maxHP;
    }

    public IEnumerator DetermineBurn(double burn, int damage)
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
                RpcDisplayHPChange(new Color(255, 150, 0), num);
                yield return new WaitForSeconds(1);
            }
            burning = false;
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
        if (!ailment_text)
        {
            yield break;
        }
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
    protected void RpcClearHPBar()
    {
        if (health_bar_show && destroy_bar_on_death)
        {
            Destroy(health_bar_show.gameObject);
        }
    }


    [ClientRpc]
    void RpcDisplayHP(int val)//Takes an int,for synvar _HP isnt synced in time with the Rpc call
    {
        if (health_bar_show == null)
        {
            health_bar_show = Instantiate(health_bar, transform.position, health_bar.transform.rotation) as GameObject;
            health_bar_show.GetComponent<HPbar>().Object = gameObject;
            health_bar_show.GetComponent<HPbar>().offset = new Vector3(0, 0, 1);
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
    public void RpcDisplayHPChange(Color color, int num)
    {
        if(health_change_show)
        {
            Destroy(health_change_show);//In case of times when there's multiple hp changes within a second
        }
        health_change_show = Instantiate(health_change_canvas, transform.position, Quaternion.Euler(90, 0, 0)) as GameObject;
        DisplayHPChange(color, num);
        Destroy(health_change_show, 1f);
    }

    protected virtual void DisplayHPChange(Color color, int num)
    {
        if (health_change_show)
        {
            health_change_show.GetComponentInChildren<Text>().text = "-" + num;
            health_change_show.GetComponentInChildren<Text>().color = color;
            HPbar bar = health_change_show.AddComponent<HPbar>();
            bar.Object = gameObject;
            const float OFFSET = 1.5f;
            bar.offset = new Vector3(OFFSET,0,OFFSET);

        }
    }
}
