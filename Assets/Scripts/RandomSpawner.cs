using System.Collections;
using System.Collections.Generic;
using MyBox;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomSpawner : MonoBehaviour
{
	[SerializeField] private GameObject[] prefabVariants;
	[SerializeField] private SpawnAmount spawnCountMode;
    [ConditionalField(nameof(spawnCountMode), false, SpawnAmount.Count)]
	[SerializeField] private int spawnCount;
    [ConditionalField(nameof(spawnCountMode), false, SpawnAmount.Density)]
	[SerializeField] private float spawnDensity;
	[SerializeField] private Rect rect;
	[SerializeField] private float maxPlayerDistance = float.PositiveInfinity;
	[SerializeField] private float minPlayerDistance = 0;
	[SerializeField] private bool arriveOnWave = true;
	[SerializeField] private SpawnMode whereToSpawn;
	[ConditionalField(nameof(whereToSpawn), false, SpawnMode.InWater)]
	[SerializeField] private float waterSpawnMinDepth = 1;
	[SerializeField] private float groundOffset = 0.1f;
	[Header("Debug")]
	[SerializeField] private int currentAlive;
	[SerializeField] private int currentPending;

	private HashSet<GameObject> spawnedObjects = new ();
	private List<Vector3> availableSpawnPositions = new ();
	private List<PrefabPool> pools = new ();
	private Transform player;
	private int groundLayers;

	public enum SpawnMode
	{
		Anywhere,
		OnGround,
		InWater,
	}

	public enum SpawnAmount
	{
		Count, 
		Density,
	}
	
	public Rect WorldRect => new Rect((float2)rect.position + ((float3)transform.position).xz - (float2)rect.size * 0.5f, rect.size);

	private void OnValidate()
	{
		minPlayerDistance = math.min(minPlayerDistance, maxPlayerDistance);
	}

	private void Awake()
	{
		groundLayers = LayerMask.GetMask("Ground");
		player = FindAnyObjectByType<CharacterController>()?.transform;

		foreach (GameObject prefab in prefabVariants)
		{
			PrefabPool pool = new GameObject(prefab.name + " Pool").AddComponent<PrefabPool>();
			pool.transform.SetParent(transform);
			pool.Init(prefab);
			pools.Add(pool);
		}
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

	private int GetSpawnCount()
	{
		return spawnCountMode switch
		{
			SpawnAmount.Count => spawnCount,
			SpawnAmount.Density => Mathf.RoundToInt(rect.size.x * rect.size.y * spawnDensity),
		};
	}
	
	private void GenerateSpawnPositions()
	{
		int count = GetSpawnCount();
		for (int i = spawnedObjects.Count + availableSpawnPositions.Count; i < count; i++)
		{
			float3 spawnPosition = new Vector3(Random.Range(WorldRect.xMin, WorldRect.xMax), 0, Random.Range(WorldRect.yMin, WorldRect.yMax));
			
			// Predetermine spawn positions that are used when player is in range so that all don't spawn next to the player
			availableSpawnPositions.Add(spawnPosition);
		}
	}
	
	private void SpawnAvailableObjects()
	{
		for (int i = availableSpawnPositions.Count - 1; i >= 0; i--)
		{
			float3 spawnPosition = availableSpawnPositions[i];
			int variant = Random.Range(0, prefabVariants.Length);

			float dist = math.distancesq(spawnPosition.xz, new float3(player.position).xz);
			if (dist > maxPlayerDistance * maxPlayerDistance || dist < minPlayerDistance * minPlayerDistance)
				continue;

			float groundHeight = 0;
			// Place on ground
			if (Physics.Raycast(spawnPosition + new float3(0, 10, 0), Vector3.down, out RaycastHit hit, 100, groundLayers))
			{
				groundHeight = hit.point.y;
			}
			
			if (whereToSpawn == SpawnMode.OnGround || !prefabVariants[variant].TryGetComponent(out Rigidbody rBody) || rBody.isKinematic)
			{
				spawnPosition.y = groundHeight + groundOffset;
			}
			else 
			{
				float waterHeight = WaterSurface.Instance.GetHeightAtPosition(spawnPosition);
				
				// Not deep enough water
				if (whereToSpawn == SpawnMode.InWater && waterHeight - groundHeight < waterSpawnMinDepth)
					continue;
				
				waterHeight = math.max(groundHeight, waterHeight - waterSpawnMinDepth);
				spawnPosition.y = Random.Range(groundHeight + groundOffset, waterHeight - waterSpawnMinDepth);
			}

			PrefabPool.PoolItem item = pools[variant].GetNewItem(spawnPosition, Random.rotation);
			item.OnReturnToPool += OnItemReturnedToPool;
			spawnedObjects.Add(item.gameObject);
			availableSpawnPositions.RemoveAt(i);

			if (item.TryGetComponent(out FollowWave followWave))
			{
				if (arriveOnWave)
					StartCoroutine(WaitDropFromWave(followWave, spawnPosition));
				else
					followWave.enabled = false;
			}
		}
	}

	private void OnItemReturnedToPool(PrefabPool.PoolItem obj)
	{
		spawnedObjects.Remove(obj.gameObject);
		obj.OnReturnToPool -= OnItemReturnedToPool;
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
		spawnedObjects.RemoveWhere(IsDead);

		static bool IsDead(GameObject obj) => obj == null;
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
		foreach (GameObject obj in spawnedObjects)
			if (obj != null)
				Gizmos.DrawWireSphere(obj.transform.position, 0.2f);		

		Gizmos.color = Color.red;
		for (int i = 0; i < availableSpawnPositions.Count; i++)
			Gizmos.DrawWireSphere(availableSpawnPositions[i], 0.2f);
		
		if (maxPlayerDistance < float.PositiveInfinity)
		{
			if (player == null)
				player = FindAnyObjectByType<CharacterController>()?.transform;
				player = FindAnyObjectByType<CharacterController>()?.transform;

			if (player != null)
			{
				#if UNITY_EDITOR
				UnityEditor.Handles.color = Color.red;
				UnityEditor.Handles.DrawWireDisc(player.position, Vector3.up, maxPlayerDistance);
				if (minPlayerDistance > 0)
					UnityEditor.Handles.DrawWireDisc(player.position, Vector3.up, minPlayerDistance);
				#endif
			}
		}
	}
}
