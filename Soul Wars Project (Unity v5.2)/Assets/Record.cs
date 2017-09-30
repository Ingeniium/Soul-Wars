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
    private char[] forbidden_chars = new char[]
    {
        '+',
        '=',
        '&',
        '^',
        '%',
        '$',
        '#',
        '@',
        '~',
        '<',
        '>',
        '/',
        '{',
        '}',
        '[',
        ']',
        '?',
        '!',
        ' '
    };


    public void SaveToFile()
    {
        PlayerController.Client.weapons.RemoveNull();
        try
        {
            stream = new FileStream("SoulWars.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            file = XDocument.Load("SoulWars.xml");
            if (file.Root.Descendants(player_name).Any())
            {
                file.Root.Descendants(player_name).Remove();
            }
            file.Root.Add(new XElement(player_name, null));
            XElement element = file.Root.Element(player_name);
            foreach (Gun g in PlayerController.Client.weapons)
            {
                element.Add(g.RecordValuesToSaveFile());
            }
            file.Save("SoulWars.xml");

        }
        catch (System.IO.IsolatedStorage.IsolatedStorageException e)
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

    IEnumerator LoadFromFile()
    {
        try
        {
            stream = new FileStream("SoulWars.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            file = XDocument.Load("SoulWars.xml");
            player_name = (file.Root.FirstNode as XElement).Name.ToString();
            PlayerController.Client.player_name = player_name;
            XElement parent = file.Root.Element(player_name);
            GameObject image_canvas = Resources.Load("ItemImageCanvas") as GameObject;
            int i = 0;
            foreach (XElement e in parent.Elements())
            {
                GameObject image_canvas_show = Instantiate(image_canvas,
                    PlayerController.Client.player_interface_show.GetComponentInChildren<HorizontalLayoutGroup>().gameObject.transform.position,
                    image_canvas.transform.rotation) as GameObject;

                NetworkMethods.Instance.CmdSpawn(
                    e.Name.ToString(),
                    PlayerController.Client.gameObject,
                    new Vector3(.21f, .11f, .902f),
                    new Quaternion(0, 0, 0, 0));

                while (PlayerController.Client.GetComponentsInChildren<Item>().Length == i)
                {
                    yield return new WaitForEndOfFrame();
                }
                Item item = PlayerController.Client.GetComponentsInChildren<Item>()[i];
                item.item_image_show = image_canvas_show;
                item.in_inventory = true;
                item.RecordValuesFromSaveFile(e);
                image_canvas_show.GetComponentInChildren<ItemImage>().item_script = item;
                item.client_user = PlayerController.Client;
                item.PrepareItemForUse();
                i++;
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

    void SetUpNameInputField()
    {
        InputField f = name_input_show.GetComponentInChildren<InputField>();
        f.onEndEdit.AddListener(delegate (string s)
        {
            bool invalid = false;
            foreach (char c in s)
            {
                if (forbidden_chars.Contains(c))
                {
                    invalid = true;
                    break;
                }
            }
            if (s == "")
            {
                invalid = true;
            }
            if (invalid)
            {
                f.text = "No spaces/special chars.";
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
        script.client_user = PlayerController.Client;
        strike.GetComponentInChildren<ItemImage>().item_script = script;

        GameObject flurry = Instantiate(image_canvas, weapon_choose_show.transform.position, image_canvas.transform.rotation) as GameObject;
        flurry.transform.SetParent(weapon_choose_show.transform);
        flurry.GetComponent<RectTransform>().localPosition += new Vector3(-2.5f, -20, 0);
        flurry.AddComponent<Flurry>();
        script = flurry.GetComponent<Flurry>();
        script.SetBaseStats();
        script.in_inventory = true;
        script.client_user = PlayerController.Client;
        flurry.GetComponentInChildren<ItemImage>().item_script = script;

        GameObject haze = Instantiate(image_canvas, weapon_choose_show.transform.position, image_canvas.transform.rotation) as GameObject;
        haze.transform.SetParent(weapon_choose_show.transform);
        haze.GetComponent<RectTransform>().localPosition += new Vector3(2.5f, -20, 0);
        haze.AddComponent<Haze>();
        script = haze.GetComponent<Haze>();
        script.SetBaseStats();
        script.in_inventory = true;
        script.client_user = PlayerController.Client;
        haze.GetComponentInChildren<ItemImage>().item_script = script;

        GameObject blaster = Instantiate(image_canvas, weapon_choose_show.transform.position, image_canvas.transform.rotation) as GameObject;
        blaster.transform.SetParent(weapon_choose_show.transform);
        blaster.GetComponent<RectTransform>().localPosition += new Vector3(7.5f, -20, 0);
        blaster.AddComponent<Blaster>();
        script = blaster.GetComponent<Blaster>();
        script.SetBaseStats();
        script.in_inventory = true;
        script.client_user = PlayerController.Client;
        blaster.GetComponentInChildren<ItemImage>().item_script = script;

        for (int i = 0; i < 2; i++)
        {
            weapon_choose_show.GetComponentInChildren<Text>().text = "Choose weapon " + (i + 1).ToString();
            Gun[] weapons = weapon_choose_show.GetComponentsInChildren<Gun>();
            int j = -1;
            while (j == -1)
            {
                yield return new WaitForEndOfFrame();
                j = Array.FindIndex(weapons, delegate (Gun g)
                {
                    return (g.in_inventory == false);
                });
            }
            NetworkMethods.Instance.CmdSpawn(weapons[j].GetBaseName(),
                PlayerController.Client.gameObject,
                 new Vector3(.004f, .005f, .794f),
                 new Quaternion(0, 0, 0, 0));
            while (PlayerController.Client.GetComponentsInChildren<Gun>().Length == i)
            {
                yield return new WaitForEndOfFrame();
            }
            Gun gun = PlayerController.Client.GetComponentsInChildren<Gun>()[i];
            GameObject image = weapons[j].gameObject;
            Destroy(weapons[j]);
            image.GetComponentInChildren<ItemImage>().item_script = gun;
            gun.client_user = PlayerController.Client;
            gun.item_image_show = image;
            if (i == 0)
            {
                /*Apparently INput.inputstring is blank for leftmouse clicks*/
                gun.button = "";
            }
            else if (i == 1)
            {
                gun.button = "q";
            }
            gun.PrepareItemForUse();
            if (i > 0)
            {
                NetworkMethods.Instance.CmdSetEnabled(gun.gameObject, "Renderer", false);
            }
            else
            {
                PlayerController.Client.CmdEquipGun(gun.gameObject);
            }
        }
        NetworkMethods.Instance.CmdSetLayer(PlayerController.Client.gameObject, 9);
        NetworkMethods.Instance.CmdSetEnabled(PlayerController.Client.gameObject, "PlayerController", true);
        Button[] buttons = PlayerController.Client.player_interface_show.GetComponentsInChildren<Button>();
        buttons[buttons.Length - 1].enabled = true;
        Destroy(weapon_choose_show.gameObject);

    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (PlayerController.Client)
        {
            Button[] buttons = PlayerController.Client.player_interface_show.GetComponentsInChildren<Button>();
            if (File.Exists("SoulWars.xml"))
            {
                StartCoroutine(LoadFromFile());
            }
            else
            {
                name_input_show = Instantiate(name_input, name_input.transform.position, name_input.transform.rotation) as Canvas;
                NetworkMethods.Instance.CmdSetLayer(PlayerController.Client.gameObject, 15);
                NetworkMethods.Instance.CmdSetEnabled(PlayerController.Client.gameObject, "PlayerController", false);
                buttons[buttons.Length - 1].enabled = false;
                SetUpNameInputField();
            }
            buttons[buttons.Length - 1].onClick.AddListener(delegate ()
            {
                SaveToFile();
            });
        }
    }


}
