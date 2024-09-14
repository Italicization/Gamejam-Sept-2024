using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class UnderwaterEffects : MonoBehaviour
{
	[SerializeField] private Volume underwaterVolume;
	[SerializeField] private Volume surfaceVolume;
	[SerializeField] private WaterEffect surfaceEffect;
	[SerializeField] private WaterEffect underwaterEffect;
	[SerializeField] private float fade = 0.05f;
	[Header("Debug")]
	[SerializeField] private float heightOverWater;

	private void LateUpdate()
	{
		if (underwaterVolume == null || surfaceVolume == null)
			return;
		
		Vector3 position = transform.position;
		float waterHeight = WaterSurface.Instance.GetHeightAtPosition(position);
		heightOverWater = position.y - waterHeight;
		underwaterVolume.weight = math.saturate(-heightOverWater / fade + fade * 0.5f);
		surfaceVolume.weight = math.saturate(heightOverWater / fade + fade * 0.5f);
		RenderSettings.fogStartDistance = math.lerp(underwaterEffect.StartFog, surfaceEffect.StartFog, math.saturate(heightOverWater / fade - 0.5f));
		RenderSettings.fogEndDistance = math.lerp(underwaterEffect.EndFog, surfaceEffect.EndFog, math.saturate(heightOverWater / fade - 0.5f));
		RenderSettings.fogColor = Color.Lerp(underwaterEffect.FogColor, surfaceEffect.FogColor, math.saturate(heightOverWater / fade - 0.5f));
	}
	
	[Serializable]
	private class WaterEffect
	{
		public Color FogColor = Color.white;
		public float StartFog = 0.0f;
		public float EndFog = 100f;
	}
}
