using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class UnderWaterChangeEvent : UnityEvent<bool> { }

public class UnderWaterChangeListener : MonoBehaviour
{
	public UnderWaterChangeEvent OnUnderWaterChange;

	private bool isUnderWater;

	private void LateUpdate()
	{
		Vector3 position = transform.position;
		float waterHeight = WaterSurface.Instance.GetHeightAtPosition(position);
		float heightOverWater = position.y - waterHeight;
		bool newUnderWater = heightOverWater < 0;

		if (newUnderWater != isUnderWater)
		{
			isUnderWater = newUnderWater;
			OnUnderWaterChange?.Invoke(isUnderWater);
		}
	}
}