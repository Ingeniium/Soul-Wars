using System;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static void SortByLeastToGreatDist<T>(this List<T> comp_list,Vector3 pos) where T : Component
    {
        comp_list.Sort(delegate (T lhs, T rhs)
        {
            if(Mathf.Abs(
                Vector3.Distance(lhs.transform.position,pos))
                < Math.Abs(
                    Vector3.Distance(rhs.transform.position,pos)) )
            {
                return -1;
            }
            else
            {
                return 1;
            }
        });
    }

}
