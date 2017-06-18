using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cooldown : MonoBehaviour
{
    public static IEnumerator NumericalCooldown(Canvas obj, float num)
    {
        int seconds = (int)num;
        Text cooldown_text = obj.GetComponentInChildren<Text>();
        cooldown_text.color = Color.yellow;
        if (seconds > 0)
        {
            cooldown_text.text = seconds.ToString();
        }
        else
        {
            cooldown_text.text = num.ToString();
        }
        yield return new WaitForSeconds(num - (float)seconds);
        while (seconds != 0)
        {
            --seconds;
            yield return new WaitForSeconds(1);
            if (cooldown_text)
            {
                cooldown_text.text = seconds.ToString();
            }
            else
            {
                seconds = 0;
            }
        }
        if (obj)
        {
            Destroy(obj.gameObject);
        }
    }

}
