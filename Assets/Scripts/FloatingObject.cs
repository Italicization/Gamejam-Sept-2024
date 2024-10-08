using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FloatingObject : MonoBehaviour
{
	const float WATER_DENSITY = 1000f;
	
	[Range(0, 1)]
	[SerializeField] private float boundsVolumeOccupies = 0.5f;
	[Tooltip("If true object will neither fall nor rise when fully submerged in water")]
	[SerializeField] bool forceNeutralBuoyancy = false;
	[SerializeField] private float floatingAngularDrag = 10;
	[SerializeField] private float floatingDrag = 5;
	[Range(0, 1)]
	[SerializeField] private float maxOffCenter = 1;
	
	private Renderer renderer;
	private Rigidbody rBody;
	private float initialDrag;
	private float initialAngularDrag;
	private float volume;
	private float currentDisplacement;

	private void Awake()
	{
		renderer = GetComponentInChildren<Renderer>();
		MeshFilter filter = GetComponentInChildren<MeshFilter>();

		volume = GetObjectVolume();
		
		TryGetComponent(out rBody);

		initialDrag = rBody.drag;
		initialAngularDrag = rBody.angularDrag;
	}

	private float GetObjectVolume()
	{
		Bounds bounds = GetBounds();
		Vector3 scale = transform.localScale;
		return bounds.size.x * bounds.size.y * bounds.size.z * scale.x * scale.y * scale.z;

		Bounds GetBounds()
		{
			MeshFilter filter = GetComponentInChildren<MeshFilter>();
			if (filter != null)
				return filter.sharedMesh.bounds;
			SkinnedMeshRenderer skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
			if (skinnedMeshRenderer != null)
				return skinnedMeshRenderer.sharedMesh.bounds;
			return new Bounds(Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f));
		}
	}

	private void FixedUpdate()
	{
		Bounds bounds = renderer.bounds;
		float waterHeightFrontRight = WaterSurface.Instance.GetHeightAtPosition(bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z));
		float waterHeightFrontLeft = WaterSurface.Instance.GetHeightAtPosition(bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z));
		float waterHeightBottomRight = WaterSurface.Instance.GetHeightAtPosition(bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z));
		float waterHeightBottomLeft = WaterSurface.Instance.GetHeightAtPosition(bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z));
		
		float displacedFrontRight = math.saturate((waterHeightFrontRight - bounds.center.y) / bounds.size.y);
		float displacedFrontLeft = math.saturate((waterHeightFrontLeft - bounds.center.y) / bounds.size.y);
		float displacedBottomRight = math.saturate((waterHeightBottomRight - bounds.center.y) / bounds.size.y);
		float displacedBottomLeft = math.saturate((waterHeightBottomLeft - bounds.center.y) / bounds.size.y);

		float totalDisplacement = displacedBottomRight + displacedBottomLeft + displacedFrontRight + displacedFrontLeft;
		currentDisplacement = totalDisplacement / 4.0f;
		float averageWaterLevel = (waterHeightFrontRight + waterHeightFrontLeft + waterHeightBottomRight + waterHeightBottomLeft) / 4.0f;
		float maxDisplacement = math.max(math.max(displacedBottomRight, displacedBottomLeft), math.max(displacedFrontRight, displacedFrontLeft));
		float2 forceCenter = new float2(displacedBottomRight + displacedFrontRight, displacedFrontLeft + displacedFrontRight) / totalDisplacement;

		forceCenter = math.select(forceCenter, 0, math.isnan(forceCenter));
		forceCenter = math.lerp(0.5f, forceCenter, maxOffCenter);
		
		float force;
		if (forceNeutralBuoyancy)
			force = -Physics.gravity.y * rBody.mass * currentDisplacement;
        else
			force = WATER_DENSITY * volume * boundsVolumeOccupies * currentDisplacement * -Physics.gravity.y;
		Vector3 position = bounds.min + new Vector3(forceCenter.x * bounds.size.x, 0.5f * bounds.size.y, forceCenter.y * bounds.size.z);
		
		Debug.DrawRay(position, new Vector3(0, force, 0), Color.red);
		Debug.DrawLine(new Vector3(position.x, averageWaterLevel, position.z), new Vector3(position.x, bounds.min.y, position.z), Color.green);
		
		rBody.AddForceAtPosition(new Vector3(0, force, 0), position);
		rBody.angularDrag = math.lerp(initialAngularDrag, floatingAngularDrag, currentDisplacement);
		rBody.drag = math.lerp(initialDrag, floatingDrag, maxDisplacement);
	}
}
