using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class DistanceDespawn : MonoBehaviour
{
	[SerializeField] private float distance = 100f;

	private Camera camera;
	private PrefabPool.PoolItem poolItem;

	private void Awake()
	{
		camera = Camera.main;
		TryGetComponent(out poolItem);
	}

	private void Update()
	{
		if (math.distancesq(transform.position, camera.transform.position) > distance * distance)
		{
			if (poolItem != null)
				poolItem.ReturnToPool();
			else
				Destroy(gameObject);
		}
	}
}
