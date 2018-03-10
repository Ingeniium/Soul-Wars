
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
    private Canvas item_descritption_canvas_show;
    public bool option_showing = false;
    protected Canvas item_options_show;//Canvas for showing item options upon right click
    public Canvas item_options;
    public Canvas settings_canvas;
    private Canvas settings_canvas_show;
    private Text settings_text;
    private string[] settings = new string[5];
    private byte _cooldown_counter;
    private byte cooldown_counter
    {
        get { return _cooldown_counter; }
        set
        {
            if(value == Byte.MaxValue)
            {
                value = Byte.MinValue;
            }
            _cooldown_counter = value;
        }
    }

    void Start()
    {
        settings_canvas_show = Instantiate(settings_canvas, Vector3.zero, settings_canvas.transform.rotation, transform);
        settings_canvas_show.transform.localPosition = Vector3.zero;
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
        int index = Array.FindIndex(settings, delegate (string str)
         {
             return (str == s);
         });
        if(index != -1)
        {
            AddSetting("", index);
        }
    }

    public IEnumerator Cooldown(float time)
    {
        const int SETTING_INDEX = 2;
        cooldown_counter += 1;
        byte ORIGINAL_COUNTER = cooldown_counter;
        int seconds = (int)time;  //This gets the time without the extra milliseconds
        string setting = "<color=yellow>" + seconds.ToString() + "</color>";
        AddSetting(setting, SETTING_INDEX);
        /*Wait the length of the milliseconds first,so that
        we only need to worry about decrementing seconds*/
        yield return new WaitForSeconds(time - seconds);
        while (seconds > 0)
        {
            setting = "<color=yellow>" + seconds.ToString() + "</color>";
            AddSetting(setting, SETTING_INDEX);
            yield return new WaitForSeconds(1);
            seconds--;
        }
        if (cooldown_counter == ORIGINAL_COUNTER)
        {
            RemoveSetting(setting);
        }
    }

    public void OnPointerEnter()
    {
        item_descritption_canvas_show = TextBox.Instance.CreateDescBox(
            transform,
            new Vector3(transform.position.x + 2.5f,transform.position.y,transform.position.z +2.5f), 
            item_script.ToString()
            );  
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
