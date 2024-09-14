using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomSpawner : MonoBehaviour
{
	[SerializeField] private GameObject[] prefabVariants;
	[SerializeField] private int spawnCount;
	[SerializeField] private Rect rect;

	private List<GameObject> spawnedObjects = new ();
	
	public Rect WorldRect => new Rect((float2)rect.position + ((float3)transform.position).xz - (float2)rect.size * 0.5f, rect.size);

	private void OnEnable()
	{
		WaterSurface.Instance.OnNewWaveStart.AddListener(OnWaveStart);
	}

	private void OnWaveStart()
	{
		PruneDeadObjects();

		for (int i = spawnedObjects.Count; i < spawnCount; i++)
		{
			Vector3 spawnPosition = new Vector3(Random.Range(WorldRect.xMin, WorldRect.xMax), 0, Random.Range(WorldRect.yMin, WorldRect.yMax));

			GameObject go = Instantiate(prefabVariants[Random.Range(0, prefabVariants.Length)], spawnPosition, Random.rotation);
			spawnedObjects.Add(go);

			if (go.TryGetComponent(out FollowWave followWave))
			{
				StartCoroutine(WaitDropFromWave(followWave, spawnPosition));
			}
		}
	}

	private IEnumerator WaitDropFromWave(FollowWave followWave, Vector3 targetPosition)
	{
		while (math.dot(WaterSurface.Instance.WaveDirection, followWave.transform.position - targetPosition) > 0)
		{
			yield return null;
		}

		followWave.enabled = false;
	}

	private void PruneDeadObjects()
	{
		for (int i = spawnedObjects.Count - 1; i >= 0; i--)
		{
			if (spawnedObjects[i] == null)
			{
				spawnedObjects.RemoveAt(i);
			}
		}
	}

	private void OnDisable()
	{
		WaterSurface.Instance.OnNewWaveStart.RemoveListener(OnWaveStart);
	}

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
