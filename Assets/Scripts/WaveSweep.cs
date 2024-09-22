using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;

/// <summary>
/// Adds a force that makes object be dragged along in the wake of a wave
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WaveSweep : MonoBehaviour
{
	[SerializeField] private float sweepForce;
	
	private Rigidbody rBody;
	private Vector3 lastForceDirection;

	private void Awake()
	{
		rBody = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		lastForceDirection = new Vector3();
		WaterSurface.WaveState state = WaterSurface.Instance.State;
		
		if (state is WaterSurface.WaveState.Dry or WaterSurface.WaveState.Flooded)
			return;

		Vector3 position = rBody.position;
		float height = WaterSurface.Instance.GetHeightAtPosition(position);
		// Must be under water
		if (position.y > height) 
			return;

		Vector3 direction = WaterSurface.Instance.GetVectorToWaveFront(position);
		// Must be on the far side of the wave
		if (Vector3.Dot(direction, WaterSurface.Instance.WaveDirection) < 0)
		{
			lastForceDirection = direction.normalized;
			rBody.AddForce(lastForceDirection * sweepForce);
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (WaterSurface.Instance.State is WaterSurface.WaveState.Flooded or WaterSurface.WaveState.Dry)
			return;
		
		Gizmos.color = Color.yellow;
		Gizmos.DrawRay(transform.position, WaterSurface.Instance.GetVectorToWaveFront(transform.position));
		Gizmos.color = Color.blue;
		Gizmos.DrawRay(transform.position, WaterSurface.Instance.WaveDirection);
	}
}

