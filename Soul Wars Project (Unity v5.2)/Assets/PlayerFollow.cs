using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class PlayerFollow : MonoBehaviour
{
    public PlayerController Player_;
    public static GameObject Player;
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
        _offset = transform.position - Player_.transform.position;
        //camera = GetComponent<Camera>();
        tr = Player_.GetComponent<Transform>();
    }

    void FixedUpdate()
    {
        if (Player_ != null && Player_.isLocalPlayer)
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
        if (Player_ != null)
        {
            transform.position = Player_.transform.position + _offset;
        }
       
    }
}
