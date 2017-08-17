
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class ItemImage : MonoBehaviour {
    private Image this_pic;
    private Sprite preview;
    public Item item_script
    {
        get { return _item_script; }
        set
        {
            _item_script = value;
            this_pic = GetComponent<Image>();
            Texture2D tex = Resources.Load(_item_script.GetImagePreviewString(), typeof(Texture2D)) as Texture2D;
            preview = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
            this_pic.sprite = preview;
        }                       
    }
    public Item _item_script;
    public Canvas item_description_canvas;
    private Canvas item_descritption_canvas_show;
    public bool option_showing = false;
    protected Canvas item_options_show;//Canvas for showing item options upon right click
    public Canvas item_options;
    public Canvas settings_canvas;
    private Canvas settings_canvas_show;
    private Text settings_text;
    private string[] settings = new string[5];
    public bool on_cooldown = false;

    void Start()
    {
        settings_canvas_show = Instantiate(settings_canvas, transform.position, settings_canvas.transform.rotation, transform);
        settings_text = settings_canvas_show.GetComponentInChildren<Text>();
    }

    public void AddSetting(string s, int index)
    {
        
        settings[index] = s;
        settings_text.text = "";
        foreach (string t in settings)
        {
            settings_text.text += t;
        }
    }

    public void RemoveSetting(string s)
    {
        int i = Array.FindIndex(settings, delegate (string str)
         {
             return (str == s);
         });
        if(i != -1)
        {
            settings[i] = "";
            settings_text.text = "";
            foreach (string t in settings)
            {
                settings_text.text += t;
            }
        }
    }

    public IEnumerator Cooldown(float time)
    {
        on_cooldown = true;
        int seconds = (int)time;
        settings[2] = "<color=yellow>" + seconds.ToString() + "</color> ";
        settings_text.text = "";
        foreach (string t in settings)
        {
            settings_text.text += t;
        }
        yield return new WaitForSeconds(time - (float)seconds);
        while (seconds > 0)
        {
            yield return new WaitForSeconds(1);
            seconds--;
            settings[2] = "<color=yellow>" + seconds.ToString() + "</color> ";
            settings_text.text = "";
            foreach (string t in settings)
            {
                settings_text.text += t;
            }
        }
        settings[2] = "";
        settings_text.text = "";
        foreach (string t in settings)
        {
            settings_text.text += t;
        }
        
        on_cooldown = false;
    }

    public void OnPointerEnter()
    {
         item_descritption_canvas_show = Instantiate(item_description_canvas, transform.position + new Vector3(1.5f, 0, 1.75f), item_description_canvas.transform.rotation) as Canvas;
         item_descritption_canvas_show.transform.parent = transform;
         item_descritption_canvas_show.GetComponentInChildren<Text>().text = item_script.ToString();     
    }
    
    public void OnPointerDown()
    {
          PlayerController.Client.equip_action = false;
          if (option_showing == false && Input.GetMouseButton(1))
          {
              Options();
              option_showing = true;
          }
        else if(Input.GetMouseButtonDown(0) && item_script.in_inventory == true)
        {
            item_script.PrepareItemForUse();
        }
       
    }

    public void OnPointerExit()
    {
        PlayerController.Client.equip_action = true;
        if (item_descritption_canvas_show)
        {
            Destroy(item_descritption_canvas_show.gameObject);
        }
       
    }

    void Options()
    {
        List<String> option_string = item_script.GetOptionsStrings();
        GameObject option_canvas = Resources.Load("Options" + option_string.Count.ToString()) as GameObject;
        GameObject options_canvas_show = Instantiate(
            option_canvas,
            transform.position,
            option_canvas.transform.rotation
            );
        Button[] buttons = options_canvas_show.GetComponentsInChildren<Button>();
        foreach(HPbar h in  options_canvas_show.GetComponentsInChildren<HPbar>())
        {
            h.Object = gameObject;
            h.offset = new Vector3(2,0,2);
        }
        List<Text> texts = new List<Text>();
        Item.Options option = item_script.GetOptionsFuncs();

        foreach(Button b in buttons)
        {
            texts.Add(b.GetComponentInChildren<Text>());
        }
        int i = 0;
        foreach(Item.Options o in option.GetInvocationList())
        {
            buttons[i].onClick.AddListener(delegate { o(); });
            texts.Add(buttons[i].GetComponentInChildren<Text>());
            texts[i].text = option_string[i];
            i++;
        }
    }
   

}
