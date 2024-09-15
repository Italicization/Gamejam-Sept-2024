using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FoodItem : SizedObject
{
    public virtual void Eat()
	{
		Destroy(gameObject);
	}
}
