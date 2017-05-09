using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

public abstract partial class Gun : Item
{

    protected void BringUpLevelUpTable()
    {
        GunTable.gun_for_consideration = this;
        GunLevelUp.transform.SetParent(client_user.hpbar_show.transform);
        GunLevelUp.transform.localPosition = Vector3.zero;
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
                if (GunTable.buttons == null)
                {
                    InitGunTable();
                }

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
                g.GetComponentInChildren<Text>().text = gun_for_consideration.ClassGunAbilityNames(g.index);
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
            if (gun_for_consideration.AreGunLevelUpButtonsAssignedForClass() == false)
            {
                for (int i = 0; i < buttons.Length - 1; i++)//Exclude "x" button
                {
                    int temp = i;
                    buttons[temp].method = gun_for_consideration.ClassGunMods(temp);
                }
            }
            if (gun_for_consideration.points == 0 && gun_for_consideration.level_up_indication)
            {//Destroy indication when there's no points
                Destroy(gun_for_consideration.level_up_indication.gameObject);
            }


        }

    }


    protected class GunTableButton : MonoBehaviour
    {
        public Button button;
        public Gun_Abilities method;
        public int index;

        void OnMouseDown()
        {
            if (GunTable.gun_for_consideration.points > 0 && button.interactable && method != null)
            {
                /*Add delegate to gun abilities*/
                --GunTable.gun_for_consideration.points;
                GunTable.gun_for_consideration.Claimed_Gun_Mods += method;
                GunTable.gun_for_consideration.SetGunNameAddons(index);
                GunTable.gun_for_consideration.claimed_gun_ability.Add(index);
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
        }
    }
}
