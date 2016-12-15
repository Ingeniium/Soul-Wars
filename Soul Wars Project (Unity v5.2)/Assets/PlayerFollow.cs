﻿using UnityEngine;
using System.Collections;

public class PlayerFollow : MonoBehaviour
{
    public GameObject Player;
    private Transform tr;
    private Vector3 offset;
    private Camera camera;
    Ray ray;
    RaycastHit hit;
    void Start()
    {
        offset = transform.position - Player.transform.position;
        camera = GetComponent<Camera>();
        tr = Player.GetComponent<Transform>();
    }

    void FixedUpdate()
    {
        if (Player != null)
        {
            ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                tr.LookAt(new Vector3(hit.point.x, 10.3f, hit.point.z));

            }
        }
    }
    void LateUpdate()
    {
        if (Player != null)
        {
            transform.position = Player.transform.position + offset;
        }
    }
}