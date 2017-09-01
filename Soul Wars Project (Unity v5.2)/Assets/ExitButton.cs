using UnityEngine;
using UnityEngine.UI;

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
        PlayersAlive.Instance.CmdUnpause();
        NetworkMethods.Instance.CmdSetLayer(
            PlayerController.Client.gameObject,
            LayerMask.NameToLayer("Ally")
            );
        NetworkMethods.Instance.CmdSetEnabled(
                    PlayerController.Client.gameObject,
                    "PlayerController",
                    true);
    }
}
