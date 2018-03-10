using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;


public partial class AIController : GenericController
{
    public Rigidbody prb;//Rigidbody of the actual enemy object
    public Transform ptr;//Transform of the actual enemy object
    public HealthDefence Target;//What the unit is trying to pursue
    private bool target_focus = true;//Whether to look at the target or not.Important when blocking.
    private Collider trig;
    public float shoot_delay;//Delay with an enemy shooting.
    private List<Collider> bullet_colliders = new List<Collider>();
    private Vector3 vec;
    public float reaction_delay;
    public SphereCollider enemy_attack_detection;//The collider of the detection sphere
    public float time_until_next_pathfind;//Time after the next frame that it does the A* algorithm again.Important in maintaing gameplay smoothness.
    public int index;
    private static int go_index = 0;
    public GameObject shell;

    /*List showing who is aggro'd and what their aggro values are.Made into an array as structs can't be modified from lists.*/
    public ValueGroup[] HateList = new ValueGroup[20]
    {
       NOT_SET,       NOT_SET,
       NOT_SET,     NOT_SET,
       NOT_SET,      NOT_SET,
       NOT_SET,      NOT_SET,
       NOT_SET,      NOT_SET,

       NOT_SET,       NOT_SET,
       NOT_SET,       NOT_SET,
       NOT_SET,      NOT_SET ,
       NOT_SET,      NOT_SET,
       NOT_SET,        NOT_SET,
    };

    private readonly static ValueGroup NOT_SET = new ValueGroup(-1, -1);

    private static System.Random rand = new System.Random();
    private ObjectiveState State;//Sets the objective and reaction to certain ally objects coming into range
       

    protected override void Start()
    {
        PaintShell();
        base.Start();
        if (PlayerController.Client && PlayerController.Client.isServer)
        {
            index = PlayersAlive.Instance.Units.Count;
            if (!EnemyInitialization.Instance.watch_only)
            {
                StartCoroutine(WaitForPlayers());
            }
            else
            {
                StartCoroutine(Travel2());
            }
        }

    }

    protected override IEnumerator AddUnitToPlayersAlive()
    {
        while (!PlayersAlive.Instance)
        {
            yield return new WaitForEndOfFrame();
        }
        PlayersAlive.Instance.Units.Add(this);
    }

    void PaintShell()
    {
        shell.GetComponent<Renderer>().material.color = SpawnManager.GetTeamColor(ptr.gameObject.layer, .5f);
    }

    public IEnumerator PaintShell(Color color)
    {
        while (ptr.childCount < 5)
        {
            yield return new WaitForEndOfFrame();
        }
        GameObject shell_show = ptr.GetChild(4).gameObject;
        shell_show.GetComponent<Renderer>().material.color = color;
    }

    [ServerCallback]
    IEnumerator WaitForPlayers()
    {
        while (PlayerController.players.Count < 1)
        {
            yield return new WaitForEndOfFrame();
        }
        PlayersAlive.Instance.CmdPause();
        while (!PlayerController.players.Exists(delegate (PlayerController p)
         {
             return p.enabled;
         }))
        {
            yield return new WaitForEndOfFrame();
        }
        PlayersAlive.Instance.CmdUnpause();
        StartCoroutine(Travel2());
    }

    [ServerCallback]
    void FixedUpdate()
    {
        if (State == null)
        {
            return;
        }
        State.AffirmTarget(Target);
        if (Target)
        {
            if (target_focus)
            {
                ptr.LookAt(Target.transform);
            }
            prb.velocity = move_dir.normalized * speed;
            int i = 0;
            foreach (Gun gun in weapons)
            {
                if (gun)
                {
                    bool hit = WillBulletHitObstacle(gun);
                    if (AttackFuncs[attack_func_indexes[i]](this, gun) && !hit)
                    {
                        main_gun = gun;
                        gun.Shoot(main_gun.barrel_end.forward,main_gun.barrel_end.position,main_gun.barrel_end.rotation);
                    }
                }
                i++;
            }
        }

    }

    [ServerCallback]
    void OnTriggerEnter(Collider col)
    {
        /*If an opponent or spawn point was detected within aggro radius,
         react based on State instructions*/
        if (State != null
            && !(LayerMask.LayerToName(col.gameObject.layer).Contains(
                "Attack")))
        {
            State.UnitAggroReaction(col);
        }
        /*If bullet,prepare to evade*/
        else
        {
            bullet_colliders.RemoveNull();
            bullet_colliders.Add(col);
            StartCoroutine(WaitForBlock(col));
        }
    }

    [ServerCallback]
    void OnTriggerExit(Collider col)
    {
        bullet_colliders.RemoveNull();
        bullet_colliders.Remove(col);
    }

