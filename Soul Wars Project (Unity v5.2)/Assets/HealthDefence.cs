using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthDefence : MonoBehaviour {
    private Color Original_Color;
    public int maxHP;
    private int _HP;
    public int HP
    {
        get { return _HP; }
        set
        {
            _HP = value;
            if (health_bar_show == null)
            {
                health_bar_show = Instantiate(health_bar, transform.position + new Vector3(0, 1, 1), health_bar.transform.rotation) as Canvas;
                health_bar_show.GetComponent<HPbar>().Object = gameObject;
                health_bar_show.GetComponent<HPbar>().offset = health_bar_show.transform.position - gameObject.transform.position;
                hp_string = health_bar_show.GetComponentInChildren<Text>();
                Slider[] r = health_bar_show.GetComponentsInChildren<Slider>();
                maxWidth = r[0].GetComponent<RectTransform>().rect.width;
                hp_bar = r[1].GetComponent<RectTransform>();
                Destroy(health_bar_show.gameObject, 5f);
            }
            hp_string.text = "<b>" + _HP + "</b>";
            float n = _HP * 1.0f;
            n /= maxHP * 1.0f;
            hp_bar.sizeDelta = new Vector2(maxWidth * n,hp_bar.rect.height);
            if (value >= maxHP)
            {
                _HP = maxHP;
                regeneration = false;
                if (shield)
                {
                    shield_collider.enabled = true;
                    gameObject.GetComponent<Renderer>().material.color = Original_Color;
                    StopCoroutine(Regeneration());
                }
            }
            else if (value <= 0)//Not on collision enter to account for DoT
            {
                if (shield == false)
                {
                    Destroy(health_bar_show.gameObject);
                    Destroy(gameObject);
                }
                else
                {
                    shield_collider.enabled = false;
                    _HP = 0;
                    hp_string.text = "<b>" + _HP + "</b>";
                    regeneration = true;
                    gameObject.GetComponent<Renderer>().material.color = Color.red;
                    StartCoroutine(Regeneration());
                }
            }
        }
    }
    public int defence;
    public bool shield;
    public float scale_factor;
    public float sec_till_regen;
    public Collider shield_collider;
    public bool regeneration = false;
    public Canvas health_bar;
    public Canvas health_bar_show;
    public Text hp_string;
    public RectTransform hp_bar;
    public float maxWidth;
	// Use this for initialization
	void Awake () 
    {
        if (shield == true)
        {
            shield_collider = GetComponent<BoxCollider>();
            transform.localScale = new Vector3(transform.localScale.x,transform.localScale.y*scale_factor,transform.localScale.z*scale_factor);
        }
        _HP = maxHP;
        if (hp_string)
        {
            hp_string.text = "<b>" + HP + "</b>";
        }
        if (hp_bar)
        {
            maxWidth = hp_bar.rect.width;
        }
        Original_Color = gameObject.GetComponent<Renderer>().material.color;
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
