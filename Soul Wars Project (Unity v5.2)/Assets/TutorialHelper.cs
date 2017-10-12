using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TutorialHelper : MonoBehaviour
{
    private StringSeries[] advice_content_list;
    public List<Vector3> advice_location_list = new List<Vector3>();
    public List<bool> is_world_space_list = new List<bool>();

    IEnumerator WaitForTextBoxRef()
    {
        advice_content_list = GetComponents<StringSeries>();
        while(!TextBox.Instance)
        {
            yield return new WaitForEndOfFrame();
        }
        for (int i = 0; i < advice_location_list.Count; i++)
        {
            string sentence = "";
            for(int j = 0;j < advice_content_list[i].strings.Count;j++)
            {
                sentence += advice_content_list[i].strings[j] + "\r\n";
            }
            TextBox.Instance.CreateExitDescBox(
                null,
                advice_location_list[i],
                sentence,
                is_world_space_list[i]);
        }
    }
    void Start()
    {
        StartCoroutine(WaitForTextBoxRef());
    }
}