    IEnumerator WaitForBlock(Collider col)
    {
        /*Only block if:
         not trying to block another bullet(target_focus is true)
         shield isnt regenerating (which would happen if the shield's HP gone to zero and hasn't fully generated)
         the collider still exists
         the collider isn't a trigger = which would go thru shield any way*/
        if (!Shield)
        {
            yield break;
        }
        else
        {
            ShieldHealthDefence SP = Shield.GetComponent<ShieldHealthDefence>();
            while (target_focus && !SP.regen && col && !col.isTrigger)
            {
                /*Wait until its gets 2 units away or it disappears*/
                if (Math.Abs(
                    Vector3.Distance(col.gameObject.transform.position, ptr.position))
                    < 2f)
                {
                    float start_block = Time.realtimeSinceStartup;
                    const float MAX_BLOCKING_TIME = 2;
                    /*Look at the bullet and block until it disappears.*/
                    target_focus = false;
                    StartShieldBlocking();
                    ptr.LookAt(col.transform);
                    while (col
                        && Time.realtimeSinceStartup < start_block + MAX_BLOCKING_TIME)
                    {
                        yield return new WaitForFixedUpdate();
                    }
                    break;
                }
                yield return new WaitForFixedUpdate();
            }
        }
        if (blocking)
        {
            target_focus = true;
            EndShieldBlocking();
        }
    }

    public void PrintAggro(string aggresor_name, uint ID)
    {
        float aggro = Array.Find(HateList, delegate (ValueGroup v)
        {
            return (v.index == ID);
        }).value;
        string team = LayerMask.LayerToName(ptr.gameObject.layer);
        Debug.Log(team + " CPU " + index + "'s aggro towards " + aggresor_name
            + " : " + aggro);
    }

    public void UpdateAggro(int damage = 0, NetworkInstanceId player_id = new NetworkInstanceId(), bool account_attack_dist = true)
    {
        if (damage != 0)
        {
            //Debug.Log("Aggro Called");
            float dist_multiplier = 1;
            Transform playertr = NetworkServer.FindLocalObject(player_id).transform;
            if (account_attack_dist && Target)
            {
                /*The closer the player is to the enemy the more threat generated from
                 the respective attack done by the player*/
                float dist_ratio = enemy_attack_detection.radius /
                    Vector3.Distance(Target.transform.position, ptr.position);
                /*The distance only has 25% bearing on the threat,however.*/
                dist_multiplier = .75f + .25f * (dist_ratio);
            }
            /*If the player is not currently on the enemy's hatelist...
             (netIds are used as ValueGroup indeces)*/
            if (!Array.Exists(HateList, delegate (ValueGroup g)
            {
                return (g.index == (int)(player_id.Value));
            }))
            {
                /*Find the first empty slot to store threat info 
                 in*/
                int index = Array.FindIndex(HateList, delegate (ValueGroup g)
                {
                    return (g.value == -1);
                });
                //Debug.Log(index);
                HateList[index] = new ValueGroup((int)player_id.Value, dist_multiplier * (float)damage);
            }
            else
            {
                /*If the player is in the aggro list,then simply add to the threat 
                 data stored into it*/
                int index = Array.FindIndex(HateList, delegate (ValueGroup g)
                {
                    return (g.index == (int)player_id.Value);
                });
                HateList[index].value += (float)damage * dist_multiplier;
            }
        }
        /*There needs to be a .2 times more threat to move up one place.
         This is to prevent rapid switching of targets constantly w/o huge 
         damage changes.*/

        Array.Sort(HateList, delegate (ValueGroup lhs, ValueGroup rhs)
        {
            /*if (lhs.value == -1)
            {
                return 0;
            } */
            if (lhs.value * .9f > rhs.value)
            {
                return -1;
            }
            else if (lhs.value * 1.1f < rhs.value)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        });
        if (HateList[0] != NOT_SET)
        {
            /*Assign target to one with most threat.The Gameobject's
             existence is checked in event that a player disconnects.*/
            GameObject g = NetworkServer.FindLocalObject(new NetworkInstanceId((uint)HateList[0].index));
            if (g == null)
            {
                RemoveAggro(new NetworkInstanceId((uint)HateList[0].index));
            }
            else
            {
                Target = g.GetComponent<HealthDefence>();
            }
        }
        else
        {
            Target = null;
        }
    }

    /*For removing info about captured spawn points or killed players
     from the HateList*/
    void RemoveAggro(NetworkInstanceId ID)
    {
        int index = Array.FindIndex(HateList, delegate (ValueGroup v)
        {
            return (v.index == (int)(ID.Value));
        });
        if (index != -1)
        {
            HateList[index] = NOT_SET;
            UpdateAggro();
        }
    }

    void ClearHateList()
    {
        for (int i = 0; i < HateList.Length; i++)
        {
            HateList[i] = NOT_SET;
        }
    }





}

