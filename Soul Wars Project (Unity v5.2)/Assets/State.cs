using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public partial class AIController : GenericController
{
    public enum Type
    {
        Conquer = 0,
        HuntPlayers = 1,
        HuntSpawn = 2,
        Guard = 3,
    }

    public void ResetHateList()
    {
        if (State != null)
        {
            State.ResetHateList();
        }
    }

    public IEnumerator SetState(Type type, Vector3 vec, float dist = -1)
    {
        List<SpawnManager> oppenent_spawns = SpawnManager.GetOpponentSpawns(transform.parent.gameObject.layer);
        while (oppenent_spawns.Count == 0)
        {
            yield return new WaitForEndOfFrame();
        }
        switch (type)
        {
            case Type.Conquer:
                {
                    State = new Conquer(this);
                    break;
                }
            case Type.HuntPlayers:
                {
                    State = new HuntPlayers(this);
                    break;
                }
            case Type.HuntSpawn:
                {
                    State = new HuntSpawns(this);
                    break;
                }
            case Type.Guard:
                {
                    if (vec == Vector3.zero)
                    {
                        State = new Guard(this, dist);
                    }
                    else
                    {
                        State = new Guard(this, vec, dist);
                    }
                    break;
                }
        }
    }

    private abstract partial class ObjectiveState
    {
        public AIController Unit;
        public abstract void UnitAggroReaction(Collider col);
        int idle_count = 0;

        public virtual void ResetHateList()
        {
            //Debug.Log("Resetting");
            Unit.ClearHateList();
            Unit.Target = null;
        }

        public bool AffirmTarget(HealthDefence Target)
        {
            bool result;
            /*Checks whether object still exists.Note in UpdateAggro(),the
           info is erased only if it is chosen/kept as the current target.
           In the function,the netId of the object to remove is 
           determined.*/
            if (!Target)
            {
                idle_count++;
                Unit.UpdateAggro();
                result = false;
            }
            /*Also check if object is on the same team or has no HP left.
             In this case,remove their aggro from the list.*/
            else if (Target.gameObject.layer == Unit.ptr.gameObject.layer
                || Target.HP <= 0)
            {
                idle_count++;
                Unit.RemoveAggro(Target.netId);
                result = false;
            }
            else
            {
                idle_count = 0;
                result = true;
            }
            /*Prevents idling if there is a desireable target not within the hatelist
             * while the hatelist has no suitable targets.*/
            if (idle_count >= Unit.HateList.Length)
            {
                ResetHateList();
            }
            return result;
        }

        protected virtual IEnumerator GenerateGradualThreat(HealthDefence target, NetworkInstanceId ID, int amount = 5)
        {
            /*Interestingly enough,Unity records that the distance is actually GREATER than the actual radius 
             whenever spawns enter the radius.Hence,w/o a subtraction of atleast ~.6f,the coroutine will never
             really function how it's supposed to.*/
            while (AffirmTarget(target)
                && Vector3.Distance(Unit.ptr.position, target.transform.position) - .75f
                < Unit.enemy_attack_detection.radius)
            {
                Unit.UpdateAggro(amount, ID, false);
                yield return new WaitForSeconds(1);
            }
        }

        /*Generate target's threat towards this unit each second based on their distance from the unit,
         provided that the target is within aggro radius.The closer a target is,the more aggro 
         is generated.*/
        protected IEnumerator GenerateGradualDistanceBasedThreat(HealthDefence target, Vector3 pos, float multiplier)
        {
            //Debug.Log("Generating dist aggro...");
            while (AffirmTarget(target)
               && Math.Abs(
                   Vector3.Distance(pos, target.transform.position)) - .75f
               < Unit.enemy_attack_detection.radius)
            {

                float dist = Math.Abs(
                    Vector3.Distance(pos, target.transform.position));
                float new_amount = Unit.enemy_attack_detection.radius / dist * multiplier;
                // Debug.Log((int)new_amount);
                Unit.UpdateAggro((int)new_amount, target.netId, false);
                yield return new WaitForSeconds(1);
            }
        }

        public ObjectiveState(AIController AI)
        {
            Unit = AI;
            ResetHateList();
        }
    }

    private partial class Conquer : ObjectiveState
    {
        public override void ResetHateList()
        {
            base.ResetHateList();
            UpdateSpawnAggro();
        }

        void UpdateSpawnAggro()
        {
            /*Sort from closest to farthest from unit*/
            List<SpawnManager> s = SpawnManager.GetOpponentSpawns(Unit.ptr.gameObject.layer);
            s.SortByLeastToGreatDist(Unit.ptr.position);
            int n = s.Count;
            foreach (SpawnManager sp in s)
            {
                /*Insert or Update threat information.Closer spawns are given slightly more threat
                 than farther spawns.*/
                if (sp)
                {
                    Unit.UpdateAggro(n * 10, sp.netId, false);
                    Debug.Log(n * 10);
                    n--;
                }
            }
        }

        public override void UnitAggroReaction(Collider col)
        {
            HealthDefence target = col.gameObject.GetComponentInParent<HealthDefence>();
            if (!target)
            {
                return;
            }
            /*Players will generate 100 aggro automatically upon entering 
             aggro radius,but only if they're not on the list beforehand*/
            if (target is UnitHealthDefence)
            {
                if (!Array.Exists(Unit.HateList, delegate (ValueGroup v)
                  {
                      return (v.index == (int)target.netId.Value);
                  }))
                {
                    Unit.UpdateAggro(100, target.netId, false);
                }
            }
            /*A Spawn Point in the radius gradually gain threat at a rate of
             10 units per second*/
            else if (target is SpawnPointHealthDefence)
            {
                Unit.StartCoroutine(GenerateGradualThreat(target, target.netId, 20));
            }

        }

        public Conquer(AIController AI) : base(AI) { }



    }

    private partial class HuntPlayers : ObjectiveState
    {
        public override void ResetHateList()
        {
            base.ResetHateList();
            List<GenericController> opponents = new List<GenericController>();
            foreach (AIController AI in PlayersAlive.Instance.Units)
            {
                if (AI.ptr.gameObject.layer != Unit.ptr.gameObject.layer)
                {
                    opponents.Add(AI);
                }
            }

            foreach (PlayerController player in PlayerController.players)
            {
                if (player && player.gameObject.layer != Unit.ptr.gameObject.layer)
                {
                    opponents.Add(player);
                }
            }

            opponents.SortByLeastToGreatDist(Unit.ptr.position);
            for (int i = 0; i < opponents.Count; i++)
            {
                int aggro = (opponents.Count - i) * 100;

                Unit.UpdateAggro(aggro, opponents[i].netId, false);
            }
        }

        public override void UnitAggroReaction(Collider col)
        {
            HealthDefence target = col.gameObject.GetComponent<HealthDefence>();
            if (!target)
            {
                return;
            }
            /*Opponent Units will generate aggro based on their distance from this
              unit.*/
            if (target is UnitHealthDefence)
            {
                Unit.UpdateAggro(10, target.netId, false);
                Unit.StartCoroutine(GenerateGradualDistanceBasedThreat(
                    target,
                    Unit.ptr.position,
                    20));
            }
            /*Spawn Points will generate 20 aggro automatically upon entering 
            aggro radius,but only if they're not on the list beforehand*/
            if (target is SpawnPointHealthDefence)
            {
                if (!Array.Exists(Unit.HateList, delegate (ValueGroup v)
                {
                    return (v.index == (int)target.netId.Value);
                }))
                {
                    Unit.UpdateAggro(20, target.netId, false);
                }
            }
        }

        public HuntPlayers(AIController AI) : base(AI) { }


    }


    private partial class HuntSpawns : Conquer
    {

        public override void ResetHateList()
        {
            base.ResetHateList();
            /*Additon done so that they focus only getting
             * spawn points*/
            for (int i = 0; i < Unit.HateList.Length; i++)
            {
                if (Unit.HateList[i] != NOT_SET)
                {
                    Unit.HateList[i].value += 1000;
                }
            }
        }

        public override void UnitAggroReaction(Collider col)
        {
            HealthDefence target = col.gameObject.GetComponent<HealthDefence>();

            if (target is SpawnPointHealthDefence)
            {
                /*SpawnPoints will generate 200 aggro automatically upon entering 
                aggro radius,but only if they're not on the list beforehand*/
                if (!Array.Exists(Unit.HateList, delegate (ValueGroup v)
                  {
                      return (v.index == (int)target.netId.Value);
                  }))
                {
                    Unit.UpdateAggro(200, target.netId, false);
                }

            }
        }

        public HuntSpawns(AIController AI) : base(AI) { }
    }

    private partial class Guard : ObjectiveState
    {
        Vector3 guard_point;
        float guard_dist;

        /*PLayers/Spawns within radius are given more priority
         than those outside.*/
        public override void UnitAggroReaction(Collider col)
        {
            HealthDefence target = col.gameObject.GetComponent<HealthDefence>();
            ///Debug.Log("Aggro Reaction : " + col);
            Unit.UpdateAggro(10, target.netId, false);
            Unit.StartCoroutine(GenerateGradualDistanceBasedThreat(
                target,
                guard_point,
                50));
        }

        public Guard(AIController AI, float dist = -1) : base(AI)
        {
            guard_point = Unit.ptr.position;
            guard_dist = dist;
            Unit.StartCoroutine(LimitDistFromGuardPoint());
        }

        public Guard(AIController AI, Vector3 pos, float dist = 0) : base(AI)
        {
            guard_point = pos;
            guard_dist = dist;
            Unit.StartCoroutine(LimitDistFromGuardPoint());
        }

        IEnumerator LimitDistFromGuardPoint()
        {
            if (guard_dist == -1)
            {
                guard_dist = Unit.enemy_attack_detection.radius * .75f;
            }
            while (Unit)
            {
                yield return new WaitForFixedUpdate();
                if (Math.Abs(
                    Vector3.Distance(Unit.ptr.position, guard_point))
                    > guard_dist)
                {
                    Unit.move_dir = (guard_point - Unit.ptr.position).normalized;
                }
            }
        }
    }







}
