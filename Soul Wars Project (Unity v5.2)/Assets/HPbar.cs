﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class HPbar : MonoBehaviour {
    public GameObject Object;
    public Vector3 offset;
	
	void LateUpdate ()
    {
        transform.position = Object.transform.position + offset;
	}
	
	
}