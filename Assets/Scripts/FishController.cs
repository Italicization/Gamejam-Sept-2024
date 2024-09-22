using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class FishController : MonoBehaviour
{
	[SerializeField] private float uprightTorque = 1;
	[SerializeField] private float forwardForce = 1;
	[SerializeField] private float upwardForce = 1;
	[SerializeField] private float turnTorque = 1;
	[SerializeField] private float turnRandomness = 1;
	[SerializeField] private float turnRandomnessDetail = 1;

	[Range(0, 1)]
	[SerializeField] private float turnRandomnessDetailMagnitude = 1;
	
	private Rigidbody rBody;
	private float noiseOffset;
	private int groundLayer;
	private float targerNormalizedDepth = 0.5f;
	private float currentNormalizedDepth = 0.5f;
	private bool isUnderWater;

	private void Awake()
	{
		rBody = GetComponent<Rigidbody>();
		noiseOffset = Random.Range(-10000f, 10000f);
		groundLayer = LayerMask.GetMask("Ground");
		targerNormalizedDepth = GetNormalizedDepth();
	}

	private void OnEnable()
	{
		StartCoroutine(UpdateDepthLoop());
	}

	private void FixedUpdate()
	{
		if (!isUnderWater)
			return;
		
		rBody.AddTorque(GetUprightTorque());
		rBody.AddForce(transform.forward * forwardForce);
		rBody.AddForce(Vector3.up * (upwardForce * math.sign(targerNormalizedDepth - currentNormalizedDepth)));

		rBody.AddTorque(Vector3.up * (GetTurnAmount() * turnTorque));
	}

	IEnumerator UpdateDepthLoop()
	{
		while (enabled)
		{
			currentNormalizedDepth = GetNormalizedDepth();
			isUnderWater = currentNormalizedDepth < 1;
			yield return new WaitForSeconds(0.25f);
		}
	}

	private float GetNormalizedDepth()
	{
		float3 position = transform.position;
		float groundHeight = -20;
		if (Physics.Raycast(position + new float3(0, 2, 0), Vector3.down, out RaycastHit hit, 100, groundLayer))
			groundHeight = hit.point.y;

		float waterHeight = WaterSurface.Instance.GetHeightAtPosition(position);
		if (waterHeight < groundHeight)
			return 1;
		return math.unlerp(groundHeight, waterHeight, position.y);
	}

	private float GetTurnAmount()
	{
		float mainTurnNoise = Mathf.PerlinNoise(turnRandomness * Time.time, noiseOffset);
		float detailTurnNoise = Mathf.PerlinNoise(turnRandomnessDetail * Time.time, noiseOffset);
		float turnNoise = (mainTurnNoise * (1 - turnRandomnessDetailMagnitude) + detailTurnNoise * turnRandomnessDetailMagnitude) * 2 - 1;
		return turnNoise;
	}

	private Vector3 GetUprightTorque()
	{
		return Vector3.Cross(transform.up, Vector3.up) * uprightTorque;
		//return -transform.right.y * uprightTorque * transform.forward;
	}

	private void OnDrawGizmosSelected()
	{
		#if UNITY_EDITOR
		UnityEditor.Handles.color = Color.cyan;
		UnityEditor.Handles.DrawWireArc(transform.position, Vector3.up, transform.forward, GetTurnAmount() * 90, 0.5f);
		
		#endif
	}
}
