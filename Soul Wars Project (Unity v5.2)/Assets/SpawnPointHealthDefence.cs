using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointHealthDefence : HealthDefence
{
    private List<ValueGroup<int, int>> damage_counter_list = new List<ValueGroup<int, int>>();

    public void UpdateDamageCounter(int damage, int layer)
    {
        if (layer == LayerMask.NameToLayer("Invincible"))
        {
            return;
        }
        int index = damage_counter_list.FindIndex(delegate (ValueGroup<int, int> v)
        {
            return (v.index == layer);
        });
        if (index == -1)
        {
            damage_counter_list.Add(new ValueGroup<int, int>(layer, damage));
        }
        else
        {
            damage_counter_list[index] = new ValueGroup<int, int>(layer,
                damage_counter_list[index].value + damage);
        }
    }

    protected override void OnDeath()
    {
        SpawnManager s = GetComponent<SpawnManager>();
        if (damage_counter_list.Count > 0)
        {
            damage_counter_list.Sort(delegate (ValueGroup<int, int> lhs, ValueGroup<int, int> rhs)
            {
                if (lhs.value > rhs.value)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });
            int new_layer = damage_counter_list[0].index;
            if(new_layer == LayerMask.NameToLayer("Invincible"))
            {
                return;
            }
            s.RpcChangeTeam(new_layer);
            damage_counter_list.Clear();
            HP = maxHP;
        }
    }
    
}

