using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections;

public class Gun : MonoBehaviour {
	public GameObject Bullet;
	public GameObject bullet;
	public float reload_time = 1.0f;
	public float next_time = -1f;
	public float home_radius = 5.0f;
	public int damage = 2;
	public Transform barrel_end;
    //Code commented out for getting previw pics
    public RawImage pic;
    public GameObject this_pic;
    public Material mat;
    //void Start()
    //{
    //    pic.texture = null;
   // }
   // void Update()
   // {
   //     if(pic.texture == null)
   //     {
   //         pic.texture = AssetPreview.GetAssetPreview(this_pic) as Texture;
   ////         mat.mainTexture = pic.texture;
    //   }
   // }
	
}
