﻿
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public abstract partial class Gun : Item
{

    protected void BringUpLevelUpTable()
    {
        GunTable.gun_for_consideration = this;
        GunLevelUp.transform.SetParent(client_user.player_interface_show.transform);
        GunLevelUp.transform.localPosition = Vector3.zero;
        GunLevelUp.transform.rotation *= new Quaternion(0, 0, 0, 0);
    }

    protected static class GunTable
    {
        public static GunTableButton[] buttons;
        public static Gun gun_for_consideration
        {
            get { return _gun_for_consideration; }
            set
            {
                _gun_for_consideration = value;
                if (buttons == null || buttons[0] == null)
                {
                    InitGunTable();
                }
                NetworkMethods.Instance.CmdSetEnabled(
                    PlayerController.Client.gameObject,
                    "PlayerController",
                    false);
                PlayersAlive.Instance.CmdPause();
                NetworkMethods.Instance.CmdSetLayer(
                    _gun_for_consideration.client_user.gameObject,
                    LayerMask.NameToLayer("Invincible"));
                SetGunTable();
                
            }
        }
        private static Gun _gun_for_consideration;

        public static void InitGunTable()//For giving gameobject buttons a private class instance of GunTableButton
        {
            Button[] b = gun_for_consideration.GunLevelUp.GetComponentsInChildren<Button>();
            for (int i = 0; i < b.Length - 1; i++)
            {
                b[i].gameObject.AddComponent<GunTableButton>();
            }
            buttons = gun_for_consideration.GunLevelUp.GetComponentsInChildren<GunTableButton>();

            for (int i = 0; i < buttons.Length; i++)//Sets up GunTable Info
            {
                buttons[i].button = b[i];
                buttons[i].index = i;
            }
        }

        public static void SetGunTable()
        {
            foreach (GunTableButton g in buttons)
            {
                //Copy the names of the abilities to the strings of the buttons.
                g.GetComponentInChildren<Text>().text = gun_for_consideration.GetGunModName(g.index);
                /*For each row(composed of 3 buttons),if the level is too low,disable the 
                next row of buttons,turning them grey.*/
                if (g.index > 3 * gun_for_consideration.level - 1)
                {
                    ColorBlock cb = g.button.colors;
                    cb.disabledColor = Color.grey;
                    g.button.colors = cb;
                    g.button.interactable = false;
                }
                /*Otherwise,check for whether it was already clamied.If so,
                disable that specific button,switching colors with it and its
                associated string*/
                else if (gun_for_consideration.claimed_gun_ability.Contains(g.index))
                {
                    ColorBlock cb = g.button.colors;
                    cb.disabledColor = Color.yellow;
                    g.button.colors = cb;
                    g.button.interactable = false;
                }
                /*If neither of the conditions are true then proceed to
                make sure that the buttons is active */
                else if (g.button.interactable == false)
                {
                    g.button.interactable = true;
                }
            }
             for (int i = 0; i < buttons.Length - 1; i++)//Exclude "x" button
             {
                  int temp = i;
                  buttons[temp].method = gun_for_consideration.GetGunModAbility(temp);                 
             }
            
            if (gun_for_consideration.points == 0 && gun_for_consideration.level_up_indication)
            {//Destroy indication when there's no points
                Destroy(gun_for_consideration.level_up_indication.gameObject);
            }


        }

    }

    [ClientRpc]
    public void RpcApplyGunAbilities(int index)
    {
        points--;
        AddAbility(index);
    }

    protected class GunTableButton : MonoBehaviour
    {
        public Button button;
        public Gun_Abilities method
        {
            get { return _method; }
            set
            {
                _method = value;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(delegate
                {
                    if (GunTable.gun_for_consideration.points > 0 && method != null)
                    {
                        /*Add delegate to gun abilities*/
                        GunTable.gun_for_consideration.client_user.CmdApplyGunAbilities(
                            GunTable.gun_for_consideration.gameObject,
                            index);
                        /*Switch colors of text and button to show it has been taken*/
                        ColorBlock cb = button.colors;
                        cb.disabledColor = Color.yellow;
                        button.colors = cb;
                        button.GetComponentInChildren<Text>().color = Color.red;
                        button.interactable = false;
                        if (GunTable.gun_for_consideration.points == 0 && GunTable.gun_for_consideration.level_up_indication)
                        {
                            Destroy(GunTable.gun_for_consideration.level_up_indication.gameObject);
                        }
                    }
                });
            }
        }
        public int index;
        private Gun_Abilities _method;
        private Canvas desc_canvas_show;

        
        void OnMouseEnter()
        {
            desc_canvas_show = TextBox.Instance.CreateDescBox(
                null,
                transform.position + new Vector3(1.5f, 0, 1.75f),
                GunTable.gun_for_consideration.GetGunModDesc(index),
                true);
            desc_canvas_show.transform.SetParent(gameObject.transform);
            desc_canvas_show.GetComponentInChildren<Text>().text = GunTable.gun_for_consideration.GetGunModDesc(index);
        }

        void OnMouseExit()
        {
            if (desc_canvas_show)
            {
                Destroy(desc_canvas_show.gameObject);
            }
        }
    }
}
