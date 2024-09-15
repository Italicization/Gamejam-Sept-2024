using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomSpawner : MonoBehaviour
{
	[SerializeField] private GameObject[] prefabVariants;
	[SerializeField] private int spawnCount;
	[SerializeField] private Rect rect;
	[SerializeField] private float maxPlayerDistance = float.PositiveInfinity;
	[SerializeField] private float minPlayerDistance = 0;
	[SerializeField] private bool arriveOnWave = true;
	[Header("Debug")]
	[SerializeField] private int currentAlive;
	[SerializeField] private int currentPending;

	private List<GameObject> spawnedObjects = new ();
	private List<(Vector3 position, int variant)> availableSpawnPositions = new ();
	private Transform player;
	
	public Rect WorldRect => new Rect((float2)rect.position + ((float3)transform.position).xz - (float2)rect.size * 0.5f, rect.size);

	private void OnValidate()
	{
		minPlayerDistance = math.min(minPlayerDistance, maxPlayerDistance);
	}

	private void Awake()
	{
		player = FindAnyObjectByType<CharacterController>()?.transform;
	}

	private void OnEnable()
	{
		if (arriveOnWave)
			WaterSurface.Instance.OnNewWaveStart.AddListener(SpawnRound);
		else
			StartCoroutine(SpawnLoop());
	}

	private IEnumerator SpawnLoop()
	{
		while (enabled)
		{
			yield return new WaitForSeconds(0.25f);
			SpawnRound();
		}
	}

	private void SpawnRound()
	{
		PruneDeadObjects();
		GenerateSpawnPositions();
		SpawnAvailableObjects();
		
		currentAlive = spawnedObjects.Count;
		currentPending = availableSpawnPositions.Count;
	}

	private void GenerateSpawnPositions()
	{
		for (int i = spawnedObjects.Count + availableSpawnPositions.Count; i < spawnCount; i++)
		{
			float3 spawnPosition = new Vector3(Random.Range(WorldRect.xMin, WorldRect.xMax), 0, Random.Range(WorldRect.yMin, WorldRect.yMax));
			int variant = Random.Range(0, prefabVariants.Length);
			if (prefabVariants[variant].TryGetComponent(out Rigidbody rBody) && rBody.isKinematic)
			{
				// Place on ground
				if (Physics.Raycast(spawnPosition + new float3(0, 10, 0), Vector3.down, out RaycastHit hit, 100))
				{
					spawnPosition = hit.point;
				}
			}
			
			// Predetermine spawn positions that are used when player is in range so that all don't spawn next to the player
			availableSpawnPositions.Add((spawnPosition, variant));
		}
	}
	
	private void SpawnAvailableObjects()
	{
		for (int i = availableSpawnPositions.Count - 1; i >= 0; i--)
		{
			(float3 spawnPosition, int variant) = availableSpawnPositions[i];

			float dist = math.distancesq(spawnPosition.xz, new float3(player.position).xz);
			if (dist > maxPlayerDistance * maxPlayerDistance || dist < minPlayerDistance * minPlayerDistance)
				continue;
			
			GameObject go = Instantiate(prefabVariants[variant], spawnPosition, Random.rotation);
			spawnedObjects.Add(go);
			availableSpawnPositions.RemoveAt(i);

			if (go.TryGetComponent(out FollowWave followWave))
			{
				if (arriveOnWave)
					StartCoroutine(WaitDropFromWave(followWave, spawnPosition));
				else
					followWave.enabled = false;
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
		if (WaterSurface.Instance != null)
		{
			WaterSurface.Instance.OnNewWaveStart.RemoveListener(SpawnRound);
		}
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

		Gizmos.color = Color.green;
		for (int i = 0; i < spawnedObjects.Count; i++)
			if (spawnedObjects[i] != null)
				Gizmos.DrawWireSphere(spawnedObjects[i].transform.position, 0.2f);		
		Gizmos.color = Color.red;
		for (int i = 0; i < availableSpawnPositions.Count; i++)
			Gizmos.DrawWireSphere(availableSpawnPositions[i].position, 0.2f);
		
		if (maxPlayerDistance < float.PositiveInfinity)
		{
			if (player == null)
				player = FindAnyObjectByType<CharacterController>()?.transform;

			if (player != null)
			{
				#if UNITY_EDITOR
				Handles.color = Color.red;
				Handles.DrawWireDisc(player.position, Vector3.up, maxPlayerDistance);
				if (minPlayerDistance > 0)
					Handles.DrawWireDisc(player.position, Vector3.up, minPlayerDistance);
				#endif
			}
		}
	}
}
