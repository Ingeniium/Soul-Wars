using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

class ModDisplay : NetworkBehaviour
{
    public SyncListString Mods = new SyncListString();

    public void  OnMouseEnter()
    {
        foreach (string s in Mods)
        {
             PlayerController.Client.mod_text.text += s;
        }
        
    }

    public void OnMouseExit()
    {
         PlayerController.Client.mod_text.text = "";
    }

   
}
