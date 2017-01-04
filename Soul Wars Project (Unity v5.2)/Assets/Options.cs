using UnityEngine;
using System.Collections;

public class Options : MonoBehaviour {
    private ItemImage image;
    private HPbar bar;
    private bool hover = false;
	// Use this for initialization
	void Start ()
    {
        bar = GetComponent<HPbar>();
        image = bar.Object.GetComponentInChildren<ItemImage>();
	}

    void OnMouseOver()
    {
        hover = true;
        Item.Player.equip_action = false;
    }

    void OnMouseExit()
    {
        hover = false;
        Item.Player.equip_action = true;
    }

    void Update()
    {
        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && hover == false)
        {
            image.option_showing = false;
            Item.Player.equip_action = true;
            Destroy(gameObject);
        }        
    }
}
