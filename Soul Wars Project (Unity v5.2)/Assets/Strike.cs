using UnityEngine;
using System.Collections;

public class Strike : Gun {
    private readonly static string[] gun_ability_names = new string[3] { "Marksman", "Sniper", "Drone" };
    private static Gun_Abilities[] Gun_Mods = new Gun_Abilities[3];//This class's pool of gun_abilities
   
    /*Ability that grants extra crit chance based on how far the bullet
     deviates from its "original" path.Less deviation means more crit,the 
     maximum amount being +30%*/
    private IEnumerator Marksman(BulletScript script)
    {
        script.coroutines_running++;
        Vector3 start_pos = script.transform.position;//Get its original position
        Vector3 start_forward = script.transform.forward;//Get where it's originally facing
        while (script.has_collided == false)//Wait for Collision
        {
            yield return new WaitForFixedUpdate();
        }
        if (script.legit_target == false)//Check if target even is valid
        {
            script.coroutines_running--;
            yield return null;
        }
        else
        {
            Vector3 current = script.transform.position - start_pos;//Get the vector representing distance traveled
            Vector3 path = Vector3.Project(current, start_forward);//Use current to project where the bullet would go if it didn't turn
            float ang = Vector3.Angle(path, current);
            if (ang < 30)//Award bonus only if deviation is less than 30 degrees
            {
                if (ang < 5)//If deviation is less than 5 degrees,go ahead and give full bonus
                {
                    ang = 0;
                }
                script.crit_chance += (30 - ang) * .01;
            }
            print(ang.ToString());
            script.coroutines_running--;
            yield return null;
        }
    }

    protected override string ClassGunAbilityNames(int index)
    {
        return gun_ability_names[index];
    }

    protected override Gun_Abilities ClassGunMods(int index)
    {
        return Gun_Mods[index];
    }

    protected override void SetBaseGunAbilities()
    {
        Gun_Mods[0] = Marksman;
    }

    protected override bool AreGunLevelUpButtonsAssignedForClass()
    {
        return (GunTable.buttons[0].method == Marksman);
    }
}
