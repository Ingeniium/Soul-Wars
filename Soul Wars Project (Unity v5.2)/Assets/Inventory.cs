using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour {
    private List<GameObject> inventory_items = new List<GameObject>();
    private uint maximum = 10;
	// Use this for initialization
	void Start () 
    {
        inventory_items = new List<GameObject>();	
	}
    public void InsertItem(ref GameObject item)
    {
        if (inventory_items.Count < maximum + 1)
        {
            inventory_items.Add(item);
            ItemImage image = item.GetComponentInChildren<ItemImage>();
            image.item_script.in_inventory = true;
            item.transform.parent = transform;
        }
    }
    public void RemoveItem(ref GameObject item)
    {
        if(inventory_items.Contains(item))
        {
            inventory_items.Remove(item);
        }
    }

}
