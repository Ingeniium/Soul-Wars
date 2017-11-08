
using UnityEngine;
using UnityEngine.Networking;

public class EnemyGroup : NetworkBehaviour
{
    public Vector3 pos;
    public GameObject Enemy;
    public AIController.Type AISetting;
    public GameObject[] Gun;
    public int[] weapon_levels;
    public AIController.AttackMode[] AttackSettings;
    public AIController.MovementMode MovementSetting;
    public bool can_block;
    public bool can_dodge;
    public enum Layer
    {
        Enemy = 8,
        Ally = 9,
        Team3 = 18,
        Team4 = 19,
        Team5 = 20
    }
    public Layer cpu_layer;
}


