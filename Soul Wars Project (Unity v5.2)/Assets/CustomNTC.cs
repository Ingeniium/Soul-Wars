using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class CustomNTC : NetworkBehaviour
{
    private Transform _parent;
    public Transform parent
    {
        get { return _parent; }
        set
        {
            _parent = value;
            if (isServer)
            {
                if (value)
                {
                    parent_id = _parent.GetComponent<NetworkIdentity>().netId;
                }
                RpcSyncPos(_parent.gameObject);
            }
        }
    }
    [SyncVar] public Vector3 local_position;
    [SyncVar] public Vector3 local_rotation_eulers;
    [SyncVar] NetworkInstanceId parent_id;
    private static List<CustomNTC> ntcs = new List<CustomNTC>();
    public static CustomNTC first
    {
        get
        {
            if (ntcs.Count > 0)
            {
                return ntcs[0];
            }
            return null;
        }
    }

    void Awake()
    {
        ntcs.Add(this);
    }

    [ClientRpc]
    public void RpcSyncNTCS()
    {
        foreach(CustomNTC ntc in ntcs)
        {
            StopCoroutine(SyncPos());
            parent = ClientScene.FindLocalObject(parent_id).transform;
            transform.parent = parent;
            StartCoroutine(SyncPos());
        }
    }

    [ClientRpc] 
    void RpcSyncPos(GameObject obj)
    {
        parent = obj.transform;
        transform.parent = parent;
        StartCoroutine(SyncPos());
    }

    IEnumerator SyncPos()
    {
        while(parent)
        {
            yield return new WaitForFixedUpdate();
            transform.localPosition = local_position;
            transform.localRotation = Quaternion.Euler(local_rotation_eulers);
        }
    }
}
