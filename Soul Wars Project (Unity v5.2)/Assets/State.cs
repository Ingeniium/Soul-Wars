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
        if(State != null)
        {
            State.ResetHateList();
        }
    }

    public IEnumerator SetState(Type type)
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
                    State = new Guard(this);
                    break;
                }
        }
    }

    private abstract partial class ObjectiveState
    {
        public AIController Unit;
        public abstract void ResetHateList();
        public abstract void UnitAggroReaction(Collider col);

        public bool AffirmTarget(HealthDefence Target)
        {
            /*Checks whether object still exists.Note in UpdateAggro(),the
           info is erased only if it is chosen/kept as the current target.
           In the function,the netId of the object to remove is 
           determined.*/
            if (!Target)
            {
                Unit.UpdateAggro();
                return false;
            }
            /*Also check if object is on the same team or has no HP left.
             In this case,remove their aggro from the list.*/
            if (Target.gameObject.layer == Unit.ptr.gameObject.layer
                || Target.HP <= 0)
            {
                Unit.RemoveAggro(Target.netId);
                return false;
            }
            else
            {
                return true;
            }    
        }


        protected  virtual IEnumerator GenerateGradualThreat(HealthDefence target, NetworkInstanceId ID, int amount = 5)
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
            Unit.Target = null;
            Unit.ClearHateList();
            UpdateSpawnAggro();
        }

        void UpdateSpawnAggro()
        {
            /*Sort from closest to farthest from unit*/
            List<SpawnManager> s = SpawnManager.GetOpponentSpawns(Unit.transform.parent.gameObject.layer);
            s.SortByLeastToGreatDist(Unit.ptr.position);
            int n = s.Count;
            foreach (SpawnManager sp in s)
            {
                /*Insert or Update threat information.Closer spawns are given slightly more threat
                 than farther spawns.*/
                if (sp)
                {
                    Unit.UpdateAggro(n * 10, sp.netId, false);
                    n--;
                }
            }
        }

        public override void UnitAggroReaction(Collider col)
        {
            HealthDefence target = col.gameObject.GetComponentInParent<HealthDefence>();
            if(!target)
            {
                return;
            }
            /*Players will generate 100 aggro automatically upon entering 
             aggro radius,but only if they're not on the list beforehand*/
            if (target.type == HealthDefence.Type.Unit)
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
            else if (target.type == HealthDefence.Type.Spawn_Point)
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
            Unit.ClearHateList();
            Unit.Target = null;
            List<PlayerController> player_list = new List<PlayerController>();
            foreach (uint u in PlayersAlive.Instance.Players)
            {
                if (u != 100)
                {
                    player_list.Add(
                        ClientScene.FindLocalObject(new NetworkInstanceId(u)).
                        GetComponent<PlayerController>());
                }
            }
            /*Sort from closest to farthest from unit*/
            player_list.SortByLeastToGreatDist(Unit.ptr.position);
            int n = player_list.Count;
            foreach (PlayerController p in player_list)
            {
                /*Insert or Update threat information.Closer units are given significantly more threat
                 than farther units.*/
                Unit.UpdateAggro(n * 25, p.netId, false);
                n--;
            }
        }

        public override void UnitAggroReaction(Collider col)
        {
            HealthDefence target = col.gameObject.GetComponent<HealthDefence>();
            /*Players will generate 100 aggro automatically upon entering 
            aggro radius,but only if they're not on the list beforehand.
             Additionally,they will gradually generate threat each second they 
             are in radius.*/
            if (target.type == HealthDefence.Type.Unit)
            {
                if (!Array.Exists(Unit.HateList, delegate (ValueGroup v)
                {
                    return (v.index == (int)target.netId.Value);
                }))
                {
                    Unit.UpdateAggro(100, target.netId, false);
                }
                Unit.StartCoroutine(GenerateGradualThreat(target, target.netId));
            }
            /*Spawn Points will generate 20 aggro automatically upon entering 
            aggro radius,but only if they're not on the list beforehand*/
            if (target.type == HealthDefence.Type.Spawn_Point)
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
                Unit.HateList[i].value += 1000;
            }
        }

        public override void UnitAggroReaction(Collider col)
        {
            HealthDefence target = col.gameObject.GetComponent<HealthDefence>();
            /*Players will be shot at momentarily before advancing to the next 
             spawn.*/
            if (target.type == HealthDefence.Type.Unit)
            {
                int i = 0;
                foreach (Gun gun in Unit.weapons)
                {
                    if (AttackFuncs[Unit.attack_func_indexes[i]](Unit, gun) && !Unit.WillBulletHitObstacle(gun))
                    {
                        Unit.main_gun = gun;
                        Unit.main_gun.Shoot();
                    }
                    i++;
                }
            }

            if (target.type == HealthDefence.Type.Spawn_Point)
            {
                /*SpawnPoints will generate 200 aggro automatically upon entering 
                aggro radius,but only if they're not on the list beforehand*/
                if (target.type == HealthDefence.Type.Unit)
                {
                    if (!Array.Exists(Unit.HateList, delegate (ValueGroup v)
                    {
                        return (v.index == (int)target.netId.Value);
                    }))
                    {
                        Unit.UpdateAggro(200, target.netId, false);
                    }
                }
            }
        }

        public HuntSpawns(AIController AI) : base(AI) { }
    }

    private partial class Guard : ObjectiveState
    {
        /*An Empty call;It will have no threat info after
         death.*/
        public override void ResetHateList() { }
        Vector3 guard_point;

        /*PLayers/Spawns within radius are given more priority
         than those outside.*/
        public override void UnitAggroReaction(Collider col)
        {
            HealthDefence target = col.gameObject.GetComponent<HealthDefence>();
            Unit.StartCoroutine(GenerateGradualThreat(target, target.netId, 50));
        }

        public Guard(AIController AI) : base(AI)
        {
            guard_point = Unit.ptr.position;
            Unit.StartCoroutine(LimitDistFromGuardPoint());
        }

        protected override IEnumerator GenerateGradualThreat(HealthDefence target, NetworkInstanceId ID, int amount = 5)
        {
            /*This makes it such that the closer an enemy is from the guard_point,the 
             more aggro it has for it.*/
            while (AffirmTarget(target)
               && Math.Abs(
                   Vector3.Distance(guard_point, target.transform.position)) - .75f
               < Unit.enemy_attack_detection.radius)
            {
                float dist = Math.Abs(
                    Vector3.Distance(guard_point, target.transform.position));
                float new_amount = Unit.enemy_attack_detection.radius / dist * amount;
                Unit.UpdateAggro((int)new_amount, ID, false);
                yield return new WaitForSeconds(1);
            }
        }

        IEnumerator LimitDistFromGuardPoint()
        {
            while(Unit)
            {
                yield return new WaitForFixedUpdate();
                if(Math.Abs(
                    Vector3.Distance(Unit.ptr.position,guard_point))
                    > 7.5f)
                {
                    Unit.move_dir = (guard_point - Unit.ptr.position).normalized;
                }
            }
        }
    }







}
