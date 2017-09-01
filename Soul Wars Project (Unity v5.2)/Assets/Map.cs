
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public delegate float Heuristic<S,T>(S s,T t);
public delegate float Heuristic<T>(T t);


public class Coordinate
{
    public uint x;
    public uint z;
    public float traverse_cost;
    
    public enum Status
    {
        Free = 0,
        Hazard = 1
    }

    public Status status = Status.Free;

    private List<Coordinate> possible_coords = new List<Coordinate>();
    public Coordinate parent;
    public Coordinate(uint _x,uint _z,float cost = 0)
    {
        x = _x;
        z = _z;
        traverse_cost = cost;
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
        int iterations = 0;
            List<Coordinate> coords = new List<Coordinate>();
            while (c != null && iterations < 100)
            {
                coords.Add(c);
                c = c.parent;
            iterations++;
            }
        return coords;
      
   }
         
            
    
    public List<Coordinate> GetChildren(Coordinate goal = null)
    {
        possible_coords.Clear();
        /*Coordinate bottom_left = new Coordinate(x - 1, z - 1);//BottomLeft
        Coordinate bottom = new Coordinate(x, z - 1);//Bottom
        Coordinate bottom_right = new Coordinate(x + 1, z - 1);//BottomRight
        Coordinate left = new Coordinate(x - 1, z);//Left
        Coordinate right = new Coordinate(x + 1, z);//Right
        Coordinate top_left = new Coordinate(x - 1, z + 1);//TopLeft
        Coordinate top = new Coordinate(x, z + 1);//Top
        Coordinate top_right = new Coordinate(x + 1, z + 1);//TopRight*/

        Coordinate bottom_left = null;
        Coordinate bottom = null;
        Coordinate bottom_right = null;
        Coordinate left = null;
        Coordinate right = null;
        Coordinate top_left = null;
        Coordinate top = null;
        Coordinate top_right = null;

        bool z_min = z == 0;
        bool z_max = z == Map.Instance.num_rects;
        bool x_min = x == 0;
        bool x_max = x == Map.Instance.num_rects;

        if (!z_min && !x_min )
        {
            bottom_left = Map.Instance.GetPos(x - 1, z - 1);//BottomLeft
            possible_coords.Add(bottom_left);
        }
        if (!z_min)
        {
            bottom = Map.Instance.GetPos(x, z - 1);//Bottom
            possible_coords.Add(bottom);
        }
        if (!z_min && !x_max)
        {
            bottom_right = Map.Instance.GetPos(x + 1, z - 1);//BottomRight
            possible_coords.Add(bottom_right);
        }
        if (!x_min)
        {
            left = Map.Instance.GetPos(x - 1, z);//Left
            possible_coords.Add(left);
        }
        if (!x_max)
        {
            right = Map.Instance.GetPos(x + 1, z);//Right
            possible_coords.Add(right);
        }
        if (!x_min && !z_max)
        {
            top_left = Map.Instance.GetPos(x - 1, z + 1);//TopLeft
            possible_coords.Add(top_left);
        }
        if(!z_max)
        {
            top = Map.Instance.GetPos(x, z + 1);//Top
            possible_coords.Add(top);
        }
        if (!x_max && !z_max)
        {
            top_right = Map.Instance.GetPos(x + 1, z + 1);//TopRight
            possible_coords.Add(top_right);
        }

        if (goal != null && possible_coords.Contains(goal))
        {
            possible_coords.RemoveAll(delegate (Coordinate c)
            {
                return (c != goal);
            });
        }
        else
        {
            for (int i = 0; i < possible_coords.Count; i++)
            {
                /*If the coord marks an obstacle object's location or a 
                 coord's values are out of bounds,remove it from the 
                 list*/

                if (ObstacleCoord.Coordinates.Contains(possible_coords[i])
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
                if (possible_coords.Contains(bottom_left))
                {
                    possible_coords.Remove(bottom_left);
                }
                if (possible_coords.Contains(bottom_right))
                {
                    possible_coords.Remove(bottom_right);
                }
            }
        }
        return possible_coords;
    }

    public float GetTotalCost(int iterations = 0)
    {
         if (parent != null && iterations < 100)
         {
              iterations++;
              return parent.GetTotalCost(iterations) + traverse_cost;
         }
         else
         {
              if(iterations >= 100)
              {
                 // Debug.Log("Overflow : too many parents");
              }
              return traverse_cost;
         }
    }

    public float GetTotalCost(Coordinate coord)
    {
        if (coord != null)
        {
            return traverse_cost + coord.GetTotalCost();
        }
        else
        {
            return traverse_cost;
        }
    }

    public int GetNumParents(int iterations = 0)
    {
        if (parent != null && iterations < 100)
        {
            iterations++;
            return parent.GetNumParents(iterations) + 1;
        }
        else
        {
            if(iterations >= 100)
            {
               // Debug.Log("Overflow : too many parents");
            }
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
    Dictionary<ValueGroup<uint,uint>, Coordinate> Coords = new Dictionary<ValueGroup<uint,uint>, Coordinate>();

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

        for (uint i = 0; i <= num_rects; i++)
        {
            for (uint j = 0; j <= num_rects; j++)
            {
                Coords.Add(new ValueGroup<uint, uint>(i, j), new Coordinate(i, j));
            }
        }


    }


    public Coordinate GetPos(Vector3 pos)
    {
        /*The coordinate can be ascertained by dividing the
        position float values by the length and width of
        each rectangle*/
        uint coord_x = (uint)((pos.x - min_x) / interval_x);
        uint coord_z = (uint)((pos.z - min_z) / interval_z);
        if(coord_x > num_rects)
        {
            coord_x = (uint)num_rects;
        }
        if(coord_z > num_rects)
        {
            coord_z = (uint)num_rects;
        }
        Coordinate Coord = Coords[new ValueGroup<uint, uint>(coord_x, coord_z)];
        return Coord;
    }

    public Coordinate GetPos(uint coord_x, uint coord_z)
    {
        if (coord_x > num_rects)
        {
            coord_x = (uint)num_rects;
        }
        if (coord_z > num_rects)
        {
            coord_z = (uint)num_rects;
        }
        return Coords[new ValueGroup<uint, uint>(coord_x, coord_z)];
    }

    public Vector3 GetCenter(Coordinate coord)
    {
        /*The center will be remniscent of that of an ellipse's.
         The 'x' and 'z' * 1.5f represents centerpoint of the 
         rectangle,while the intervals scale them up. to the
          platform's actual dimensions.*/
        return new Vector3((interval_x * ((float)coord.x + .5f)) + min_x,
            11f,
            (interval_z * ((float)coord.z + .5f)) + min_z );
    }

    


}

