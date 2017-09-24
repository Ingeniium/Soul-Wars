using System;
using UnityEngine;
using UnityEngine.Networking;

/*ClientRpcs can't be extension methods*/
public class NetworkMethods : NetworkBehaviour
{
    public static NetworkMethods Instance;
    /*pass_over is used as a reference for setting
     network instantiated objects as local 
     client values.*/
    void Awake()
    {
        Instance = this;
    }

   
    [ClientRpc]
    public void RpcSetParent(GameObject tr, GameObject ptr,Vector3 pos,Quaternion rot)
    {
        if (tr && ptr)
        {
            tr.transform.SetParent(ptr.transform);
            if (pos != Vector3.zero)
            {
                tr.transform.localPosition = pos;
            }
            tr.transform.rotation *= rot;
        }
        else
        {
            Debug.Log(false);
        }
    }

    [ClientRpc]
    public void RpcSetColor(GameObject rend, Color color)
    {
        if (rend)
        {
            rend.GetComponent<Renderer>().material.color = color;
        }
    }

    [ClientRpc]
    public void RpcSetLayer(GameObject obj, int layer)
    {
        if (obj)
        {
            obj.layer = layer;
        }
    }

    [ClientRpc]
    public void RpcSetEnabled(GameObject obj,string class_name,bool enabled)
    {
        if (obj)
        {
            if (class_name != "Collider" && class_name != "Renderer")
            {
                Type t = Type.GetType(class_name);
                if (t != null)
                {
                    MonoBehaviour c = obj.GetComponent(t) as MonoBehaviour;
                    c.enabled = enabled;
                }
                else
                {
                    Debug.Log(class_name + obj.ToString());
                }
            }
            else if (class_name == "Collider")
            {
                obj.GetComponent<Collider>().enabled = enabled;
            }
            else
            {
                obj.GetComponent<Renderer>().enabled = enabled;
                foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
                {
                    rend.enabled = enabled;
                }
            }
        }
    }
    

    [ClientRpc]
    public void RpcSetScale(GameObject Obj, Vector3 scale)
    {
        if (Obj)
        {
            Obj.transform.localScale = scale;
        }
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
    public void CmdSetScale(GameObject Obj, Vector3 scale)
    {
        RpcSetScale(Obj, scale);
    }

    [Command]
     public void CmdSetParentNull(GameObject obj)
     {
        RpcSetParentNull(obj);
     }

    [ClientRpc]
    void RpcSetParentNull(GameObject obj)
    {
        obj.transform.SetParent(null);
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

    [Command]
    public void CmdSpawn(string Obj,GameObject parent,Vector3 pos,Quaternion rot)
    {
        GameObject obj = Instantiate(Resources.Load(Obj) as GameObject
            , pos, rot) as GameObject;
        NetworkServer.Spawn(obj);
        if (parent)
        {
            RpcSetParent(obj, parent, pos, rot);
        }
    }
}

    


