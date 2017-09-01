using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;

public class ObstacleCoord : NetworkBehaviour
{
    public static List<Coordinate> Coordinates = new List<Coordinate>();
  /*  {
        new Coordinate(2,5),
        new Coordinate(3,5),
        new Coordinate(4,5),
        new Coordinate(5,5),
        new Coordinate(6,5),
        new Coordinate(7,5),
        new Coordinate(8,5),
        new Coordinate(9,5),
        new Coordinate(10,5),
    };*/

    
    void Start()
    {
        Collider col = GetComponent<Collider>();
        float max_x = col.bounds.max.x;
        float max_z = col.bounds.max.z;
        float min_x = col.bounds.min.x;
        float min_z = col.bounds.min.z;

        Coordinate max_coord = Map.Instance.GetPos(new Vector3(max_x, 11, max_z));
        Coordinate min_coord = Map.Instance.GetPos(new Vector3(min_x, 11, min_z));
        Coordinate coord;
        for (uint i = min_coord.x; i <= max_coord.x; i++)
        {
            for (uint j = min_coord.z; j <= max_coord.z; j++)
            {
                coord = Map.Instance.GetPos(i, j);
                if (!Coordinates.Contains(coord))
                {
                    Coordinates.Add(coord);
                }
            }
        }

    }
}

