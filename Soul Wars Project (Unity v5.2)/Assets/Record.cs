using System;
using System.Collections.Generic;
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
            PlayerController.Client.CmdCreateName(
                   player_name,
                   PlayerController.Client.gameObject);
            XElement parent = file.Root.Element(player_name);
            GameObject image_canvas = Resources.Load("ItemImageCanvas") as GameObject;
            Type t;
            uint first_gun_id = PlayerController.Client.gun.netId.Value;
            foreach (XElement e in parent.Elements())
            {
                if (e.Attribute("Index") != null)
                {
                    t = Type.GetType(e.Name.ToString());
                    GameObject image_canvas_show = Instantiate(image_canvas, PlayerController.Client.gun.weapons_bar.transform.position, image_canvas.transform.rotation) as GameObject;
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
                        i.PrepareItemForUse();
                        if (Int32.Parse(e.Attribute("Index").Value) == 0)
                        {
                            GameObject first_gun = ClientScene.FindLocalObject(new NetworkInstanceId(first_gun_id));
                            if (first_gun == null)
                            {
                                first_gun = NetworkServer.FindLocalObject(new NetworkInstanceId(first_gun_id));
                            }
                            PlayerController.Client.CmdDestroy(first_gun);
                        }
                    }
                }
            }
            
              
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

    void SetUpInputField()
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
                PlayerController.Client.enabled = true;
                PlayerController.Client.gameObject.layer = 9;
                PlayerController.Client.CmdCreateName(
                   player_name,
                   PlayerController.Client.gameObject);
                Destroy(name_input_show.gameObject);
            }
        });

        f.ActivateInputField();
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (PlayerController.Client)
        {
            if (File.Exists("SoulWars.xml"))
            {
                LoadFromFile();
            }
            else
            {
                name_input_show = Instantiate(name_input, name_input.transform.position, name_input.transform.rotation) as Canvas;
                PlayerController.Client.gameObject.layer = 15;
                PlayerController.Client.enabled = false;
                SetUpInputField();
            }
            Button[] buttons = PlayerController.Client.hpbar_show.GetComponentsInChildren<Button>();
            buttons[buttons.Length - 1].onClick.AddListener(delegate()
            {
                SaveToFile();
            });
        }
    }

    
}
