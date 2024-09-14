using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class RandomSpawner : MonoBehaviour
{
	[SerializeField] private GameObject[] prefabVariants;
	[SerializeField] private Rect rect;

	public Rect WorldRect => new Rect((float2)rect.position + ((float3)transform.position).xz - (float2)rect.size * 0.5f, rect.size);
	
	private void OnDrawGizmosSelected()
	{
		Rect worldRect = WorldRect;
		float height = transform.position.y;
		
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(new Vector3(worldRect.xMin, height, worldRect.yMin), new Vector3(worldRect.xMin, height, worldRect.yMax));
		Gizmos.DrawLine(new Vector3(worldRect.xMin, height, worldRect.yMax), new Vector3(worldRect.xMax, height, worldRect.yMax));
		Gizmos.DrawLine(new Vector3(worldRect.xMax, height, worldRect.yMax), new Vector3(worldRect.xMax, height, worldRect.yMin));
		Gizmos.DrawLine(new Vector3(worldRect.xMax, height, worldRect.yMin), new Vector3(worldRect.xMin, height, worldRect.yMin));
	}
}
