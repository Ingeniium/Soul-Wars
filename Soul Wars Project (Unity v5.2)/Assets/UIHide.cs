using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIHide : MonoBehaviour {

    public static UIHide obj;
	void Start()
    {
        obj = this;
        transform.SetParent(null);
	}
	
}
