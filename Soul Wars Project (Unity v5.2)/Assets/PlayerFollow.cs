using UnityEngine;


public class PlayerFollow : MonoBehaviour
{
    public PlayerController Player;
    private int camera_focus_index = -1;
    public Transform tr;
    public Vector3 _offset;
    public float center_offset_y = 60;
    public static new Camera camera;
    private float next_time = 0;
    private Ray ray;
    public RaycastHit hit;
    private const string LOOK_AT_NEXT_CPU_KEY = "n";
    private const string LOOK_AT_PLAYER_KEY = "m";
    private const string LOOK_AT_CENTER = "p";
    private const string ZOOM_IN = "i";
    private const string ZOOM_OUT = "o";

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
        if (EnemyInitialization.Instance.watch_only && Input.GetKeyDown(LOOK_AT_NEXT_CPU_KEY) && PlayersAlive.Instance)
        {
            int max_iterations = 25;
            for (int i = 0; i < max_iterations; i++)
            {
                camera_focus_index++;
                if (camera_focus_index >= PlayersAlive.Instance.Units.Count)
                {
                    camera_focus_index = 0;
                }
                if(PlayersAlive.Instance.Units[camera_focus_index]
                    .GetComponentInParent<HealthDefence>().HP > 0)
                {
                    break;
                }
            }
        }
        else if (Input.GetKeyDown(LOOK_AT_PLAYER_KEY))
        {
            camera_focus_index = -1;
        }
        else if (EnemyInitialization.Instance.watch_only && Input.GetKey(LOOK_AT_CENTER))
        {
            camera_focus_index = -2;
            transform.position = new Vector3(0, center_offset_y, 0);
        }
        if (Input.GetKey(ZOOM_IN))
        {
            _offset = new Vector3(_offset.x, _offset.y - .25f, _offset.z);
        }
        else if (Input.GetKey(ZOOM_OUT))
        {
            _offset = new Vector3(_offset.x, _offset.y + .25f, _offset.z);
        }
        if (Player != null && camera_focus_index == -1)
        {
            transform.position = Player.transform.position + _offset;
        }
        else if(EnemyInitialization.Instance.watch_only && PlayersAlive.Instance && camera_focus_index > -1)
        {
            transform.position = PlayersAlive.Instance.Units[camera_focus_index].transform.position
                + _offset;
        }
       
    }
}
