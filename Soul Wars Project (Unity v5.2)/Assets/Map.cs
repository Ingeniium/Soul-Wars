using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Coordinate
{
    public uint x;
    public uint z;
    public float traverse_cost;
    public float hazard_cost;

    private List<Coordinate> possible_coords = new List<Coordinate>();
    public Coordinate parent;
    public Coordinate(uint _x, uint _z)
    {
        x = _x;
        z = _z;
    }

     public enum Status
    {
        Safe = 0,
        Hazard = 1
    }

    public Status status = Status.Safe;
    public static bool operator ==(Coordinate lhs, Coordinate rhs)
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

        Coordinate bottom_left = Map.Instance.GetPos(x - 1, z - 1);
        Coordinate bottom = Map.Instance.GetPos(x, z - 1);
        Coordinate bottom_right = Map.Instance.GetPos(x + 1, z - 1); 
        Coordinate left = Map.Instance.GetPos(x - 1, z); 
        Coordinate right = Map.Instance.GetPos(x + 1, z);
        Coordinate top_left = Map.Instance.GetPos(x - 1, z + 1);
        Coordinate top = Map.Instance.GetPos(x, z + 1);
        Coordinate top_right = Map.Instance.GetPos(x + 1, z + 1); ;
    
  
        possible_coords.Add(bottom_left);
        possible_coords.Add(bottom);                            
        possible_coords.Add(bottom_right);      
        possible_coords.Add(left);       
        possible_coords.Add(right);       
        possible_coords.Add(top_left);     
        possible_coords.Add(top);      
        possible_coords.Add(top_right);
       
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

                if (possible_coords[i] == null
                   || parent == possible_coords[i])

                {
                    possible_coords.Remove(possible_coords[i]);
                    i--;
                }
            }
            /*For manuevering around corners;going diagonal
             around corners isn't possible */
            if (!possible_coords.Contains(top))
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
            if (!possible_coords.Contains(bottom))
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

    public float GetTotalCost(bool account_for_hazards = false,int iterations = 0)
    {
        if (parent != null && iterations < 100)
        {
            iterations++;
            if (account_for_hazards)
            {
                return parent.GetTotalCost(account_for_hazards, iterations) + traverse_cost + hazard_cost;
            }
            else
            {
                return parent.GetTotalCost(account_for_hazards,iterations) + traverse_cost;
            }
        }
        else
        {
            if (iterations >= 100)
            {
                 Debug.Log("Overflow : too many parents");
            }
            if (account_for_hazards)
            {
                return traverse_cost + hazard_cost;
            }
            else
            {
                return traverse_cost;
            }
        }
    }

    public float GetTotalCost(Coordinate coord, bool account_for_hazards = false)
    {
        if (coord != null)
        {
            return traverse_cost + coord.GetTotalCost(account_for_hazards);
        }
        else
        {
            if (account_for_hazards)
            {
                return traverse_cost + hazard_cost;
            }
            else
            {
                return traverse_cost;
            }
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
            if (iterations >= 100)
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
    public uint num_rects = 20;//Dimensions for coordinate plane are num_rects * num_rects
    /*Each interval marks a new coordinate point
     on the respective axis.*/
    /*Essentially acts as the
    length of the rectangle*/
    public float interval_x
    {
        get { return _interval_x; }
        private set
        {
            _interval_x = value;
        }
    }
    private float _interval_x;
    /*Z's act like Y's, for the it is being
         graphed as it's a 2D plane.This interval
     essentially acts as the width of the rectangle*/
    public float interval_z
    {
        get { return _interval_z; }
        private set
        {
            _interval_z = value;
        }
    }
    private float _interval_z;
    public float min_x
    {
        get { return _min_x; }
        private set
        {
            _min_x = value;
        }
    }
    private float _min_x;
    public float min_z
    {
        get { return _min_z; }
        private set
        {
            _min_z = value;
        }
    }
    private float _min_z;
    private Dictionary<ValueGroup<uint, uint>, Coordinate> Coords = new Dictionary<ValueGroup<uint, uint>, Coordinate>();

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
        ValueGroup<uint, uint> Key = new ValueGroup<uint, uint>(coord_x, coord_z);
        if (Coords.ContainsKey(Key))
        {
            return Coords[Key];
        }
        else
        {
            return null;
        }
    }

    public Coordinate GetPos(uint coord_x, uint coord_z)
    {
        ValueGroup<uint, uint> Key = new ValueGroup<uint, uint>(coord_x, coord_z);
        if (Coords.ContainsKey(Key))
        {
            return Coords[Key];
        }
        else
        {
            return null;
        }
    }

    public Vector3 GetCenter(Coordinate coord)
    {
        /*The center will be remniscent of that of an ellipse's.
         The 'x' and 'z' * 1.5f represents centerpoint of the 
         rectangle,while the intervals scale them up. to the
          platform's actual dimensions.*/
        return new Vector3((interval_x * ((float)coord.x + .5f)) + min_x,
            11f,
            (interval_z * ((float)coord.z + .5f)) + min_z);
    }

    public void RemoveCoord(ValueGroup<uint, uint> key)
    {
        if(Coords.ContainsKey(key))
        {
            Coords.Remove(key);
        }
    }




}
