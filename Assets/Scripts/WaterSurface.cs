using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class WaterSurface : MonoBehaviour
{
	private static readonly int vertexBufferId = Shader.PropertyToID("_VertexBuffer");
	
	[SerializeField] private Vector2 size = new(10, 10);
	[SerializeField] int2 resolution = new(100, 100);
	[SerializeField] private float waveFrequency = 1;
	[SerializeField] private float waveHeight = 1;
	[SerializeField] ComputeShader waterSimulationShader;
	[SerializeField] private Material material;
	
	private Mesh mesh;
	private int groupSize;
	private GraphicsBuffer vertexBuffer;

	private void OnValidate()
	{
		CreateMesh();
		SetComputeProperties();
	}

	private void Awake()
	{
		CreateMesh();
		
		waterSimulationShader.GetKernelThreadGroupSizes(0, out uint x, out _, out _);
		groupSize = (int)x;
		
		SetComputeProperties();
	}

	private void SetComputeProperties()
	{
		waterSimulationShader.SetInt("_ResolutionX", resolution.x);
		waterSimulationShader.SetInt("_ResolutionY", resolution.y);
		waterSimulationShader.SetFloat("_Amplitude", waveHeight);
		waterSimulationShader.SetFloat("_Frequency", waveFrequency);
	}

	private void CreateMesh()
	{
		CleanupMesh();
		
		mesh = MeshUtility.CreatePlaneMesh(size.x, size.y, resolution.x, resolution.y);

		waterSimulationShader.SetInt("_VertexStride", mesh.GetVertexBufferStride(0));
		waterSimulationShader.SetInt("_PositionHeightOffset", mesh.GetVertexAttributeOffset(VertexAttribute.Position) + sizeof(float));
		
		MeshFilter meshFilter = gameObject.GetComponentInChildren<MeshFilter>();
		if (meshFilter != null)
			meshFilter.sharedMesh = mesh;
	}

	private void Update()
	{
		int2 vertexCount = resolution + 1;
		int2 dispatch = (vertexCount + groupSize - 1) / groupSize;
		waterSimulationShader.SetBuffer(0, vertexBufferId, GetVertexBuffer());
		waterSimulationShader.Dispatch(0, dispatch.x, dispatch.y, 1);

		RenderParams renderParams = new RenderParams(material);
		
		Graphics.RenderMesh(renderParams, mesh, 0, transform.localToWorldMatrix);
	}

	private GraphicsBuffer GetVertexBuffer()
	{
		if (vertexBuffer == null || !vertexBuffer.IsValid())
			vertexBuffer = mesh.GetVertexBuffer(0);
		return vertexBuffer;
	}

	private void CleanupMesh()
	{
		if (mesh != null)
		{
			if (!Application.isPlaying)
				DestroyImmediate(mesh);
			else
				Destroy(mesh);
		}

		if (vertexBuffer != null)
		{
			vertexBuffer.Dispose();
		}
	}

	private void OnDestroy()
	{
		CleanupMesh();
	}
}