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
                    foreach(Renderer rend in Gun.GetComponentsInChildren<Renderer>())
                    {
                        rend.enabled = false;
                    }
                }
                foreach(Renderer rend in value.gameObject.GetComponentsInChildren<Renderer>())
                {
                    rend.enabled = true;
                }             
                Gun = value.gameObject;
                _main_gun = value;
            }
        }
    }
    protected Gun _main_gun;
    public List<Gun> weapons = new List<Gun>();
    public GameObject Gun;
    public GameObject Shield;
    [SyncVar] public float speed = 20f;
    [SyncVar] public float shield_speed;
    protected bool blocking = false;

    protected virtual void Start()
    {
        shield_speed = speed / 2;
    }

    [Command]
    protected void CmdStartShieldBlocking()
    {
        RpcStartShieldBlocking();
    }
    
    [Command]
    protected void CmdEndShieldBlocking()
    {
        RpcEndShieldBlocking();
    }

    [ClientRpc]
    protected void RpcStartShieldBlocking()
    {
        StartShieldBlocking();
    }

    [ClientRpc]
    protected void RpcEndShieldBlocking()
    {
        EndShieldBlocking();
    }

    protected virtual void StartShieldBlocking()
    {
        blocking = true;
        Shield.GetComponent<Collider>().enabled = true;
        Quaternion rot = Quaternion.AngleAxis(-90, Vector3.up);
        Shield.transform.rotation *= rot;
        if (Gun)
        {
            Gun.transform.rotation *= rot;
            Vector3 temp = Gun.transform.position;
            Gun.transform.position = Shield.transform.position;
            Shield.transform.position = temp;
        }
        speed = shield_speed;
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
        Shield.GetComponent<Collider>().enabled = false;
        if (Gun)
        {
            Gun.transform.rotation *= rot;
            Vector3 temp = Gun.transform.position;
            Gun.transform.position = Shield.transform.position;
            Shield.transform.position = temp;
        }
        speed = shield_speed * 2;
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
