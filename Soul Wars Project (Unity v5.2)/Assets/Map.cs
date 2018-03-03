using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Coordinate
{
    public uint x;
    public uint z;
    public float traverse_cost;

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

    public List<ValueGroup<ValueGroup<uint, int>, float>> hazard_layers = new List<ValueGroup<ValueGroup<uint, int>, float>>();
    public Status status = Status.Safe;
    public static bool operator ==(Coordinate lhs, Coordinate rhs)
    {
        if (Equals(lhs, null) || Equals(rhs, null))
        {
            return Equals(lhs, rhs);
        }
        else
        {
            return lhs.Equals(rhs);
        }
    }

    public static bool operator !=(Coordinate lhs, Coordinate rhs)
    {
        return !(lhs == rhs);

    }

    public override bool Equals(object o)
    {
        Coordinate rhs = o as Coordinate;
        if(rhs == null)
        {
            return false;
        }
        else
        {
            return rhs.x == x && rhs.z == z;
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

    /*Gets the hazard cost of the coordinates.Hazard
      cost is based on the damage of bullets occupying it
      that aren't the same layer as the arg safe_layer*/
    public float GetHazardCost(int safe_layer)
    {
        float hazard_cost = 0;
        foreach(ValueGroup<ValueGroup<uint,int>,float> v in hazard_layers)
        {
            int layer = v.index.value;
            if(layer != safe_layer)
            {
                hazard_cost += v.value;
            }
        }
        return hazard_cost;
    }

    /*Returns whether there's any "occupying" bullets that aren't
      of the safe layer arg,if there's any bullets "occupying" it
      in the first place*/
    public bool isHazardous(int safe_layer)
    {
        return status == Status.Hazard &&
        hazard_layers.Exists(delegate (ValueGroup<ValueGroup<uint,int>,float> v)
        {
            int layer = v.index.value;
            return (layer != safe_layer);
        });
    }

    /*Gets the total cost of the coord.Returned float is affected by whether the algorithm accounts for
      the hazard cost of the bullet(with given safe layer arg).Iterations arg is meant to exit the algorithm
      for whenever there's too much parents(caps at 100)*/
    public float GetTotalCost(int safe_layer = 0,bool account_for_hazards = false,int iterations = 0,Coordinate input_coord = null)
    {
        float hazard_cost = 0;
        float border_cost = 0;
        if(account_for_hazards && isHazardous(safe_layer))
        {
            hazard_cost = GetHazardCost(safe_layer);
        }
        if(Map.Instance.OnMapBorder(this))
        {
            border_cost = 1000;
        }
        if(input_coord != null)
        {
            return traverse_cost + border_cost + input_coord.GetTotalCost(safe_layer, account_for_hazards);
        }
        else if (parent != null && iterations < 100)
        {
            iterations++;         
            return parent.GetTotalCost(safe_layer, account_for_hazards, iterations) + traverse_cost 
                + border_cost + hazard_cost;                  
        }
        else
        {
            return traverse_cost + border_cost + hazard_cost;
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

    public bool OnMapBorder(Coordinate coord)
    {
        return coord.x == num_rects || coord.z == num_rects
            || coord.x == 0 || coord.z == 0;
    }

    public Coordinate GetPos(Vector3 pos)
    {
        /*The coordinate can be ascertained by dividing the
        position float values by the length and width of
        each rectangle*/
        uint coord_x = (uint)((pos.x - min_x) / interval_x);
        uint coord_z = (uint)((pos.z - min_z) / interval_z);
        const uint NEGATIVE_OUTBOUND = 1000;
        if (coord_x > NEGATIVE_OUTBOUND)
        {
            coord_x = 0;
        }
        else if (coord_x > num_rects)
        {
            coord_x = num_rects;
        }
        if (coord_z > NEGATIVE_OUTBOUND)
        {
            coord_z = 0;
        }
        else if (coord_z > num_rects)
        {
            coord_z = num_rects;
        }
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
        const uint NEGATIVE_OUTBOUND = 1000;
        if (coord_x > NEGATIVE_OUTBOUND)
        {
            coord_x = 0;
        }
        else if (coord_x > num_rects)
        {
            coord_x = num_rects;
        }
        if (coord_z > NEGATIVE_OUTBOUND)
        {
            coord_z = 0;
        }
        else if (coord_z > num_rects)
        {
            coord_z = num_rects;
        }
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
        if (coord != null)
        {
            return new Vector3((interval_x * ((float)coord.x + .5f)) + min_x,
                11f,
                (interval_z * ((float)coord.z + .5f)) + min_z);
        }
        else
        {
            return Vector3.zero;
        }
    }

    public void RemoveCoord(ValueGroup<uint, uint> key)
    {
        if(Coords.ContainsKey(key))
        {
            Coords.Remove(key);
        }
    }




}
