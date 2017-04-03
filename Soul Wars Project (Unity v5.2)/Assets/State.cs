using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public partial class AIController : GenericController
{
    private abstract partial class ObjectiveState
    {
        public AIController Unit;
        public abstract void ResetHateList();
        public abstract void UnitAggroReaction(Collider col);
        public void AffirmTarget(HealthDefence Target)
        {
            /*Checks whether object still exists.Note in UpdateAggro(),the
             info is erased only if it is chosen/kept as the current target.
             In the function,the netId of the object to remove is 
             determined.*/
            if (!Target)
            {
                Unit.UpdateAggro();
                return;
            }
            /*If target is an ally player,Check if he is dead.*/
            else if (Target.type == HealthDefence.Type.Unit)
            {
                if (Target.HP <= 0)
                {
                    Unit.RemoveAggro(Target.netId);
                }
            }
            /*If a spawn,check if its captured by comparing its collision
             layer to that of the default enemy unit one */
            else if (Target.type == HealthDefence.Type.Spawn_Point)
            {
                if (Target.gameObject.layer == 8)
                {
                    Unit.RemoveAggro(Target.netId);
                }
            }

        }

        protected IEnumerator GenerateGradualThreat(Transform tr, NetworkInstanceId ID, int amount = 5)
        {
            while (tr != null && Vector3.Distance(Unit.ptr.position, tr.position) < Unit.enemy_attack_detection.radius)
            {
                Unit.UpdateAggro(amount, ID, false);
                yield return new WaitForSeconds(1);
            }
        }

        public ObjectiveState(AIController AI)
        {
            Unit = AI;
        }
    }

    private partial class Conquer : ObjectiveState
    {
        public override void ResetHateList()        {
            UpdateSpawnAggro();        }

        void UpdateSpawnAggro()
        {
            List<ValueGroup> Distances = new List<ValueGroup>();
            int i = 0;
            foreach (SpawnManager s in SpawnManager.AllySpawnPoints)
            {
                /*Index information is stored in order to refer to the same SpawnManager*/
                Distances.Add(new ValueGroup(i, Vector3.Distance(Unit.ptr.position, s.transform.position)));
                i++;
            }
            /*Sort from closest to farthest from unit*/
            Distances.Sort(delegate(ValueGroup lhs, ValueGroup rhs)
            {
                if (lhs.value < rhs.value)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });
            int n = Distances.Count;
            i = 0;
            foreach (ValueGroup v in Distances)
            {
                /*Insert or Update threat information.Closer spawns are given slightly more threat
                 than farther spawns.*/
                Unit.UpdateAggro(n * 10, SpawnManager.AllySpawnPoints[Distances[i].index].netId, false);
                i++;
                n--;
            }
        }

        public override void UnitAggroReaction(Collider col)
        {
            HealthDefence target = col.gameObject.GetComponent<HealthDefence>();
            /*Players will generate 100 aggro automatically upon entering 
             aggro radius,but only if they're not on the list beforehand*/
            if (target.type == HealthDefence.Type.Unit)
            {
                if(!Array.Exists(Unit.HateList,delegate(ValueGroup v)
                {
                    return(v.index == (int)target.netId.Value);
                }))
                {
                    Unit.UpdateAggro(100, target.netId, false);
                }
            }
            /*A Spawn Point in the radius gradually gain threat at a rate of
             5 units per second*/
            else if (target.type == HealthDefence.Type.Spawn_Point)
            {
                Unit.StartCoroutine(GenerateGradualThreat(target.transform,target.netId));
            }
        }

        public Conquer(AIController AI) : base(AI) { }
            
        

    }

    private partial class HuntPlayers : ObjectiveState
    {
        public override void ResetHateList()
        {
            List<ValueGroup> Distances = new List<ValueGroup>();
            int i = 0;
            GameObject p;
            foreach (uint u in PlayerController.PlayerIDList)
            {
                /*Index information is stored in order to refer to the same SpawnManager*/
                p = ClientScene.FindLocalObject(new NetworkInstanceId(u));
                Distances.Add(new ValueGroup(i, Vector3.Distance(Unit.ptr.position, p.transform.position)));
                i++;
            }
            /*Sort from closest to farthest from unit*/
            Distances.Sort(delegate(ValueGroup lhs, ValueGroup rhs)
            {
                if (lhs.value < rhs.value)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });
            int n = Distances.Count;
            i = 0;
            foreach (ValueGroup v in Distances)
            {
                /*Insert or Update threat information.Closer units are given significantly more threat
                 than farther units.*/
                Unit.UpdateAggro(n * 25, new NetworkInstanceId(PlayerController.PlayerIDList[Distances[i].index]), false);
                i++;
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
                    if (!Array.Exists(Unit.HateList, delegate(ValueGroup v)
                    {
                        return (v.index == (int)target.netId.Value);
                    }))
                    {
                        Unit.UpdateAggro(100, target.netId, false);
                    }
                    Unit.StartCoroutine(GenerateGradualThreat(target.transform, target.netId));
                }
                /*Spawn Points will generate 20 aggro automatically upon entering 
                aggro radius,but only if they're not on the list beforehand*/
                if (target.type == HealthDefence.Type.Spawn_Point)
                {
                    if (!Array.Exists(Unit.HateList, delegate(ValueGroup v)
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
                if (AIController.AttackFuncs[Unit.attack_func_index](Unit))
                {
                    Unit.ptr.LookAt(target.gameObject.transform);
                    Unit.gun.Shoot();
                }
            }

            if (target.type == HealthDefence.Type.Spawn_Point)
            {
                /*SpawnPoints will generate 200 aggro automatically upon entering 
                aggro radius,but only if they're not on the list beforehand*/
                if (target.type == HealthDefence.Type.Unit)
                {
                    if (!Array.Exists(Unit.HateList, delegate(ValueGroup v)
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
        public override void ResetHateList() { }                /*PLayers/Spawns within radius are given more priority         than those outside.*/        public override void UnitAggroReaction(Collider col)        {            HealthDefence target = col.gameObject.GetComponent<HealthDefence>();
            Unit.StartCoroutine(GenerateGradualThreat(target.transform, target.netId, 15));        }
        public Guard(AIController AI) : base(AI) { }
    }


    
    

   

}
