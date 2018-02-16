using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ShieldHealthDefence : HealthDefence
{
    public int regen_amount;
    [SyncVar] public bool _regen;
    public bool regen
    {
        get { return _regen; }
        private set
        {
            _regen = value;
            if (value)
            {
                StartCoroutine(Regeneration());
            }
        }
    }
    private float _scale_factor;
    public float scale_factor
    {
        get { return _scale_factor; }
        set
        {
            _scale_factor = value;
           // transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * scale_factor, transform.localScale.z * scale_factor);
        }
    }
    private Collider shield_collider;
    private Color original_color;

    protected override void Start()
    {
        base.Start();
        shield_collider = GetComponent<Collider>();
        original_color = GetComponent<Renderer>().material.color;
    }

    protected override void OnDeath()
    {
        if(shield_collider)
        {
            NetworkMethods.Instance.RpcSetEnabled(gameObject, "Collider", false);
            NetworkMethods.Instance.RpcSetColor(gameObject, Color.red);
            regen = true;
        }
    }

    protected override void OnOverMaxHP()
    {
        if (shield_collider)
        {
            NetworkMethods.Instance.RpcSetEnabled(gameObject, "Collider", true);
            NetworkMethods.Instance.RpcSetColor(gameObject, original_color);
        }
    }

    protected override void DisplayHPChange(Color color, int num)
    {
        if(health_bar_show)
        {
            Text t = health_bar_show.GetComponentInChildren<Text>();
            t.text = "*BLOCKED*";
            t.color = Color.black;
        }
    }

    IEnumerator Regeneration()
    {
        while (HP < maxHP)
        {
            yield return new WaitForSeconds(1);
            HP += regen_amount;
        }
    }
}

