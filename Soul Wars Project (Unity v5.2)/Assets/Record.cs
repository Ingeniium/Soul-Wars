using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.IO;

class Record : MonoBehaviour
{
    public static Record Instance;
    private XDocument file;
    private XElement element;
    private FileStream stream;
    public Canvas name_input;
    private Canvas name_input_show;
    public Canvas weapon_choose;
    private Canvas weapon_choose_show;
    private string player_name;


    public void SaveToFile()
    {
            try
            {
                stream = new FileStream("SoulWars.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                file = XDocument.Load("SoulWars.xml");
                if (file.Root.Descendants(player_name).Any())
                {
                    file.Root.Descendants(player_name).Remove();
                }
                file.Root.Add(new XElement(player_name,null));
                XElement element = file.Root.Element(player_name);
                foreach (Item i in PlayerController.Client.GetComponentsInChildren<Item>())
                {
                    element.Add(i.RecordValuesToSaveFile());
                }
                foreach (Item i in PlayerController.Client.gun.inv.GetComponentsInChildren<Item>())
                {
                    element.Add(i.RecordValuesToSaveFile());
                }
                file.Save("SoulWars.xml");
            }
            catch (FileNotFoundException e)
            {
                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                }
                File.WriteAllText("SoulWars.xml", "<SaveData></SaveData>");
                SaveToFile();
                return;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                }
            }
            return;   
    }

    void LoadFromFile()
    {
        try
        {
            stream = new FileStream("SoulWars.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            file = XDocument.Load("SoulWars.xml");
            player_name = (file.Root.FirstNode as XElement).Name.ToString();
            PlayerController.Client.player_name = player_name;
            XElement parent = file.Root.Element(player_name);
            GameObject image_canvas = Resources.Load("ItemImageCanvas") as GameObject;
            Type t;
            foreach (XElement e in parent.Elements())
            {
                if (e.Attribute("Index") != null)
                {
                    t = Type.GetType(e.Name.ToString());
                    GameObject image_canvas_show = Instantiate(image_canvas, PlayerController.Client.hpbar_show.GetComponentInChildren<VerticalLayoutGroup>().gameObject.transform.position, image_canvas.transform.rotation) as GameObject;
                    image_canvas_show.AddComponent(t);
                    Item i = image_canvas_show.GetComponent(t) as Item;
                    i._item_image = image_canvas_show;
                    i.in_inventory = true;
                    i.RecordValuesFromSaveFile(e);
                    i.set = true;
                    image_canvas_show.GetComponentInChildren<ItemImage>().item_script = i;
                    i.client_user = PlayerController.Client;
               
                    if (Int32.Parse(e.Attribute("Index").Value) < 0)
                    {
                        PlayerController.Client.gun.inv.InsertItem(ref image_canvas_show);
                    }
                    else
                    {
                       StartCoroutine(i.PrepareItemForUse());
                    }
                }
            }
            PlayerController.Client.loaded = true;
            
              
        }
        finally
        {
            if (stream != null)
            {
                stream.Close();
                stream.Dispose();
            }
        }
    }

    void SetUpNameInputField()
    {
        InputField f = name_input_show.GetComponentInChildren<InputField>();
        f.onEndEdit.AddListener(delegate(string s)
        {
            if (s == "Type Your Name Here" || s == "")
            {
                f.ActivateInputField();
            }
            else
            {
                player_name = s;
                f.DeactivateInputField();
                PlayerController.Client.player_name = s;
                StartCoroutine(ChooseWeapon());
                Destroy(name_input_show.gameObject);
            }
        });

        f.ActivateInputField();
    }

    IEnumerator ChooseWeapon()
    {
        Gun script;
        weapon_choose_show = Instantiate(weapon_choose, weapon_choose.transform.position, weapon_choose.transform.rotation) as Canvas;
        weapon_choose_show.worldCamera = PlayerController.Client.cam_show;
        GameObject image_canvas = Resources.Load("ItemImageCanvas") as GameObject;
        GameObject strike = Instantiate(image_canvas, weapon_choose_show.transform.position, image_canvas.transform.rotation) as GameObject;
        strike.transform.SetParent(weapon_choose_show.transform);
        strike.GetComponent<RectTransform>().localPosition += new Vector3(-7.5f, -20, 0);
        strike.AddComponent<Strike>();
        script = strike.GetComponent<Strike>();
        script.SetBaseStats();
        script.in_inventory = true;
        script._item_image = strike;
        script.client_user = PlayerController.Client;
        strike.GetComponentInChildren<ItemImage>().item_script = script;

        GameObject flurry = Instantiate(image_canvas, weapon_choose_show.transform.position, image_canvas.transform.rotation) as GameObject;
        flurry.transform.SetParent(weapon_choose_show.transform);
        flurry.GetComponent<RectTransform>().localPosition += new Vector3(-2.5f, -20, 0);
        flurry.AddComponent<Flurry>();
        script = flurry.GetComponent<Flurry>();
        script.SetBaseStats();
        script.in_inventory = true;
        script._item_image = flurry;
        script.client_user = PlayerController.Client;
        flurry.GetComponentInChildren<ItemImage>().item_script = script;

        GameObject haze = Instantiate(image_canvas, weapon_choose_show.transform.position, image_canvas.transform.rotation) as GameObject;
        haze.transform.SetParent(weapon_choose_show.transform);
        haze.GetComponent<RectTransform>().localPosition += new Vector3(2.5f, -20, 0);
        haze.AddComponent<Haze>();
        script = haze.GetComponent<Haze>();
        script.SetBaseStats();
        script.in_inventory = true;
        script._item_image = haze;
        script.client_user = PlayerController.Client;
        haze.GetComponentInChildren<ItemImage>().item_script = script;

        GameObject blaster = Instantiate(image_canvas, weapon_choose_show.transform.position, image_canvas.transform.rotation) as GameObject;
        blaster.transform.SetParent(weapon_choose_show.transform);
        blaster.GetComponent<RectTransform>().localPosition += new Vector3(7.5f, -20, 0);
        blaster.AddComponent<Blaster>();
        script = blaster.GetComponent<Blaster>();
        script.SetBaseStats();
        script.in_inventory = true;
        script._item_image = blaster;
        script.client_user = PlayerController.Client;
        blaster.GetComponentInChildren<ItemImage>().item_script = script;

        while (!PlayerController.Client.pass_over)
        {
            yield return new WaitForEndOfFrame();
        }
        NetworkMethods.Instance.CmdSetLayer(PlayerController.Client.gameObject, 9);
        NetworkMethods.Instance.CmdSetEnabled(PlayerController.Client.gameObject, "PlayerController", true);          
        Destroy(weapon_choose_show.gameObject);
        
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (PlayerController.Client && !PlayerController.Client.loaded)
        {
            Debug.Log(PlayerController.Client.netId);
            if (File.Exists("SoulWars.xml"))
            {
                LoadFromFile();
            }
            else
            {
                name_input_show = Instantiate(name_input, name_input.transform.position, name_input.transform.rotation) as Canvas;
                NetworkMethods.Instance.CmdSetLayer(PlayerController.Client.gameObject, 15);
                NetworkMethods.Instance.CmdSetEnabled(PlayerController.Client.gameObject,"PlayerController", false);
                SetUpNameInputField();
            }
            Button[] buttons = PlayerController.Client.hpbar_show.GetComponentsInChildren<Button>();
            buttons[buttons.Length - 1].onClick.AddListener(delegate()
            {
                SaveToFile();
            });
        }
    }

    
}
