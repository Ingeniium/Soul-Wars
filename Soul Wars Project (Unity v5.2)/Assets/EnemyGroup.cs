
using UnityEngine;
using UnityEngine.Networking;

public class EnemyGroup : NetworkBehaviour
{
    public Vector3 pos;
    public GameObject Enemy;
    public GameObject[] Gun;
    public AIController.MovementMode MovementSetting;
    public AIController.AttackMode[] AttackSettings;

}


