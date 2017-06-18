using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/*ClientRpcs can't be extension methods*/
public class NetworkMethods : NetworkBehaviour
{
    public static NetworkMethods Instance;

    void Awake()
    {
        Instance = this;
    }

    [ClientRpc]
    public void RpcSetParent(GameObject tr, GameObject ptr,Vector3 pos,Quaternion rot)
    {
        tr.transform.SetParent(ptr.transform);
        if(pos != Vector3.zero)
        {
            tr.transform.localPosition = pos;
        }
        tr.transform.rotation *= rot;
    }

    [ClientRpc]
    public void RpcSetColor(GameObject rend, Color color)
    {
        rend.GetComponent<Renderer>().material.color = color;
    }

    [ClientRpc]
    public void RpcSetLayer(GameObject obj, int layer)
    {
        obj.layer = layer;
    }

    [ClientRpc]
    public void RpcSetEnabled(GameObject obj,string class_name,bool enabled)
    {
        Type t = Type.GetType(class_name);
        NetworkBehaviour c = obj.GetComponent(t) as NetworkBehaviour;
        c.enabled = enabled;
    }

    [Command]
    public void CmdSetLayer(GameObject obj,int layer)
    {
        RpcSetLayer(obj,layer);
    }

    //[Command]
    

}

    


