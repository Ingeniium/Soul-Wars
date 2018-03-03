using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public partial class AIController : GenericController
{
    public int movement_func_index = 0;
    private Coordinate Path;//The path to take.
    private Coordinate prev_start_coord;//Used in case the next start coord is null.(See
    private Coordinate prev_end_coord;//Used in case the next end coord is null.
    public bool can_dodge;
    private float minimal_distance = 1.5f;

    public enum MovementMode
    {
        Charge = 0,
        Sneak = 1
    }

    private static Dictionary<int, Func<AIController, ValueGroup<Coordinate, Coordinate>>> MovementFuncs = new Dictionary<int, Func<AIController, ValueGroup<Coordinate, Coordinate>>>()
    {
        {0,Charge },
        {1,Sneak }
    };

    private static ValueGroup<Coordinate,Coordinate> Charge(AIController AI)
    {
        return AI.Charge();
    }

    private static ValueGroup<Coordinate,Coordinate> Sneak(AIController AI)
    {
        return AI.Sneak();
    }

    /*Gets a coordinate that simply has minimal distance - i 
      added to the coordinates z value.It iterates until it gets 
      an existing coord to return.*/
    ValueGroup<Coordinate,float> GetMinimalDistStarterCoord(Coordinate coord)
    {
        for(float i = minimal_distance;i > minimal_distance * -1;i -= .5f)
        {
            Vector3 pos = Map.Instance.GetCenter(coord);
            pos = new Vector3(pos.x, pos.y, pos.z + i);
            Coordinate new_coord = Map.Instance.GetPos(pos);
            //Coordinate new_coord = Map.Instance.GetPos(coord.x, coord.z + (uint)i);
            if(new_coord != null)
            {
                return new ValueGroup<Coordinate, float>(new_coord, i);
            }
        }
        return new ValueGroup<Coordinate, float>(coord, minimal_distance) ;
    }


    /*Gets the closest coord whose center is minimum distance away
      from the target coordinate.Note that coordinate target doesn't
      have to the coordinate of the AI's current target*/    
    Coordinate GetClosestCoordToTargetCoord(Coordinate target)
    {
        if(target == null)
        {
            return null;
        }
        const float INTERVAL_THETA = 20;
        const float FULL_CIRCLE = 360;
        ValueGroup<Coordinate, float> v = GetMinimalDistStarterCoord(target);
        Coordinate goal = v.index;
        Coordinate current_coord = Map.Instance.GetPos(ptr.position);
        Vector3 ORIGINAL_POS = Map.Instance.GetCenter(goal);
        float dist = v.value;
        float goal_cost = 0;
        if(goal.isHazardous(ptr.gameObject.layer))
        {
            goal_cost = goal.GetHazardCost(ptr.gameObject.layer);
           // Debug.Log("Hazardous");
        }
        float goal_distance = Math.Abs(
            Vector3.Distance(
                Map.Instance.GetCenter(goal),
                Map.Instance.GetCenter(current_coord)));
        for(float degrees = 10;degrees < FULL_CIRCLE;degrees += INTERVAL_THETA )
        {
            Vector3 pos = Quaternion.AngleAxis(degrees, Vector3.up) * ORIGINAL_POS;
            Coordinate coord = Map.Instance.GetPos(pos);
            if (coord != null)
            {
                float cost = 0;
                if(coord.isHazardous(ptr.gameObject.layer))
                {
                    cost = coord.GetHazardCost(ptr.gameObject.layer);
                }
                float distance = Math.Abs(
               Vector3.Distance(
                   Map.Instance.GetCenter(coord),
                   Map.Instance.GetCenter(current_coord)));
                
                if (distance + cost < goal_distance + goal_cost)
                {
                    goal = coord;
                    goal_distance = distance;
                    goal_cost = cost;
                }
            }
        }
        return goal;
    }


    ValueGroup<Coordinate, Coordinate> Charge()
    {
        return new ValueGroup<Coordinate, Coordinate>(
            Map.Instance.GetPos(ptr.position),
            GetClosestCoordToTargetCoord(
                Map.Instance.GetPos(Target.transform.position)));
    }

    ValueGroup<Coordinate,Coordinate> Sneak()
    {
        Coordinate targ_coord = Map.Instance.GetPos(Target.transform.position);
        if(targ_coord == null)
        {
            if (prev_end_coord != null)
            {
                targ_coord = prev_end_coord;
            }
            else
            {
                return new ValueGroup<Coordinate, Coordinate>(null, null);
            }
        }
        float ptr_angle = Math.Abs(ptr.rotation.eulerAngles.y);
        float targ_angle = Math.Abs(Target.transform.rotation.eulerAngles.y);
        float angle = Math.Abs(ptr_angle + targ_angle);
        uint divisor = 5;
        if (angle > 310 || angle < 40)
        {
            if (ptr.position.x > Target.transform.position.x)
            {
                for (uint i = Map.Instance.num_rects / divisor; i > 0; i--)
                {
                    Coordinate coord = Map.Instance.GetPos(targ_coord.x + i, targ_coord.z);
                    if (coord != null && (coord.status == Coordinate.Status.Safe || !coord.isHazardous(transform.parent.gameObject.layer))  )
                    {
                        return new ValueGroup<Coordinate, Coordinate>(
                            Map.Instance.GetPos(ptr.position),
                           coord);
                    }
                }
            }
            else
            {
                for (uint i = Map.Instance.num_rects / divisor; i > 0; i--)
                {
                    Coordinate coord = Map.Instance.GetPos(targ_coord.x - i, targ_coord.z);
                    if (coord != null && (coord.status == Coordinate.Status.Safe || !coord.isHazardous(transform.parent.gameObject.layer)))
                    { 
                        return new ValueGroup<Coordinate, Coordinate>(
                            Map.Instance.GetPos(ptr.position),
                            coord);
                    }
                }
            }
        }
        else
        {
            if (ptr.position.z > Target.transform.position.z)
            {
                for (uint i = Map.Instance.num_rects / divisor; i > 0; i--)
                {
                    Coordinate coord = Map.Instance.GetPos(targ_coord.x, targ_coord.z + i);
                    if (coord != null && (coord.status == Coordinate.Status.Safe || !coord.isHazardous(transform.parent.gameObject.layer))    )
                    {
                        return new ValueGroup<Coordinate, Coordinate>(
                            Map.Instance.GetPos(ptr.position),
                            coord);
                    }
                }
            }
            else
            {
                for (uint i = Map.Instance.num_rects / divisor; i > 0; i--)
                {
                    Coordinate coord = Map.Instance.GetPos(targ_coord.x, targ_coord.z - i);
                    if (coord != null && (coord.status == Coordinate.Status.Safe || !coord.isHazardous(transform.parent.gameObject.layer)))
                    {
                        return new ValueGroup<Coordinate, Coordinate>(
                            Map.Instance.GetPos(ptr.position),
                            Map.Instance.GetPos(targ_coord.x, targ_coord.z - i));
                    }
                }
            }
        }
        return new ValueGroup<Coordinate, Coordinate>(
           Map.Instance.GetPos(ptr.position),
           targ_coord);

    }

    float GetCoordinateDistFromTarget(Coordinate coord)
    {
        if (Target)
        {
            return Math.Abs(
                Vector3.Distance(Map.Instance.GetCenter(coord), Target.transform.position));
        }
        else
        {
            return 0;
        }
    }

    IEnumerator GetPathCoro(Coordinate start, Coordinate end)
    {
        List<Coordinate> visited = new List<Coordinate>();
        if (Target)
        {
            Priority_Queue.SimplePriorityQueue<Coordinate> queue = new Priority_Queue.SimplePriorityQueue<Coordinate>();
            if (start == null)
            {
                start = prev_start_coord;
            }
            if (start != null)
            {
                start.parent = null;
            }
            if (end == null)
            {
                end = prev_end_coord;
            }
            if (start == null || end == null)
            {
                yield break;
            }
            float tstart = Time.realtimeSinceStartup;
            prev_start_coord = start;
            prev_end_coord = end;
            queue.Enqueue(start, GetCoordinateDistFromTarget(start));
            while (Path == null)
            {
                start = queue.Dequeue();
                if (start == end)
                {
                  /*  Debug.Log(Time.realtimeSinceStartup - tstart + " seconds : " +
                         queue.Count + " end routes considered : " +
                         start.GetNumParents() + " parents.");              */
                    Path = start;
                    yield break;
                }
                if(Time.realtimeSinceStartup > tstart + .01f || index != go_index)
                {
                    yield return new WaitForEndOfFrame();
                    tstart = Time.realtimeSinceStartup;
                }
                int safe_layer = ptr.gameObject.layer;
                foreach (Coordinate coord in start.GetChildren())
                {
                    coord.traverse_cost = GetCoordinateDistFromTarget(coord);
                    if (!visited.Contains(coord))
                    {
                        coord.parent = start;
                        queue.Enqueue(coord, coord.GetTotalCost(safe_layer, can_dodge));
                        visited.Add(coord);
                    }
                    else if (queue.Contains(coord) && coord.GetTotalCost(safe_layer, can_dodge,0,start) < queue.GetPriority(coord))
                    {
                        coord.parent = start;
                        queue.UpdatePriority(coord, coord.GetTotalCost(safe_layer, can_dodge));
                    }
                }
                if (queue.Count != 0)
                {
                    start = queue.First;
                }
                else
                {
                    break;
                }
            }

        }
        yield return null;
    }

    IEnumerator Travel2()
    {
        //yield return new WaitForSecondsRealtime(time_until_next_pathfind);
        while (this)
        {
            ValueGroup<Coordinate, Coordinate> travel_coords;
            if (Target)
            {
                Path = null;
                travel_coords = MovementFuncs[movement_func_index](this);
                StartCoroutine(GetPathCoro(travel_coords.index, travel_coords.value));
                while(Path == null)
                {
                    yield return new WaitForEndOfFrame();
                }
                if (Path != null)
                {
                    List<Coordinate> list = Path.GetParents();
                    if (list.Count > 1 && list[list.Count - 2] != null)
                    {
                        move_dir = Map.Instance.GetCenter(list[list.Count - 2]) - ptr.position;
                    }
                    else
                    {
                        move_dir = Vector3.zero;
                    }
                }
            }
            go_index++;
            if(go_index == PlayersAlive.Instance.Units.Count)
            {
                go_index = 0;
            }
            yield return new WaitForEndOfFrame();
        }
    }


}