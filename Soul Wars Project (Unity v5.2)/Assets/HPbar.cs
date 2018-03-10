using UnityEngine;

public class HPbar : MonoBehaviour {
    public GameObject Object;
    public Vector3 offset;
	void LateUpdate ()
    {
        if (Object)
        {
            transform.position = Object.transform.position + offset;
        }
	}
	
	
}
