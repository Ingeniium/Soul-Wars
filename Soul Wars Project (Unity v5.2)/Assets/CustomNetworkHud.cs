using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.SceneManagement;

public class CustomNetworkHud : MonoBehaviour
{
    private bool local;
    private uint level_to_load;
    private string match_text;
    public Button[] buttons;
    public GameObject match_list_interface;
    private GameObject match_list_interface_show;
    public GameObject match_button;
    public GameObject match_name;
    public Canvas level_button_canvas;
    public Type[] types;


    public enum Type
    {
        LocalHost = 0,
        ListMatch = 1,
        HiddenLevelButton = 2
    }

    void Start()
    {
        if (!NetworkManager.singleton)
        {
            Debug.Log("NetworkManager isn't fully set up!");
            return;
        }
        for (int i = 0; i < buttons.Length; i++)
        {
            switch (types[i])
            {
                case Type.LocalHost:
                    {
                        buttons[i].onClick.AddListener(delegate ()
                        {
                            transform.DetachChildren();
                            local = true;
                            RevealLevelButtons();
                        });
                        break;
                    }
                case Type.ListMatch:
                    {
                        buttons[i].onClick.AddListener(delegate ()
                        {
                            NetworkManager.singleton.StartMatchMaker();
                            match_list_interface_show = Instantiate(
                                match_list_interface,
                                match_list_interface.transform.position,
                                Quaternion.identity) as GameObject;
                            NetworkManager.singleton.matchMaker.ListMatches(0, 10, "", false, 0, 0, OnMatchList);
                            transform.DetachChildren();
                        });
                        break;
                    }
                case Type.HiddenLevelButton:
                    {
                        uint temp = (uint)i;
                        buttons[i].onClick.AddListener(delegate ()
                        {
                            const uint START_OF_LEVEL_BUTTON_OFFSET = 1;
                            temp -= START_OF_LEVEL_BUTTON_OFFSET;
                            level_to_load = temp;
                            if (local)
                            {
                                OnMatchCreated(true, null, null);
                            }
                            else
                            {
                                NetworkManager.singleton.matchMaker.CreateMatch(match_text, 6, true, "", "", "", 0, 0, OnMatchCreated);
                            }
                        });
                        level_button_canvas.enabled = false;
                        break;
                    }
            }
        }
    }

    /*CallBack that's called after all the info about the accessible matches are retrieved.
      Responsible for creating buttons which list and allow access to these matches,as well as
      creating one's own online match.*/
    public void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
    {
        if (success)
        {
            const float DELTA_Y = 20;
            float y_offset = 150;
            foreach (MatchInfoSnapshot m in matches)
            {
                Vector3 pos = new Vector3(
                    0,
                    y_offset,
                    0);
                y_offset -= DELTA_Y;

                GameObject match_button_show = Instantiate(
                    match_button,
                    pos,
                    Quaternion.identity,
                    match_list_interface_show.transform) as GameObject;

                match_button_show.transform.localPosition = pos;
                Text text = match_button_show.GetComponentInChildren<Text>();
                TitleMatchButton(text, m);

                Button b = match_button_show.GetComponentInChildren<Button>();
                b.onClick.AddListener(delegate ()
                {
                    NetworkManager.singleton.matchMaker.JoinMatch(m.networkId, "", "", "", 0, 0, OnMatchJoined);
                });
            }
            Debug.Log("Making create match button");
            MakeCreateMatchButton(y_offset, matches);
        }
        else
        {
            Debug.Log("Match Info Listing Failed!");
        }
    }

    /*Creates a button responsible for creating a new online match.*/
    void MakeCreateMatchButton(float y_offset, List<MatchInfoSnapshot> matches)
    {
        Vector3 pos = new Vector3(
                  0,
                  y_offset,
                  0);

        GameObject create_match_button = Instantiate(
            match_button,
            pos,
            Quaternion.identity,
            match_list_interface_show.transform) as GameObject;

        create_match_button.transform.localPosition = pos;

        Text text = create_match_button.GetComponentInChildren<Text>();
        text.text = "Create New Match";

        SetUpCreateMatchButtonListener(create_match_button, matches);
    }

    /*Sets up the listener for the create match button.*/
    void SetUpCreateMatchButtonListener(GameObject create_match_button, List<MatchInfoSnapshot> matches)
    {
        Button b = create_match_button.GetComponentInChildren<Button>();
        b.onClick.AddListener(delegate ()
        {
            GameObject match_name_show = Instantiate(
                match_name,
                Vector3.zero,
                match_list_interface_show.transform.rotation,
                match_list_interface_show.transform) as GameObject;

            match_name_show.transform.localPosition = Vector3.zero;
            const float SIZE = 20;
            match_name_show.transform.localScale = new Vector3(SIZE, SIZE / 2, 100);
            Text t = match_name_show.GetComponentInChildren<Text>();
            t.color = Color.blue;
            t.fontSize = (t.fontSize / 4) * 3;
            InputField input = match_name_show.GetComponentInChildren<InputField>();
            input.text = "Enter the name of the match.";

            input.onEndEdit.AddListener(delegate (string s)
            {
                if (s != null && s != "" &&
                    !matches.Exists(delegate (MatchInfoSnapshot match)
                    {
                        return match.name == s;
                    }))
                {
                    match_text = s;
                    RevealLevelButtons();
                    input.DeactivateInputField();
                    Destroy(match_name_show);
                }
                else
                {
                    input.text = "Either a match already has this name or no name is supplied";
                    input.ActivateInputField();
                }
                b.enabled = false;
            });
        });
    }

    /*Sets up the words to be displayed on the button.*/
    void TitleMatchButton(Text t, MatchInfoSnapshot m)
    {
        string text = "";
        const int NUM_SPACES = 20;
        for (int i = 0; i < NUM_SPACES; i++)
        {
            text += " ";
        }
        text += m.name + "  " + m.currentSize + "/" + m.maxSize;
        t.text = text;
    }

    /*Shows and enables the level buttons.*/
    void RevealLevelButtons()
    {
        level_button_canvas.enabled = true;
        if (match_list_interface_show)
        {
            Destroy(match_list_interface_show.gameObject);
        }
    }

    /*Called when a match is joined.*/
    public void OnMatchJoined(bool success, string extendedInfo, MatchInfo match)
    {
        if (success)
        {
            NetworkManager.singleton.StartClient();
        }
        else
        {
            Debug.Log("Match Joined Failed!");
        }
    }

    /*Called when a match is created.*/
    public void OnMatchCreated(bool success, string extendedInfo, MatchInfo match)
    {
        if (success)
        {
            // NetworkManager.singleton.StartServer();
            NetworkManager.singleton.ServerChangeScene("Level " + level_to_load);
            NetworkManager.singleton.StartHost();
            //NetworkManager.singleton.StartClient();
        }
        else
        {
            Debug.Log("Match Creation Failed!");
        }
    }

}

