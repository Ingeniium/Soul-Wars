using System;
using System.Collections.Generic;
using UnityEngine;

public abstract partial class Gun : Item
{
    protected class GunMod
    {
        public Gun_Abilities ability
        {
            get { return _ability; }
            private set
            {
                _ability = value;
            }
        }
        Gun_Abilities _ability;
        public string addon
        {
            get { return _addon; }
            private set
            {
                _addon = value;
            }
        }
        string _addon;
        public string description
        {
            get { return _description; }
            private set
            {
                _description = value;
            }
        }
        string _description;

        public GunMod(Gun_Abilities _ability,string _addon,string _description)
        {
            ability = _ability;
            addon = _addon;
            description = _description;
        }
    }
}
