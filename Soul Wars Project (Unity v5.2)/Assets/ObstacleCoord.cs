using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class ObstacleCoord : NetworkBehaviour
{
    public static List<Coordinate> Coordinates = new List<Coordinate>();

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
        for (int i = min_coord.x - 1 ; i < max_coord.x + 1; i++) 
        {
            for (int j = min_coord.z - 1; j < max_coord.z + 1; j++)
            {
                coord = new Coordinate(i,j);
                if (!Coordinates.Contains(coord))
                {
                    Coordinates.Add(coord);
                }
            }
        }

    }
}

