using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class WaterSurface : MonoBehaviour
{
	private static readonly int vertexBufferId = Shader.PropertyToID("_VertexBuffer");
	
	[SerializeField] private Vector2 size = new(10, 10);
	[SerializeField] int2 resolution = new(100, 100);
	[SerializeField] private WaveConstruction[] waves;
	[SerializeField] ComputeShader waterSimulationShader;
	[SerializeField] private Material material;
	[Header("Debug")]
	[SerializeField] private int offset;
	[SerializeField] private float normalScale = 1;
	
	private Mesh mesh;
	private int groupSize;
	private GraphicsBuffer vertexBuffer;
	private ComputeBuffer waveBuffer;

	[Serializable]
	class WaveConstruction
	{
		public float Frequency = 1;
		public float Amplitude;
		public float Speed = 1;
		[Range(0, 360)]public float Angle;
		public float Steepness;
		public bool Active = true;

		public WaveDescription ToDescription()
		{
			if (!Active) 
				return WaveDescription.Default;
			
			return new WaveDescription()
			{
				Frequency = Frequency,
				Amplitude = Amplitude,
				Speed = Speed,
				Direction = new float2(math.cos(math.radians(Angle)), math.sin(math.radians(Angle))),
				Steepness = Steepness,
			};
		}
	}
	
	[StructLayout(LayoutKind.Sequential)]
	struct WaveDescription
	{
		public float Frequency;
		public float Amplitude;
		public float Speed;
		public float2 Direction;
		public float Steepness;

		public static readonly WaveDescription Default = new() { Frequency = 1, Direction = new(1, 0) };
		public static readonly unsafe int Stride = sizeof(WaveDescription);
	}

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

		waveBuffer = new ComputeBuffer(waves.Length, WaveDescription.Stride);
		
		SetComputeProperties();
	}

	private void SetComputeProperties()
	{
		if (waveBuffer == null || !waveBuffer.IsValid() || waveBuffer.count != waves.Length)
		{
			waveBuffer?.Dispose();
			waveBuffer = new ComputeBuffer(waves.Length, WaveDescription.Stride);
		}

		WaveDescription[] waveDescriptions = new WaveDescription[waves.Length];
		for (int i = 0; i < waves.Length; i++)
			waveDescriptions[i] = waves[i].ToDescription();
		waveBuffer.SetData(waveDescriptions);
		
		waterSimulationShader.SetInt("_ResolutionX", resolution.x);
		waterSimulationShader.SetInt("_ResolutionY", resolution.y);
		waterSimulationShader.SetBuffer(0, "_Waves", waveBuffer);
		waterSimulationShader.SetVector("_Size", new Vector4(size.x, size.y)); 
		waterSimulationShader.SetFloat("_NormalScale", normalScale); 
	} 

	private void CreateMesh()
	{
		CleanupMesh();
		
		mesh = MeshUtility.CreatePlaneMesh(size.x, size.y, resolution.x, resolution.y);

		waterSimulationShader.SetInt("_VertexStride", mesh.GetVertexBufferStride(0));
		waterSimulationShader.SetInt("_PositionOffset", mesh.GetVertexAttributeOffset(VertexAttribute.Position));
		waterSimulationShader.SetInt("_NormalOffset", mesh.GetVertexAttributeOffset(VertexAttribute.Normal));
		
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
		waveBuffer?.Dispose();
	}

	private void OnDestroy()
	{
		CleanupMesh();
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;

		GraphicsBuffer vertexBuffer = GetVertexBuffer();

		MeshUtility.Vertex[] vertices = new MeshUtility.Vertex[100];
		vertexBuffer.GetData(vertices, 0, offset, vertices.Length);

		float spacing = size.x / resolution.x;
		
		for (int i = 0; i < vertices.Length; i++)
		{
			Gizmos.DrawWireSphere(vertices[i].position, spacing * 0.5f);
			Gizmos.DrawRay(vertices[i].position, math.normalize(vertices[i].normal));
		}
	}
}