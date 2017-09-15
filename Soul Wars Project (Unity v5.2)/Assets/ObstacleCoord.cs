using UnityEngine;
using UnityEngine.Networking;

public class ObstacleCoord : NetworkBehaviour
{
    
    void Start()
    {
        Collider col = GetComponent<Collider>();
        float max_x = col.bounds.max.x;
        float max_z = col.bounds.max.z;
        float min_x = col.bounds.min.x;
        float min_z = col.bounds.min.z;

        Coordinate max_coord = Map.Instance.GetPos(new Vector3(max_x, 11, max_z));
        Coordinate min_coord = Map.Instance.GetPos(new Vector3(min_x, 11, min_z));
        for (uint i = min_coord.x; i <= max_coord.x; i++)
        {
            for (uint j = min_coord.z; j <= max_coord.z; j++)
            {
               Map.Instance.RemoveCoord(new ValueGroup<uint, uint>(i, j));
            }
        }

    }
}

