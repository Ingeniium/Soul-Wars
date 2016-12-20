using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour {
    public List<GameObject> inventory_items;
    public uint size = 0;
    private uint maximum = 10;
	// Use this for initialization
	void Start () 
    {
        inventory_items = new List<GameObject>();	
	}
    public void InsertItem(ref GameObject item)
    {
        if (size < maximum)
        {
            size++;
            inventory_items.Add(item);
            item.transform.parent = transform;
        }
    }

}
