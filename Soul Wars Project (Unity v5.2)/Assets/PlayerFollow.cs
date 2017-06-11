using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class PlayerFollow : MonoBehaviour
{
    public PlayerController Player;
    public static PlayerController player;
    public Transform tr;
    public static  Vector3 offset;
    public Vector3 _offset;
    public static new Camera camera;
    private float next_time = 0;
    private Ray ray;
    public RaycastHit hit;

    void Awake()
    {
        camera = GetComponent<Camera>();
    }

    void Start()
    {
        tr = Player.transform;
        _offset = new Vector3(0, 20, 0);
    }


    void FixedUpdate()
    {
        if (Player != null && Player.isLocalPlayer)
        {
            ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                tr.LookAt(new Vector3(hit.point.x, tr.position.y, hit.point.z));
            }
        }
    }
    void LateUpdate()
    {
        if (Player != null)
        {
            transform.position = Player.transform.position + _offset;
        }
       
    }
}
