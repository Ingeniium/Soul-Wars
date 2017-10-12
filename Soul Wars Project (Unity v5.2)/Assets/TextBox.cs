
using UnityEngine;
using UnityEngine.UI;

/*Class responsible for making Desc boxes*/
public class TextBox : MonoBehaviour
{
    public static TextBox Instance;
    public Canvas item_desc_canvas;
    public Canvas exitable_desc_canvas;

    void Awake()
    {
        Instance = this;
    }

    /*Creates a desc box without an exit button.*/
    public Canvas CreateDescBox(Transform parent,Vector3 position,string text,bool world_space = true)
    {
        Canvas item_desc_canvas_show = Instantiate(item_desc_canvas, position, item_desc_canvas.transform.rotation, parent) as Canvas;
        item_desc_canvas_show.GetComponentInChildren<Text>().text = text;
        if(!world_space)
        {
            MakeProperOverlay(item_desc_canvas_show);
        }
        return item_desc_canvas_show;
    }

    /*Creates a desc box with an exit button*/
    public Canvas CreateExitDescBox(Transform parent, Vector3 position, string text, bool world_space = true)
    {
        Canvas exitable_desc_canvas_show = Instantiate(exitable_desc_canvas, position, exitable_desc_canvas.transform.rotation, parent) as Canvas;
        exitable_desc_canvas_show.GetComponentInChildren<Text>().text = text;
        Button exit = exitable_desc_canvas_show.GetComponentInChildren<Button>();
        exit.onClick.AddListener(delegate ()
        {
            Destroy(exitable_desc_canvas_show.gameObject);
        });
        if (!world_space)
        {
            MakeProperOverlay(exitable_desc_canvas_show);
        }
        return exitable_desc_canvas_show;
    }

    /*Makes the canvas an overlay and changes values to make the canvas
     look almost the same as if it were in world space.*/
    void MakeProperOverlay(Canvas canvas_show)
    {
        float panel_dimensions = .3f;
        float word_dimensions = .5f;
        Vector3 position = canvas_show.transform.position;//Unfortunately, Overlay stations the canvas in 0,0,0,so we need to adjust the children positions accordingly
        canvas_show.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas_show.worldCamera = PlayerController.Client.cam_show;

        RectTransform[] rects = canvas_show.GetComponentsInChildren<RectTransform>();

        RectTransform panel = rects[1];
        panel.transform.localScale = new Vector3(panel_dimensions, panel_dimensions, panel_dimensions);
        panel.transform.localPosition = new Vector3(position.x, position.y, position.z);

        RectTransform description = rects[2];
        description.transform.localScale = new Vector3(word_dimensions, word_dimensions, word_dimensions);
        description.transform.localPosition = new Vector3(position.x - 80, position.y + 80, 0);

        RectTransform exit_button = rects[3];
        exit_button.localPosition = new Vector3(position.x + 78,position.y - 71, 0);
        exit_button.sizeDelta = new Vector3(30, 30);
        Text exit_text = exit_button.GetComponentInChildren<Text>();
        exit_text.transform.localPosition = new Vector3(-70.6f, 10.15f, 0);
        exit_text.transform.localScale = new Vector3(3, 2, 1);
    }
}
