using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public abstract class GenericController : NetworkBehaviour
{
    [HideInInspector]public float total_damage_to_units = 0;
    [HideInInspector]public float num_of_deaths = 0;
    [HideInInspector]public float num_shot_that_hit = 0;
    public Gun gun;
    public GameObject Gun;
    public GameObject Shield;
    [SyncVar] public float speed = 20f;
    protected bool blocking = false;
    protected BoxCollider shield_collider;
    protected void StartShieldBlocking()
    {
        blocking = true;
        NetworkMethods.Instance.CmdSetEnabled(Shield, "Collider", true);
        Quaternion rot = Quaternion.AngleAxis(-90, Vector3.up);
        Shield.transform.rotation *= rot;
        Gun.transform.rotation *= rot;
        Vector3 temp = Gun.transform.position;
        Gun.transform.position = Shield.transform.position;
        Shield.transform.position = temp;
        speed /= 2;
    }

    protected void EndShieldBlocking()
    {
        blocking = false;
        Quaternion rot = Quaternion.AngleAxis(90, Vector3.up);
        Shield.transform.rotation *= rot;
        NetworkMethods.Instance.CmdSetEnabled(Shield, "Collider", false);
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
