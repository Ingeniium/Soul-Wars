using UnityEngine.Networking;
using UnityEngine;

public class DestroyPrevObjects : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {
        foreach (GameObject o in FindObjectsOfType<GameObject>())
        {
            if (o != gameObject && o != NetworkManager.singleton.gameObject)
            {
                Destroy(o);
            }
            NetworkManager.singleton.dontDestroyOnLoad = false;
            NetworkManager.singleton.ServerChangeScene("Networksample");
        }
    }
	
}
