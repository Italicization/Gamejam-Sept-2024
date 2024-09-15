using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class SizedObject : MonoBehaviour
{
	[SerializeField] private float size;

	public float Size => size;
	public event Action<float> OnSizeChanged; 

	public void Grow(float size)
	{
		this.size += size;
		OnSizeChanged?.Invoke(this.size);
	}
}
