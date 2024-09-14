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
	[SerializeField] private float floatingAngularDrag = 10;
	[SerializeField] private float floatingDrag = 5;
	[Range(0, 1)]
	[SerializeField] private float maxOffCenter = 1;
	
	private Renderer renderer;
	private Rigidbody rBody;
	private float initialDrag;
	private float initialAngularDrag;
	private float volume;

	private void Awake()
	{
		renderer = GetComponentInChildren<Renderer>();
		MeshFilter filter = GetComponentInChildren<MeshFilter>();
		
		Bounds meshBounds = filter.sharedMesh.bounds;
		Vector3 scale = transform.localScale;
		volume = meshBounds.size.x * meshBounds.size.y * meshBounds.size.z * scale.x * scale.y * scale.z;
		
		TryGetComponent(out rBody);

		initialDrag = rBody.drag;
		initialAngularDrag = rBody.angularDrag;
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
		float averageDisplacement = totalDisplacement / 4.0f;
		float averageWaterLevel = (waterHeightFrontRight + waterHeightFrontLeft + waterHeightBottomRight + waterHeightBottomLeft) / 4.0f;
		float maxDisplacement = math.max(math.max(displacedBottomRight, displacedBottomLeft), math.max(displacedFrontRight, displacedFrontLeft));
		float2 forceCenter = new float2(displacedBottomRight + displacedFrontRight, displacedFrontLeft + displacedFrontRight) / totalDisplacement;

		forceCenter = math.select(forceCenter, 0, math.isnan(forceCenter));
		forceCenter = math.lerp(0.5f, forceCenter, maxOffCenter);
		
		float force = WATER_DENSITY * volume * boundsVolumeOccupies * averageDisplacement * -Physics.gravity.y;
		Vector3 position = bounds.min + new Vector3(forceCenter.x * bounds.size.x, 0.5f * bounds.size.y, forceCenter.y * bounds.size.z);
		
		Debug.DrawRay(position, new Vector3(0, force, 0), Color.red);
		Debug.DrawLine(new Vector3(position.x, averageWaterLevel, position.z), new Vector3(position.x, bounds.min.y, position.z), Color.green);
		
		rBody.AddForceAtPosition(new Vector3(0, force * Time.fixedDeltaTime, 0), position);
		rBody.angularDrag = math.lerp(initialAngularDrag, floatingAngularDrag, averageDisplacement);
		rBody.drag = math.lerp(initialDrag, floatingDrag, maxDisplacement);
	}
}
