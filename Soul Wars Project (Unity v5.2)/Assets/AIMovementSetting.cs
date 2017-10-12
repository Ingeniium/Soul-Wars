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

    ValueGroup<Coordinate,Coordinate> Charge()
    {
        return new ValueGroup<Coordinate, Coordinate>(
            Map.Instance.GetPos(ptr.position),
            Map.Instance.GetPos(Target.transform.position));
    }

    ValueGroup<Coordinate,Coordinate> Sneak()
    {
        Coordinate targ_coord = Map.Instance.GetPos(Target.transform.position);
        if(targ_coord == null)
        {
            targ_coord = prev_end_coord;
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
                    if (coord != null && coord.status == Coordinate.Status.Safe )
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
                    if (coord != null && coord.status == Coordinate.Status.Safe)
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
                    if (coord != null && coord.status == Coordinate.Status.Safe)
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
                    if (coord != null && coord.status == Coordinate.Status.Safe)
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

    IEnumerator Travel()
    {
        while (this)
        {
            ValueGroup<Coordinate, Coordinate> travel_coords;
            /*The wait for end of frame limits each run of the pathfinding algorithm
             to one execution per frame.The time_until_next_pathfind variable
             serves to spread out its execution between many AI,thereby
             hopefully increasing overall game performance by reducing how much times
             it executes in a single frame.*/
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(time_until_next_pathfind);
            if (Target)
            {
                travel_coords = MovementFuncs[movement_func_index](this);
                Path = GetPath(
                   travel_coords.index,
                   travel_coords.value);
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
        }
    }

    Coordinate GetPath(Coordinate start, Coordinate end)
    {
        List<Coordinate> visited = new List<Coordinate>();
        if (Target)
        {
            Priority_Queue.SimplePriorityQueue<Coordinate> queue = new Priority_Queue.SimplePriorityQueue<Coordinate>();
            if (start == null)
            {
                start = prev_start_coord;
            }
            start.parent = null;
            if (end == null)
            {
                end = prev_end_coord;
            }
            float tstart = Time.realtimeSinceStartup;
            prev_start_coord = start;
            prev_end_coord = end;
            queue.Enqueue(start, GetCoordinateDistFromTarget(start));
            if(end.status == Coordinate.Status.Hazard)
            {
                Debug.Log("Unsafe!");
            }
            while (Time.realtimeSinceStartup < tstart + .01f)
            {
                start = queue.Dequeue();
                if (start == end)
                {
                    /* Debug.Log(Time.realtimeSinceStartup - tstart + " seconds : " +
                          queue.Count + " end routes considered : " +
                          start.GetNumParents() + " parents.");*/
                    return start;
                }
                foreach (Coordinate coord in start.GetChildren())
                { 
                    coord.traverse_cost = GetCoordinateDistFromTarget(coord);
                    if (!visited.Contains(coord))
                    {
                        coord.parent = start;
                        queue.Enqueue(coord, coord.GetTotalCost(can_dodge));
                        visited.Add(coord);
                    }
                    else if (queue.Contains(coord) && coord.GetTotalCost(start,can_dodge) < queue.GetPriority(coord))
                    {
                        coord.parent = start;
                        queue.UpdatePriority(coord, coord.GetTotalCost(can_dodge));
                    }


                }
                if (queue.Count != 0)
                {
                    start = queue.First;
                }
                else
                {
                    return null;
                }
            }

        }
        return null ;
    }



}