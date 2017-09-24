
using UnityEngine;
using UnityEngine.Networking;

public class EnemyGroup : NetworkBehaviour
{
    public Vector3 pos;
    public GameObject Enemy;
    public GameObject[] Gun;
    public int[] weapon_levels;
    public AIController.MovementMode MovementSetting;
    public AIController.AttackMode[] AttackSettings;
    public bool can_block;
    public bool can_dodge;
}


