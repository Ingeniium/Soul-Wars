using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TutorialHelper : MonoBehaviour
{
    private StringSeries[] advice_content_list;
    public List<Vector3> advice_location_list = new List<Vector3>();
    public List<bool> is_world_space_list = new List<bool>();
    public static TutorialHelper Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(WaitForTextBoxRef());
    }

    /*Sets up boxes based on values set at editor time*/
    IEnumerator WaitForTextBoxRef()
    {
        advice_content_list = GetComponents<StringSeries>();
        while(!TextBox.Instance || (!PlayerController.Client && !PlayerController.Client.player_interface_show) )
        {
            yield return new WaitForEndOfFrame();
        }
        for (int i = 0; i < advice_location_list.Count; i++)
        {
            string sentence = "";
            for(int j = 0;j < advice_content_list[i].strings.Count;j++)
            {
                sentence += advice_content_list[i].strings[j] + "\r\n";
            }
            TextBox.Instance.CreateExitDescBox(
                PlayerController.Client.player_interface_show.transform,
                advice_location_list[i],
                sentence,
                is_world_space_list[i]);
        }
    }

    /*Sets up a text box reminding players about leveling up*/
    public void LevelUpIndication(string gun_name,int gun_level)
    {
        List<string> strings = new List<string>();
        strings.Add("<Color=green>" + gun_name + " has leveled up to " + gun_level + "!</Color>");
        strings.Add("Right Click the gun's image in the weapon's");
        strings.Add("bar and click \" Allocate Gun Points \" in order");
        strings.Add("to choose which upgrade you want.");
        strings.Add("<Color=gray>You can't choose grayed out abilities until</Color>");
        strings.Add("<Color=gray>The gun reaches a higher level.</Color>");
        strings.Add("<Color=purple>Remember that each ability costs one point</Color>");
        strings.Add("<Color=purple>and that the max level is 5,so choose wisely!</Color>");

        string sentence = "";
        foreach(string s in strings)
        {
            sentence += s + "\r\n";
        }
        TextBox.Instance.CreateExitDescBox(
               null,
               new Vector3(-225,0,200),
               sentence,
               false);
    }

}
