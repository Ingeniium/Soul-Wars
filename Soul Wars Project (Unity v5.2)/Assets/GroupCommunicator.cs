using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class AIController : GenericController
{
    
    private abstract class GroupCommunicator
    {
        public List<AIController> Members = new List<AIController>();
        protected int alive_members;
        protected float evaluation_interval = 3f;
        public float need = 0;//used so universal communicatorcan determine which group gets what members
        public abstract void SetMemberTargets();//Sets state and sometimes ally unit/spawn reaction in range of members
        public abstract void SetMemberTarget(AIController AI);//Used for members that get switched into this group mid-game
        public virtual IEnumerator EvaluateSituation() { return null; }

        public void Start()
        {
            alive_members = Members.Count;
            SetMemberTargets();
            /*Trying to inherit from monobehaviour to start a coroutine on a nested class will cause an exception.
             Likewise,the engine doesn't start their respectiv Awake and Start functions automatically*/
            Members[0].StartCoroutine(EvaluateSituation());
        }

        protected float EvalutatePlayervEnemyDeathRatio()
        {
            float num = 0;
            foreach (AIController AI in Members)
            {
                num += AI.num_of_deaths;
            }
            if (num != 0)
            {
                return num / Item.Player.num_of_deaths;
            }
            else
            {
                return 0;
            }
        }

        protected float EvaluatePlayervEnemyDamageRatio()
        {
            float num = 0;
            foreach (AIController AI in Members)
            {
                num += AI.total_damage_to_units;
            }
            if (num != 0)
            {
                return Item.Player.total_damage_to_units / num;
            }
            else
            {
                return 0;
            }
        }
    }

    private class HuntAdversaries : GroupCommunicator
    {
        public override void SetMemberTargets()
        {
            foreach (AIController AI in Members)
            {
                AI.State = new AttackAllyUnits(AI);
            }
        }

        public override void SetMemberTarget(AIController AI)
        {
            AI.State = new AttackAllyUnits(AI);
        }

        public override IEnumerator EvaluateSituation()
        {
            float num;
            while (Members[0])
            {
                yield return new WaitForSeconds(evaluation_interval);
                num = 0;
                num += EvaluatePlayervEnemyDamageRatio();
                num += EvalutatePlayervEnemyDeathRatio();
                need = num;
            }
        }
    }

    private class Conquer : GroupCommunicator
    {
        public override void SetMemberTargets()
        {
            foreach (AIController AI in Members)
            {
                AI.State = new AttackAllySpawn(AI);
                AI.State.RespondtoAllyUnit = AI.State.BecomeAgressive;
                AI.State.EndAllyUnitResponse = AI.State.LastSpawnNear;
            }
        }

        public override void SetMemberTarget(AIController AI)
        {
            AI.State = new AttackAllySpawn(AI);
            AI.State.RespondtoAllyUnit = AI.State.BecomeAgressive;
            AI.State.EndAllyUnitResponse = AI.State.LastSpawnNear;
        }

        private float EvaluatePlayervEnemySpawnRatio()
        {
            return (float)SpawnManager.AllySpawnPoints.Count / (float)SpawnManager.EnemySpawnPoints.Count;
        }

        public override IEnumerator EvaluateSituation()
        {
            float num;
            while (Members[0])
            {
                yield return new WaitForSeconds(evaluation_interval);
                num = 0;
                num += EvaluatePlayervEnemySpawnRatio();
                num += EvalutatePlayervEnemyDeathRatio();
                need = num;
            }
        }
    }

    private class GuardLocation : GroupCommunicator
    {
        public override void SetMemberTargets()
        {
            foreach (AIController AI in Members)
            {
                AI.State = new GuardLocations(AI, AI.ptr.position, 8f);
                AI.State.RespondtoAllyUnit = AI.State.BecomeAgressive;
                AI.State.EndAllyUnitResponse = AI.State.OutsideJurisdiction;
            }
        }

        public override void SetMemberTarget(AIController AI)
        {
            AI.State = new GuardLocations(AI, AI.ptr.position, 8f);
            AI.State.RespondtoAllyUnit = AI.State.BecomeAgressive;
            AI.State.EndAllyUnitResponse = AI.State.OutsideJurisdiction;
        }
    }
}

