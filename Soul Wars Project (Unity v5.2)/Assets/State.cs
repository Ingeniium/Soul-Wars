using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AIController : GenericController
{
    private abstract partial class ObjectiveState 
    {
        public abstract void SetTarget();
        public abstract bool ShouldFindNewTarget();
        public delegate void TriggerResponse(Collider col);
        public delegate bool EndResponse();
        public TriggerResponse RespondtoAllyUnit;
        public EndResponse EndAllyUnitResponse;
        public AIController Unit;
        public bool immediate_responding = false;
        public bool standby = false;

        public bool LastSpawnNear()
        {
            if (PlayerFollow.Player == null)
            {
                return true;
            }
            else if (SpawnManager.AllySpawnPoints.Count == 1)
            {
                return Vector3.Distance(Unit.ptr.position, SpawnManager.AllySpawnPoints[0].transform.position) <
                    Vector3.Distance(Unit.ptr.position, Unit.Target.transform.position);
            }
            else
            {
                return false;
            }
        }

        public bool PlayerisKilled()
        {
            return (PlayerFollow.Player == null);
        }

        public  virtual bool OutsideJurisdiction()
        {
            return false;
        }


        public void BecomeAgressive(Collider col)
        {
            Unit.Target = col.gameObject;
            Unit.guarding = false;
            Unit.target_focus = true;
        }

        public void AvoidConfrontation(Collider col)
        {
            if (col.gameObject.tag == "Player")
            {
                Unit.StartCoroutine(AvoidConfronting(col));
            }
        }

        IEnumerator AvoidConfronting(Collider col)
        {
            while (Unit.ally_in_range)
            {
                Vector3 vec = (col.gameObject.transform.position - Unit.ptr.position);
                vec = Quaternion.AngleAxis(90,Vector3.up) * vec;
                Unit.prb.AddForce(vec.normalized *(10 - vec.magnitude));
                yield return new WaitForFixedUpdate();
            }
        }
    }


    private partial class AttackAllyUnits : ObjectiveState
    {
       
        public AttackAllyUnits(AIController unit)
        {
            Unit = unit;
            SetTarget();
        }

        public override void SetTarget()
        {
            if (!PlayerisKilled())
            {
                Unit.Target = PlayerFollow.Player;
            }
            else
            {
                standby = true;
            }
        }

        public override bool ShouldFindNewTarget()
        {
            return PlayerisKilled();
        }
    }

    private partial class AttackAllySpawn : ObjectiveState
    {
         public AttackAllySpawn(AIController unit)
        {
            Unit = unit;
            SetTarget();
        }

        public override void SetTarget()
        {
            if (SpawnManager.AllySpawnPoints.Count > 1)
            {

                List<ValueGroup> Distances = new List<ValueGroup>();
                float dist;
                for (int i = 0; i < SpawnManager.AllySpawnPoints.Count; i++)
                {
                    dist = Vector3.Distance(Unit.ptr.position, SpawnManager.AllySpawnPoints[i].transform.position);
                    Distances.Add(new ValueGroup(i, dist));
                }
                Distances.Sort(delegate(ValueGroup lhs, ValueGroup rhs)
                {
                    if (lhs.value > rhs.value)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                });
               Unit.Target = SpawnManager.AllySpawnPoints[Distances[0].index].gameObject;
            }
            else
            {
                if (SpawnManager.AllySpawnPoints.Count == 1)
                {
                    Unit.Target = SpawnManager.AllySpawnPoints[0].gameObject;
                }
                else
                {
                    standby = true;
                }
            }
        }

        public override bool ShouldFindNewTarget()
        {
            return (Unit.Target.layer == 8);
        }
    }

    private partial class GuardLocations : ObjectiveState
    {
        public Vector3 Location;
        private GameObject location_point = new GameObject();
        public Vector3[] patrol_points;
        public float guard_radius;

        public override void SetTarget()
        {
            Unit.guarding = true;        
            Unit.Target = location_point;
        }

        public GuardLocations(AIController unit, Vector3 location, float radius)
        {
            Unit = unit;
            Location = location;
            guard_radius = radius;
            location_point.transform.position = location;
        }

        public GuardLocations(AIController unit, Vector3 location, float radius, Vector3[] Waypoints)
        {
            Unit = unit;
            Location = location;
            guard_radius = radius;
            patrol_points = Waypoints;
        }

        public override bool ShouldFindNewTarget()
        {
            return false;
        }

        public override bool OutsideJurisdiction()
        {
            return (Vector3.Distance(Location, Unit.ptr.position) > guard_radius);
        }

    }

   

}
