using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections;

public class ItemImage : MonoBehaviour {
    private Image this_pic;
    public GameObject Item
    {
        get { return _Item; }
        set
        {
            _Item = value;
            while (preview == null)
            {
                this_pic = GetComponent<Image>();
                Texture2D tex = AssetPreview.GetAssetPreview(value) as Texture2D;
                preview = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
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
                print("Gun");
            }
            else
            {
                _item_script = value;
            }
        }                       
    }
    private Item _item_script;
    public Canvas item_description_canvas;
    private Canvas item_descritption_canvas_show;
    private bool in_inventory = true;
	
	
    void OnMouseEnter()
    {
       item_descritption_canvas_show = Instantiate(item_description_canvas,transform.position + new Vector3(1.5f,0,1.75f), item_description_canvas.transform.rotation) as Canvas;
       item_descritption_canvas_show.transform.parent = transform;
       item_descritption_canvas_show.GetComponentInChildren<Text>().text = item_script.ToString();
    }

    void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0) && in_inventory)
        {
            GameObject weapons_bar = GameObject.FindGameObjectWithTag("Weapons");
            transform.parent.parent = weapons_bar.transform;
            transform.parent.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(-29f,-250) ;
            PlayerController Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
            GameObject prev_Gun = Player.Gun;
            Color color = Player.gun.color;
            Player.Gun = Instantiate(Item,prev_Gun.transform.position, prev_Gun.transform.rotation) as GameObject;
            Player.Gun.transform.parent = Player.gameObject.transform;
            Destroy(prev_Gun);
            Player.gun = item_script as Gun;
            Player.gun.color = color;
            Player.gun.layer = 13;
            Player.gun.home_layer = 10;
            Player.gun.barrel_end = Player.Gun.GetComponentInChildren<Transform>();
            in_inventory = false;
        }
    }

    void OnMouseExit()
    {
        if (item_descritption_canvas_show)
        {
            Destroy(item_descritption_canvas_show.gameObject);
        }
    }
   

}
