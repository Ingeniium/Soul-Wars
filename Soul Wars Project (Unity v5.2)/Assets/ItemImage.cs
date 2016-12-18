using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections;

public class ItemImage : MonoBehaviour {
    private Image this_pic;
    public GameObject Item;
    public Sprite preview;
    public Gun item_script;
    public Canvas item_description_canvas;
    private Canvas item_descritption_canvas_show;
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
	
	
    void OnMouseEnter()
    {
       item_descritption_canvas_show = Instantiate(item_description_canvas,transform.position + new Vector3(3.00f,0,3.00f), item_description_canvas.transform.rotation) as Canvas;
       item_descritption_canvas_show.GetComponentInChildren<Text>().text = item_script.ToString();
    }
    void OnMouseExit()
    {
        if (item_descritption_canvas_show)
        {
            Destroy(item_descritption_canvas_show.gameObject);
        }
    }
   

}
