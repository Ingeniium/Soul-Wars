using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class UnitHealthDefence : HealthDefence
{
    public int blink_rate;
    private int next_blink_interval;
    public Rigidbody rb;

    [SyncVar] public double mezmerize_resistance;
    [SyncVar] public double chill_resistance;

    private bool _chilling;
    public bool chilling
    {
        get { return _chilling; }
        private set
        {
            _chilling = true;
        }
    }
    private bool _mezmerized;
    public bool mezmerized
    {
        get { return _mezmerized; }
        private set
        {
            _mezmerized = value;
        }
    }
    private bool _stunned;
    public bool stunned
    {
        get { return _stunned; }
        private set
        {
            _stunned = value;
        }
    }

    public float exp_rate = .5f;

    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();
        next_blink_interval = maxHP - blink_rate;
    }

    protected override void OnDeath()
    {
        if (!GetComponent<PlayerController>())
        {
            RpcClearHPBar();
        }
        StartCoroutine(SpawnManager.WaitForRespawn(this));
        next_blink_interval = maxHP - blink_rate;
    }

    public override void DetermineStatusEffects(double[] powers, int damage)
    {
        base.DetermineStatusEffects(powers, damage);
        StartCoroutine(DetermineChill(powers[2]));
        StartCoroutine(DetermineMezmerize(powers[3]));
    }

    protected override void OnNormalHPChange()
    {
        if (blink_rate > 0 && HP < next_blink_interval)
        {
            next_blink_interval -= blink_rate;
            NetworkMethods.Instance.RpcBlink(gameObject, 1.5f);
        }
    }

    public IEnumerator DetermineChill(double chill)
    {
        double net_chill = chill - chill_resistance;
        if (net_chill > 0 && !chilling && rand.NextDouble() < net_chill * 8)
        {
            GenericController controller = GetComponentInChildren<GenericController>();
            chilling = true;
            float original = controller.speed;
            controller.speed = (100 - (float)net_chill * 800) * (.01f * controller.speed);
            float time = (float)(net_chill * 200);
            float next_time = Time.time + time;
            RpcUpdateAilments("\r\n <color=cyan>Chill</color> ", time);
            while (Time.time < next_time && HP != 0)
            {
                yield return new WaitForEndOfFrame();
            }
            controller.speed = original;
            chilling = false;
        }
    }

    public IEnumerator DetermineMezmerize(double mez)
    {
        double net_mez = mez - mezmerize_resistance;
        if (net_mez > 0 && !mezmerized && rand.NextDouble() < net_mez * 6)
        {
            GenericController controller = GetComponentInChildren<GenericController>();
            mezmerized = true;
            foreach (Gun gun in controller.weapons)
            {
                if (gun)
                {
                    gun.mez_threshold = (int)(net_mez * 100) / 2;
                }
            }
            float time = (float)(net_mez * 150);
            float next_time = Time.time + time;
            RpcUpdateAilments("\r\n <color=purple>Mezmerize</color> ", time);
            while (Time.time < next_time && HP != 0)
            {
                yield return new WaitForEndOfFrame();
            }
            foreach (Gun gun in controller.weapons)
            {
                if (gun)
                {
                    gun.mez_threshold = 0;
                }
            }
            mezmerized = false;
        }
    }

    public void DetermineStun(float time)
    {
        if (!stunned)
        {
            RpcUpdateAilments("\r\n <color=yellow>Stun</color>", time);
            GenericController controller = GetComponentInChildren<GenericController>();
            if (controller is PlayerController)
            {
                RpcStun(time);
            }
            else
            {
                StartCoroutine(Stun(controller,time));
            }
        }
    }

    [ClientRpc]
    void RpcStun(float time)
    {
        GenericController controller = GetComponentInChildren<GenericController>();
        StartCoroutine(Stun(controller,time));
    }

    IEnumerator Stun(GenericController controller,float time)
    {
        controller.enabled = false;
        stunned = true;
        yield return new WaitForSeconds(time);
        stunned = false;
        if (HP > 0)
        {
            controller.enabled = true;
        }
    }
}

