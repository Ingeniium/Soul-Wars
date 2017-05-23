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
    public GameObject Gun;
    public GameObject Shield;
    protected Collider shield_collider;
    [SyncVar] public float speed = 75f;

    protected void StartShieldBlocking()
    {
        shield_collider.enabled = true;
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
        Quaternion rot = Quaternion.AngleAxis(90, Vector3.up);
        Shield.transform.rotation *= rot;
        Gun.transform.rotation *= rot;
        shield_collider.enabled = false;
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
