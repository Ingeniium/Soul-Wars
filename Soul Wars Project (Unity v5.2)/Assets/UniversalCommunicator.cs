using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AIController : GenericController
{
    private partial class UniversalCommunicator
    {
        public List<AIController> Units = new List<AIController>();//Total list of all the members
        List<GroupCommunicator> Groups;//What groups are there
        float evaluation_interval = 5f;//How often changes to groups may be made
        public bool set = false;//For onetime initialization with each Aicontroller's start()

        public void Start(List<GroupCommunicator> GCs)
        {
            Groups = GCs;
            SetGroupMembers();
            foreach (GroupCommunicator GC in GCs)
            {
                GC.Start();
            }
            /*Trying to inherit from monobehaviour to start a coroutine on a nested class will cause an exception.
             Likewise,the engine doesn't start their respectiv Awake and Start functions automatically*/
            Units[0].StartCoroutine(EvaluateGroupNeeds());
        }

        void SetGroupMembers()//For assigning the which group gets what members at the start of the game
        {
            foreach (AIController AI in Units)
            {
                Groups[0].Members.Add(AI);
            }
        }

        IEnumerator EvaluateGroupNeeds()//For determining which group needs another member,if any.
        {
            System.Random rand = new System.Random();
            while (Units[0])
            {
                yield return new WaitForSeconds(evaluation_interval);
                int i = 0;//For noting the index of the respective group
                List<ValueGroup> NeedTable = new List<ValueGroup>();
                foreach (GroupCommunicator GC in Groups)
                {
                    NeedTable.Add(new ValueGroup(i, GC.need));
                    print(GC.ToString() + ":" + GC.need.ToString());
                    i++;
                }
                if (Groups.Count > 1)
                {
                    /*Sorting is done based on the values of each group's "need" value.
                     SOrt is done from greatest need to least need.*/
                    NeedTable.Sort(delegate(ValueGroup lhs, ValueGroup rhs)
                    {
                        if (lhs.value > rhs.value)
                        {
                            return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    });
                    /*Changes in membership are only done if the most needy group has atleast twice
                     the need of the least needy.If so,a random member from the least needy group is
                     given to the most needy group.*/
                    if (NeedTable[0].value >= 2 * NeedTable[NeedTable.Count - 1].value)
                    {
                        /*The index of the one with the least need is going to be
                         in the last value group in needtable due to its sorting*/
                        int group_index = NeedTable[NeedTable.Count - 1].index;
                        /*random number is going to be generated between the amount of members
                        that group has*/
                        int member_index = rand.Next(Groups[group_index].Members.Count - 1);
                        AIController AI = Groups[group_index].Members[member_index];
                        Groups[group_index].Members.Remove(AI);
                        group_index = NeedTable[0].index;
                        Groups[group_index].Members.Add(AI);
                        Groups[group_index].SetMemberTarget(AI);//Set new Target
                    }
                }
            }
        }
    }
}
