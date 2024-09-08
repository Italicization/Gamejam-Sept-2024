using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshUtility
{
    public static Mesh CreatePlaneMesh(float width, float height, int widthSegments, int heightSegments)
    {
	    Mesh mesh = new Mesh()
	    {
		    name = "Plane",
	    };

	    int vertexCount = (widthSegments + 1) * (heightSegments + 1);
	    mesh.SetVertexBufferParams(vertexCount,
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
			new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2));

	    NativeArray<float3> vertices = new NativeArray<float3>(vertexCount, Allocator.Temp);
	    NativeArray<float2> uvs = new NativeArray<float2>(vertexCount, Allocator.Temp);
		
		float widthHalf = width / 2;
		float heightHalf = height / 2;

		float segmentWidth = width / widthSegments;
		float segmentHeight = height / heightSegments;

		for (int y = 0; y <= heightSegments; y++)
		{
			for (int x = 0; x <= widthSegments; x++)
			{
				int index = y * (widthSegments + 1) + x;
				vertices[index] = new float3(-widthHalf + x * segmentWidth, 0, heightHalf - y * segmentHeight);
				uvs[index] = new float2((float)x / widthSegments, (float)y / heightSegments);
			}
		}
		
		mesh.SetVertices(vertices);
		mesh.SetUVs(0, uvs);

		vertices.Dispose();
		uvs.Dispose();

		int indexCount = widthSegments * heightSegments * 6;
		mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
		NativeArray<uint> triangles = new NativeArray<uint>(indexCount, Allocator.Temp);

		for (int y = 0; y < heightSegments; y++)
		{
			for (int x = 0; x < widthSegments; x++)
			{
				int index = (y * widthSegments + x) * 6;
				triangles[index + 0] = (uint)(y * (widthSegments + 1) + x + 1);
				triangles[index + 1] = (uint)((y + 1) * (widthSegments + 1) + x);
				triangles[index + 2] = (uint)(y * (widthSegments + 1) + x);

				triangles[index + 3] = (uint)((y + 1) * (widthSegments + 1) + x + 1);
				triangles[index + 4] = (uint)((y + 1) * (widthSegments + 1) + x);
				triangles[index + 5] = (uint)(y * (widthSegments + 1) + x + 1);
			}
		}

		mesh.SetIndexBufferData(triangles, 0, 0, indexCount);
		mesh.subMeshCount = 1;
		mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length));
		triangles.Dispose();
		mesh.vertexBufferTarget = GraphicsBuffer.Target.Vertex | GraphicsBuffer.Target.Raw;
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		return mesh;
	}

    [StructLayout(LayoutKind.Sequential)]
	private struct Vertex
	{
		public float3 position;
		public float3 normal;
		public float2 uv;
	}
}
