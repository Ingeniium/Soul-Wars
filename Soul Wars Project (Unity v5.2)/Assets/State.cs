using UnityEngine;
using System.Collections;

public partial class AIController : MonoBehaviour {

    private class State
    {
       public abstract void OnStateEnter();
       public abstract void OnStateStay();
       public abstract void OnStateExit();
       private abstract void RevertToPreviousState();
       State prev_state;
    }

    private class Objective : State
    {

    }
}
