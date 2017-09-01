using UnityEngine;


public class PlayerFollow : MonoBehaviour
{
    public PlayerController Player;
    public Transform tr;
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

    void Update()
    {
        //Debug.Log(1 / Time.deltaTime);
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
