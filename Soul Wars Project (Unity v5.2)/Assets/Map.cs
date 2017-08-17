using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
//using Priority_Queue;

public delegate float Heuristic<S,T>(S s,T t);
public delegate float Heuristic<T>(T t);


public class Coordinate
{
    public int x;
    public int z;
    public float traverse_cost;
    
    public enum Status
    {
        Free = 0,
        /*For Advisory against crossing where a bullet is or might be*/
        Should_Not_Cross = 1,
        /*For natural obstacles like boundaries and players*/
        Can_Not_Cross = 2
    };
    public Status status
    {
        get { return _status; }
        set
        {
            _status = value;
        }
    }
    private Status _status;
    public Coordinate parent;
    public Coordinate(int _x,int _z,float cost = 0)
    {
        x = _x;
        z = _z;
        traverse_cost = cost;
        _status = Coordinate.Status.Free;
        parent = null;
    }
   

    public static bool operator == (Coordinate lhs,Coordinate rhs)
    {
        bool a = object.Equals(lhs, null);
        bool b = object.Equals(rhs, null);
        if (a && b)
        {
            return true;
        }
        else if ((!a && b) || (a && !b))
        {
            return false;
        }
        else
        {
            return (lhs.x == rhs.x && lhs.z == rhs.z);
        }     
    }

    public static bool operator !=(Coordinate lhs, Coordinate rhs)
    {
        bool a = object.Equals(lhs, null);
        bool b = object.Equals(rhs, null);
        if (a && b)
        {
            return false;
        }
        else if ((!a && b) || (a && !b))
        {
            return true;
        }
        else
        {
            return (lhs.x != rhs.x || lhs.z != rhs.z);
        }
     
    }

    public override bool Equals(object o)
    {
        if ((o as Coordinate) != this)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public override string ToString()
    {
        return x + " : " + z;
    }

   public Coordinate GetFoundingParent()
   {
       if (parent != null && parent.parent != null)
       {
           return parent.GetFoundingParent();
       }
       else
       {
           return this;
       }
   }

   public List<Coordinate> GetParents()
   {
       Coordinate c = this;
       List<Coordinate> coords = new List<Coordinate>();
       while (c != null)
       {
           coords.Add(c);
           c = c.parent;
       }
       return coords;
   }
         
            
    
    public List<Coordinate> GetChildren()
    {
        List<Coordinate> possible_coords = new List<Coordinate>();

        Coordinate bottom_left = new Coordinate(x - 1, z - 1);//BottomLeft
        Coordinate bottom = new Coordinate(x, z - 1);//Bottom
        Coordinate bottom_right = new Coordinate(x + 1, z - 1);//BottomRight
        Coordinate left = new Coordinate(x - 1, z);//Left
        Coordinate right = new Coordinate(x + 1, z);//Right
        Coordinate top_left = new Coordinate(x - 1, z + 1);//TopLeft
        Coordinate top = new Coordinate(x, z + 1);//Top
        Coordinate top_right = new Coordinate(x + 1, z + 1);//TopRight
        
        possible_coords.Add(bottom_left);
        possible_coords.Add(bottom);
        possible_coords.Add(bottom_right);
        possible_coords.Add(left);
        possible_coords.Add(right);
        possible_coords.Add(top_left);
        possible_coords.Add(top);
        possible_coords.Add(top_right);

        for(int i = 0;i < possible_coords.Count;i++)
        {
            /*If the coord marks an obstacle object's location or a 
             coord's values are out of bounds,remove it from the 
             list*/
            if (possible_coords[i].x < 0 || possible_coords[i].z < 0 ||
                possible_coords[i].x >  Map.Instance.num_rects ||
                possible_coords[i].z >  Map.Instance.num_rects ||
                ObstacleCoord.Coordinates.Contains(possible_coords[i])
                || parent == possible_coords[i])
                
            {
                possible_coords.Remove(possible_coords[i]);
                i--;
            }
        }
        /*For manuevering around corners;going diagonal
         around corners isn't possible */
        if (ObstacleCoord.Coordinates.Contains(top))
        {
            if (possible_coords.Contains(top_left))
            {
                possible_coords.Remove(top_left);
            }
            if (possible_coords.Contains(top_right))
            {
                possible_coords.Remove(top_right);
            }
        }
        if (ObstacleCoord.Coordinates.Contains(bottom))
        {
            if(possible_coords.Contains(bottom_left))
            {
                possible_coords.Remove(bottom_left);
            }
            if(possible_coords.Contains(bottom_right))
            {
                possible_coords.Remove(bottom_right);
            }
        }
        return possible_coords;
    }

    public float GetTotalCost()
    {
        if (parent != null)
        {
            return parent.GetTotalCost() + traverse_cost;
        }
        else
        {
            return traverse_cost;
        }
    }

    public int GetNumParents()
    {
        if (parent != null)
        {
            return parent.GetNumParents() + 1;
        }
        else
        {
            return 0;
        }
    }

 
}



public class Map : NetworkBehaviour
{
    public static Map Instance;
    public float num_rects = 10;
    /*Each interval marks a new coordinate point
     on the respective axis.*/
    /*Essentially acts as the
     length of the rectangle*/
    public float interval_x;
    /*Z's act like Y's, for the it is being
         graphed as it's a 2D plane.This interval
     essentially acts as the width of the rectangle*/
    public float interval_z;
    public float min_x;
    
    public float min_z;

    void Awake()
    {
        Instance = this;
        Renderer rend = GetComponent<Renderer>();

        float max_x = rend.bounds.max.x;
        float max_z = rend.bounds.max.z;
        min_x = rend.bounds.min.x;
        min_z = rend.bounds.min.z;
        /*Ranges depict the width and lentgh respectively
         of the imaginary, rectangular plane. */
        float range_z = max_z - min_z;
        float range_x = max_x - min_x;

        interval_x = range_x / num_rects;
        interval_z = range_z / num_rects;

       


    }

    public Coordinate GetPos(Vector3 pos)
    {
        /*The coordinate can be ascertained by dividing the
        position float values by the length and width of
        each rectangle*/
        int coord_x = (int)((pos.x - min_x) / interval_x);
        int coord_z = (int)((pos.z - min_z) / interval_z);
        Coordinate Coord = new Coordinate(coord_x,coord_z);
        return Coord;
    }

    public Vector3 GetCenter(Coordinate coord)
    {
        /*The center will be remniscent of that of an ellipse's.
         The 'x' and 'z' * 1.5f represents centerpoint of the 
         rectangle,while the intervals scale them up. to the
          platform's actual dimensions.*/
        return new Vector3((interval_x * (float)coord.x + min_x),
            11f,
            (interval_z * (float)coord.z + min_z) );
    }


}

