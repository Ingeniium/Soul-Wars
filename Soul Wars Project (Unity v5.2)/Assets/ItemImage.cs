using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections;
using System;

public class ItemImage : MonoBehaviour {
    private Image this_pic;
    public GameObject Item_
    {
        get { return _Item; }
        set
        {
            _Item = value;
            while (preview == null)
            {
                this_pic = GetComponent<Image>();
                Texture2D tex = AssetPreview.GetAssetPreview(value) as Texture2D;
                try
                {
                    preview = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                }
                catch (NullReferenceException exception) { preview = null; }
                this_pic.sprite = preview;
            }
        }
    }
    private GameObject _Item;
    private Sprite preview;
    public Item item_script
    {
        get { return _item_script; }
        set
        {
            if (value is Gun)
            {
                _item_script = value as Gun;
            }
            else
            {
                _item_script = value;
            }
        }                       
    }
    public Item _item_script;
    public Canvas item_description_canvas;
    private Canvas item_descritption_canvas_show;
    public bool option_showing = false;
	
	
    void OnMouseEnter()
    {
       item_descritption_canvas_show = Instantiate(item_description_canvas,transform.position + new Vector3(1.5f,0,1.75f), item_description_canvas.transform.rotation) as Canvas;
       item_descritption_canvas_show.transform.parent = transform;
       item_descritption_canvas_show.GetComponentInChildren<Text>().text = item_script.ToString();
    }
    //Mouse events can't evaluate more than on button,therefore use two events for detection.
    void OnMouseDown()
    {
        if (Input.GetMouseButton(0))
        {
            if (item_script.in_inventory)
            {
                item_script.PrepareItemForUse();
            }
        }
    }
    void OnMouseOver()
    {
        Item.Player.equip_action = false;
         if (Input.GetMouseButton(1) && option_showing == false)
          {
                item_script.Options();
                option_showing = true;
          }
              
    }

    void OnMouseExit()
    {
        Item.Player.equip_action = true;
        if (item_descritption_canvas_show)
        {
            Destroy(item_descritption_canvas_show.gameObject);
        }
       
    }
   

}
