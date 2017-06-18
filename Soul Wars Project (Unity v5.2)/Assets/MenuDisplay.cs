using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuDisplay : MonoBehaviour {
    private Button button;
    private Vector3 Pos;
    public GameObject Menu;
    public GameObject Guntable;
	// Use this for initialization
    /*Notes on UI Instantiation,Parent must be set same time as creation,therefore cant use instantiate as overload doesn't exist
     * Unity 5.2.*/
     
	void Start () 
    {
        Pos = Menu.transform.localPosition;
        Menu.transform.parent = null;
        Guntable.transform.SetParent(null);
        button = GetComponent<Button>();
        button.onClick.AddListener(delegate { ShowMenu(); });
	}
	
	void ShowMenu ()
    {
       
        Menu.transform.parent = transform.parent;
        Menu.transform.localPosition = Pos;
	}
}
