
using UnityEngine;
using UnityEngine.Networking;

public class EnemyGroup : NetworkBehaviour
{
    public Vector3 pos;
    public GameObject Enemy;
    public GameObject[] Gun;
    public int movement_type;
    public AIController.AttackMode[] AttackSettings;

}


