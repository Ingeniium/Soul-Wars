using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using System.IO;

class Record : MonoBehaviour
{
    public static XDocument file;
    public static XElement element;
    public static FileStream stream;
    public static readonly string player_name = "Supnaplamqw";

    static GameObject GetRespectivePrefab(XElement e)
    {
        return Resources.Load(e.Name.ToString()) as GameObject;
    }


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
                foreach (Item i in Item.Player.GetComponentsInChildren<Item>())
                {
                    element.Add(i.RecordValuesToSaveFile());
                }
                foreach (Item i in Item.inv.GetComponentsInChildren<Item>())
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
            XElement parent = file.Root.Element(player_name);
            GameObject image_canvas = Resources.Load("ItemImageCanvas") as GameObject;
            Type t;
            //GameObject first_gun = GetComponentInChildren<Gun>().gameObject;
            foreach (XElement e in parent.Elements())
            {
                if (e.Attribute("Index") != null)
                {
                    t = Type.GetType(e.Name.ToString());
                    GameObject image_canvas_show = Instantiate(image_canvas, image_canvas.transform.position, image_canvas.transform.rotation) as GameObject;
                    image_canvas_show.GetComponentInChildren<ItemImage>().Item_ = GetRespectivePrefab(e);
                    image_canvas_show.GetComponentInChildren<BoxCollider>().center = Vector2.zero;
                    image_canvas_show.AddComponent(t);
                    Item i = image_canvas_show.GetComponent(t) as Item;
                    i._item_image = image_canvas_show;
                    i.in_inventory = true;
                    i.RecordValuesFromSaveFile(e);
                    i.set = true;
                    if (Int32.Parse(e.Attribute("Index").Value) < 0)
                    {
                        Item.inv.InsertItem(ref image_canvas_show);
                        image_canvas_show.GetComponentInChildren<ItemImage>().item_script = i;
                    }
                    else
                    {
                        i.PrepareItemForUse();
                        if(Int32.Parse(e.Attribute("Index").Value) == 0)
                        {
                          // Destroy(first_gun);
                        }
                    }
                }
            }
            //XElement element = parent.Element("Strike");
            //Item.Player.gun.RecordValuesFromSaveFile(element);           
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

    void Start()
    {
        if (File.Exists("SoulWars.xml"))
        {
            LoadFromFile();
        }
    }

    void Update()
    {
        if (SpawnManager.AllySpawnPoints.Count == 2)
        {
            SaveToFile();
            SaveToFile();
            enabled = false;
        }
    }
}
