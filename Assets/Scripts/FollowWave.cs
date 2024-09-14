using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteInEditMode, RequireComponent(typeof(Rigidbody))]
public class FollowWave : MonoBehaviour
{
	[SerializeField] private float maxOffset = 1;
	
	private Rigidbody rBody;
	private float offset;

	private void Awake()
	{
		TryGetComponent(out rBody);
		offset = Random.Range(-maxOffset, maxOffset);
		
		Vector3 vector = WaterSurface.Instance.GetVectorToWaveFront(transform.position);
		transform.position += vector + WaterSurface.Instance.WaveDirection * offset;
	}

	private void FixedUpdate()
    {
	    Vector3 vector = WaterSurface.Instance.GetVectorToWaveFront(rBody.position);
	    rBody.position += vector + WaterSurface.Instance.WaveDirection * offset;
    }
}
