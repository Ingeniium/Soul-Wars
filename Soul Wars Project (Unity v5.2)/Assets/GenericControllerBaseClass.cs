using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public abstract class GenericController : NetworkBehaviour
{
    public Gun main_gun
    {
        get { return _main_gun; }
        set
        {
            if (value != _main_gun && value != null)
            {
                if (Gun)
                {
                    Gun.GetComponent<Renderer>().enabled = false;
                }
                value.gameObject.GetComponent<Renderer>().enabled = true;              
                Gun = value.gameObject;
                _main_gun = value;
            }
        }
    }
    protected Gun _main_gun;
    public Gun[] weapons = new Gun[3];
    public GameObject Gun;
    public GameObject Shield;
    [SyncVar] public float speed = 20f;
    protected bool blocking = false;
    protected BoxCollider shield_collider;
    protected void StartShieldBlocking()
    {
        blocking = true;
        if (isServer)
        {
            NetworkMethods.Instance.RpcSetEnabled(Shield, "Collider", true);
        }
        else
        {
            NetworkMethods.Instance.CmdSetEnabled(Shield, "Collider", true);
        }
        Quaternion rot = Quaternion.AngleAxis(-90, Vector3.up);
        Shield.transform.rotation *= rot;
        Gun.transform.rotation *= rot;
        Vector3 temp = Gun.transform.position;
        Gun.transform.position = Shield.transform.position;
        Shield.transform.position = temp;
        speed /= 2;
    }

    [Command]
    public void CmdEquipGun(GameObject g)
    {
        RpcEquipGun(g);
    }

    [ClientRpc]
    protected void RpcEquipGun(GameObject g)
    {
        main_gun = g.GetComponent<Gun>();
    }


    protected void EndShieldBlocking()
    {
        blocking = false;
        Quaternion rot = Quaternion.AngleAxis(90, Vector3.up);
        Shield.transform.rotation *= rot;
        if (isServer)
        {
            NetworkMethods.Instance.RpcSetEnabled(Shield, "Collider", false);
        }
        else
        {
            NetworkMethods.Instance.CmdSetEnabled(Shield, "Collider", false);
        }
        Gun.transform.rotation *= rot;
        Vector3 temp = Gun.transform.position;
        Gun.transform.position = Shield.transform.position;
        Shield.transform.position = temp;
        speed *= 2;
    }

    protected void ToggleGunHoming(Gun gun)
    {
        if (gun.homes)
        {
            gun.homes = false;
            if (gun.bullet)
            {
                gun.bullet.GetComponentInChildren<HomingScript>().enabled = false;
            }
        }
        else
        {
            gun.homes = true;
            if (gun.bullet)
            {
                gun.bullet.GetComponentInChildren<HomingScript>().enabled = true;
            }
        }
    }


}
