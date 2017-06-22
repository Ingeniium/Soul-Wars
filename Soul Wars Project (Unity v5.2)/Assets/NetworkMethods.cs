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
        if (class_name != "Collider")
        {
            Type t = Type.GetType(class_name);
            MonoBehaviour c = obj.GetComponent(t) as MonoBehaviour;
            c.enabled = enabled;
        }
        else
        {
            obj.GetComponent<Collider>().enabled = enabled;
        }
    }
    /*For better position syncing*/
    [ClientRpc]
    public void RpcSetPosition(GameObject obj, Vector3 pos)
    {
        if (obj)
        {
            obj.transform.position = pos;
        }
    }

    [ClientRpc]
    public void RpcSetScale(GameObject Obj, Vector3 scale)
    {
        Obj.transform.localScale = scale;
    }

    [Command]
    public void CmdSetLayer(GameObject obj,int layer)
    {
        RpcSetLayer(obj,layer);
    }

    [Command]
    public void CmdSetColor(GameObject rend, Color color)
    {
        RpcSetColor(rend, color);
    }

    [Command]
    public void CmdSetEnabled(GameObject obj, string class_name, bool enabled)
    {
        RpcSetEnabled(obj, class_name, enabled);
    }

    [Command]
    public void CmdAddPlayerId(NetworkInstanceId ID)
    {
        PlayersAlive.Instance.Players.Add(ID.Value);
    }

    [Command]
    public void CmdRemovePlayerId(NetworkInstanceId ID)
    {
        PlayersAlive.Instance.Players.Remove(ID.Value);
    }

}

    


