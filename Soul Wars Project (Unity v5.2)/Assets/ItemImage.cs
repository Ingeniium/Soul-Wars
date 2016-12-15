using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections;

public class ItemImage : MonoBehaviour {
    private Image this_pic;
    public GameObject Item;
    public Sprite preview;
    public GameObject item_script;
    public Canvas item_description_canvas;
    private Canvas item_descritption_canvas_show;
    public enum DescType
    {
        GUN_DESC = 0
    };
    public DescType type;         
	// Use this for initialization
	void Start ()
    {
        while (preview == null)
        {
            this_pic = GetComponent<Image>();
            Texture2D tex = AssetPreview.GetAssetPreview(Item) as Texture2D;
            preview = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
            this_pic.sprite = preview;
        }
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnMouseEnter()
    {
       item_descritption_canvas_show = Instantiate(item_description_canvas,transform.position + new Vector3(1.25f,0,1.25f), item_description_canvas.transform.rotation) as Canvas;
       item_descritption_canvas_show.GetComponentInChildren<Text>().text += Description_Add(ref item_script);
    }
    void OnMouseExit()
    {
        if (item_descritption_canvas_show)
        {
            Destroy(item_descritption_canvas_show.gameObject);
        }
    }
    string Description_Add(ref GameObject p_script)
    {
        GameObject script = p_script;
        string desc_addon = null;
        switch (type)
        {
            case DescType.GUN_DESC:
                desc_addon = "\n Homing : " + p_script.GetComponent<Gun>().home_radius;
                break;
        }
        return desc_addon;
    }
}
