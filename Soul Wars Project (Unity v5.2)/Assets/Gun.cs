using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections;

public class Gun : MonoBehaviour {
	public GameObject Bullet;
	private GameObject bullet;
	public float reload_time = 1.0f;
	public float next_time = -1f;
	public float home_radius = 5.0f;
	public int damage = 2;
	public Transform barrel_end;
    public Color color;
    public int layer;
    public int home_layer;
    public virtual void Shoot()
    {
        bullet = Instantiate(Bullet, barrel_end.position, barrel_end.rotation) as GameObject;
        ReadyWeaponForFire(ref bullet);
        bullet.GetComponent<Rigidbody>().AddForce(barrel_end.forward, ForceMode.Impulse);//works
    }
    protected void ReadyWeaponForFire(ref GameObject weapon_fire)
    {
        weapon_fire.GetComponent<Renderer>().material.color = color;
        BulletScript script = weapon_fire.GetComponent<BulletScript>();
        script.damage = damage;
        script.home_radius = home_radius;
        script.gameObject.layer = layer;
        script.home.layer = home_layer;
        next_time = Time.time + reload_time;
    }
    public override string ToString()
    {
        return string.Format("Launches a powerful arrow.\n Homing : {0} Damage : {1} \n Reload Time : {2}", home_radius, damage, reload_time);                
    }
 }
