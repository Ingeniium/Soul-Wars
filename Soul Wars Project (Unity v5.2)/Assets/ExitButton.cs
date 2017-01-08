using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ExitButton : MonoBehaviour {
    private Button button;
	// Use this for initialization
	void Start () 
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(delegate { HideMenu(); });
	}

    void HideMenu()
    {
        transform.parent.SetParent(null);
    }
}
