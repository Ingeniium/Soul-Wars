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
    [SyncVar] public Vector3 move_dir;
    [SyncVar] public Quaternion rotation;
    protected bool blocking = false;

    protected virtual void Start()
    {
        shield_speed = speed / 2;
        StartCoroutine(AddUnitToPlayersAlive());
    }

    [Command]
    protected void CmdStartShieldBlocking()
    {
        StartShieldBlocking();
    }
    
    [Command]
    protected void CmdEndShieldBlocking()
    {
        //RpcEndShieldBlocking();
        EndShieldBlocking();
    }

    [ClientRpc]
    protected void RpcStartShieldBlocking()
    {
        if (!Shield.GetComponent<ShieldHealthDefence>().regen)
        {
            StartShieldBlocking();
        }
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
        CustomNTC ntc = Shield.GetComponent<CustomNTC>();
        ntc.local_position = new Vector3(0, 0, .5f);
        ntc.local_rotation_eulers = new Vector3(0, 90, 0);
       // Quaternion rot = Quaternion.AngleAxis(-90, Vector3.up);
       // Shield.transform.rotation *= rot;
        if (Gun)
        {
            /* Gun.transform.rotation *= rot;
             Vector3 temp = Gun.transform.position;
             Gun.transform.position = Shield.transform.position;
             Shield.transform.position = temp;   */
            CustomNTC gun_ntc = Gun.GetComponent<CustomNTC>();
            gun_ntc.local_position = new Vector3(1, 0, 0);
            gun_ntc.local_rotation_eulers = ntc.local_rotation_eulers;
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
        Shield.GetComponent<Collider>().enabled = false;
        CustomNTC ntc = Shield.GetComponent<CustomNTC>();
        ntc.local_position = new Vector3(.5f, 0, 0);
        ntc.local_rotation_eulers = new Vector3(0, 0, 0);
        // Quaternion rot = Quaternion.AngleAxis(90, Vector3.up);
        // Shield.transform.rotation *= rot;
        //Shield.GetComponent<Collider>().enabled = false;
        if (Gun)
        {
            /*Gun.transform.rotation *= rot;
            Vector3 temp = Gun.transform.position;
            Gun.transform.position = Shield.transform.position;
            Shield.transform.position = temp;    */
            CustomNTC gun_ntc = Gun.GetComponent<CustomNTC>();
            gun_ntc.local_position = new Vector3(0, 0, .8f);
            gun_ntc.local_rotation_eulers = ntc.local_rotation_eulers;
        }
        speed = shield_speed * 2;
    }

    protected abstract IEnumerator AddUnitToPlayersAlive();
  

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
