using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    public Canvas level_choose_show;
    void Awake()
    {
        Button button = GetComponent<Button>();
        level_choose_show.enabled = false;
        Button[] level_buttons = level_choose_show.GetComponentsInChildren<Button>();
        for(int i = 0;i < level_buttons.Length;i++)
        {
            /*A temp variable is needed in this case as it seems that
              the other buttons point to where i = buttons.length*/
            int temp = i;
            level_buttons[i].onClick.AddListener(delegate ()
            {
                SceneManager.LoadScene("Level " + (temp + 1));
            });  
        }
        button.onClick.AddListener(delegate ()
        {
            NetworkManager.singleton.GetComponent<NetworkManagerHUD>().showGUI = true;
            level_choose_show.enabled = true;
            Destroy(transform.parent.gameObject);
        });
    }     
}
