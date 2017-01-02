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
    }

    void OnMouseExit()
    {
        hover = false;
    }

    void Update()
    {
        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && hover == false)
        {
            image.option_showing = false;
            Destroy(gameObject);
        }        
    }
}
