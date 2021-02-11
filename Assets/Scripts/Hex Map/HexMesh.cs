using Geometry;
using Neitron;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;



namespace Neitron
{


	public static class DictionaryExtension
	{
		public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
		{
			key = tuple.Key;
			value = tuple.Value;
		}
	}



	class Vector3CoordComparer : IEqualityComparer<Vector3>
	{
		public bool Equals(Vector3 a, Vector3 b)
		{
			if (Mathf.Abs(a.x - b.x) > 0.001) return false;
			if (Mathf.Abs(a.y - b.y) > 0.001) return false;
			if (Mathf.Abs(a.z - b.z) > 0.001) return false;

			return true; //indeed, very close
		}

		public int GetHashCode(Vector3 obj)
		{
			//a cruder than default comparison, allows to compare very close-vector3's into same hash-code.
			return Math.Round(obj.x, 3).GetHashCode()
				 ^ Math.Round(obj.y, 3).GetHashCode() << 2
				 ^ Math.Round(obj.z, 3).GetHashCode() >> 2;
		}
	}



	public class NavMesh
	{


		private List<NavChunk> _chunks;
		// TODO: key is Vector3 which is the middle point of an edge, I need to replace it with Edge class
		private Dictionary<Vector3, HashSet<Triangle>> _adjuscentTriangles;

		

		public NavMesh()
		{
			_chunks = new List<NavChunk>();
			_adjuscentTriangles = new Dictionary<Vector3, HashSet<Triangle>>(new Vector3CoordComparer());
		}


		public void Add(NavChunk chunk)
		{
			_chunks.Add(chunk);
		}


		internal void Add(NavChunk chunk, HashSet<Triangle> triangles)
		{
			foreach (var t in triangles)
			{
				AddTriangleToLinkedList(t);
			}
		}


		private void AddTriangleToLinkedList(Triangle t)
		{
			// By AB edge
			if (_adjuscentTriangles.TryGetValue(t.ab, out var tris))
			{
				AddTriangleNeighbors(t, tris);
				tris.Add(t);
			}
			else
				_adjuscentTriangles.Add(t.ab, new HashSet<Triangle> { t });

			// By BC edge
			if (_adjuscentTriangles.TryGetValue(t.bc, out tris))
			{
				AddTriangleNeighbors(t, tris);
				tris.Add(t);
			}
			else
				_adjuscentTriangles.Add(t.bc, new HashSet<Triangle> { t });

			// By CA edge
			if (_adjuscentTriangles.TryGetValue(t.ca, out tris))
			{
				AddTriangleNeighbors(t, tris);
				tris.Add(t);
			}
			else
				_adjuscentTriangles.Add(t.ca, new HashSet<Triangle> { t });
		}


		private void AddTriangleNeighbors(Triangle t, HashSet<Triangle> neighbors)
		{
			foreach (var n in neighbors)
			{
				t.neighbours.Add(n);
				n.neighbours.Add(t);
			}
		}


		internal void Clear()
		{
			foreach (var chunk in _chunks)
			{
				chunk.Clear();
			}
			_chunks.Clear();
		}


	}



	public class NavChunk
	{


		private HashSet<Triangle> _triangles;
		private NavMesh _navMesh;



		public NavChunk(NavMesh navMesh)
		{
			_navMesh = navMesh;
			_triangles = new HashSet<Triangle>();
		}


		public void Apply()
		{
			_navMesh.Add(this, _triangles);
		}


		public void Clear()
		{
			// It may have references to triangles in neighbor chunks
			foreach (var t in _triangles)
			{
				// We just remove the trianfle from all its neighbors
				foreach (var n in t.neighbours)
				{
					n.neighbours.Remove(t);
				}
			}
			// No need to remove neighbor refs from this triangles cuz we delete all of them
			_triangles.Clear();
		}


		public void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
		{
			var triangle = Triangle.Create(HexMetrics.Perturb(v0), HexMetrics.Perturb(v1), HexMetrics.Perturb(v2));
			_triangles.Add(triangle);
		}


		public void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
		{
			AddTriangle(v0, v2, v1);
			AddTriangle(v1, v2, v3);
		}


		internal bool Intersect(Ray ray, out RaycastHit hit, out Triangle triangle)
		{
			hit = default;
			triangle = default;
			foreach (var t in _triangles)
			{
				//DebugExtension.DrawTriangle(t, Color.white * 0.2f, 0.015f);
				if (t.Intersect(ray, out hit, true))
				{
					// TODO: We overr
					triangle = t;
					return true;
				}
			}

			if (triangle != null)
				return true;

			return false;
		}


	}


}



[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{


	[SerializeField] private bool _isUseCellData;
	[SerializeField] private bool _isUseCollider;
	
	[SerializeField] private bool _isUseUv2;
	[SerializeField] private bool _isUseUv;


	[SerializeField, HideInInspector] private Mesh _mesh;
	[SerializeField, HideInInspector] private MeshCollider _meshCollider;


	[NonSerialized] List<Vector3> _cellIndices;
	[NonSerialized] List<Color> _cellWeights;
	[NonSerialized] List<Vector3> _vertices;
	[NonSerialized] List<int> _triangles;
	[NonSerialized] List<Vector2> _uvs2;
	[NonSerialized] List<Vector2> _uvs;



	public Bounds bounds => _mesh.bounds;


	public void Init()
	{
		_mesh = new Mesh
		{
			name = "Hex Mesh"
		};

		GetComponent<MeshFilter>().mesh = _mesh;

		if (_isUseCollider)
		{
			_meshCollider = gameObject.AddComponent<MeshCollider>();
		}
	}
	


	public void AddTriangleUv(Vector2 uv0, Vector2 uv1, Vector2 uv2)
	{
		_uvs.Add(uv0);
		_uvs.Add(uv1);
		_uvs.Add(uv2);
	}


	public void AddTriangleUv2(Vector2 uv0, Vector2 uv1, Vector2 uv2)
	{
		_uvs2.Add(uv0);
		_uvs2.Add(uv1);
		_uvs2.Add(uv2);
	}


	public void AddTriangleCellData(
		Vector3 indices, 
		Color weights1,
		Color weights2,
		Color weights3
		)
	{
		_cellIndices.Add(indices);
		_cellIndices.Add(indices);
		_cellIndices.Add(indices);
		_cellWeights.Add(weights1);
		_cellWeights.Add(weights2);
		_cellWeights.Add(weights3);
	}


	public void AddTriangleCellData(Vector3 indices, Color weights)
	{
		AddTriangleCellData(indices, weights, weights, weights);
	}


	public void AddTriangleUnperturbed(Vector3 v0, Vector3 v1, Vector3 v2)
	{
		var vertexIndex = _vertices.Count;
		_vertices.Add(v0);
		_vertices.Add(v1);
		_vertices.Add(v2);
		_triangles.Add(vertexIndex);
		_triangles.Add(vertexIndex + 1);
		_triangles.Add(vertexIndex + 2);
	}


	public void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
	{
		var vertexIndex = _vertices.Count;
		_vertices.Add(HexMetrics.Perturb(v0));
		_vertices.Add(HexMetrics.Perturb(v1));
		_vertices.Add(HexMetrics.Perturb(v2));
		_triangles.Add(vertexIndex);
		_triangles.Add(vertexIndex + 1);
		_triangles.Add(vertexIndex + 2);
	}


	public void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
	{
		var vertexIndex = _vertices.Count;
		_vertices.Add(HexMetrics.Perturb(v0));
		_vertices.Add(HexMetrics.Perturb(v1));
		_vertices.Add(HexMetrics.Perturb(v2));
		_vertices.Add(HexMetrics.Perturb(v3));
		_triangles.Add(vertexIndex);
		_triangles.Add(vertexIndex + 2);
		_triangles.Add(vertexIndex + 1);
		_triangles.Add(vertexIndex + 1);
		_triangles.Add(vertexIndex + 2);
		_triangles.Add(vertexIndex + 3);
	}


	public void AddQuadUnperturbed(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
	{
		var vertexIndex = _vertices.Count;
		_vertices.Add(v0);
		_vertices.Add(v1);
		_vertices.Add(v2);
		_vertices.Add(v3);
		_triangles.Add(vertexIndex);
		_triangles.Add(vertexIndex + 2);
		_triangles.Add(vertexIndex + 1);
		_triangles.Add(vertexIndex + 1);
		_triangles.Add(vertexIndex + 2);
		_triangles.Add(vertexIndex + 3);
	}


	public void AddQuadUv(Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
	{
		_uvs.Add(uv0);
		_uvs.Add(uv1);
		_uvs.Add(uv2);
		_uvs.Add(uv3);
	}


	public void AddQuadUv(float uMin, float uMax, float vMin, float vMax)
	{
		_uvs.Add(new Vector2(uMin, vMin));
		_uvs.Add(new Vector2(uMax, vMin));
		_uvs.Add(new Vector2(uMin, vMax));
		_uvs.Add(new Vector2(uMax, vMax));
	}


	public void AddQuadUv2(Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
	{
		_uvs2.Add(uv0);
		_uvs2.Add(uv1);
		_uvs2.Add(uv2);
		_uvs2.Add(uv3);
	}


	public void AddQuadUv2(float uMin, float uMax, float vMin, float vMax)
	{
		_uvs2.Add(new Vector2(uMin, vMin));
		_uvs2.Add(new Vector2(uMax, vMin));
		_uvs2.Add(new Vector2(uMin, vMax));
		_uvs2.Add(new Vector2(uMax, vMax));
	}


	public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2, Color weights3, Color weights4)
	{
		_cellIndices.Add(indices);
		_cellIndices.Add(indices);
		_cellIndices.Add(indices);
		_cellIndices.Add(indices);
		_cellWeights.Add(weights1);
		_cellWeights.Add(weights2);
		_cellWeights.Add(weights3);
		_cellWeights.Add(weights4);
	}


	public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2)
	{
		AddQuadCellData(indices, weights1, weights1, weights2, weights2);
	}


	public void AddQuadCellData(Vector3 indices, Color weights)
	{
		AddQuadCellData(indices, weights, weights, weights, weights);
	}




	public void Clear()
	{
		if (_mesh == null)
			return;

		_mesh.Clear();
		_vertices = ListPool<Vector3>.Get();

		if (_isUseCellData)
		{
			_cellWeights = ListPool<Color>.Get();
			_cellIndices = ListPool<Vector3>.Get();
		}

		if (_isUseUv)
		{
			_uvs = ListPool<Vector2>.Get();
		}

		if (_isUseUv2)
		{
			_uvs2 = ListPool<Vector2>.Get();
		}

		_triangles = ListPool<int>.Get();
	}


	public void Apply()
	{
		_mesh.SetVertices(_vertices);
		ListPool<Vector3>.Refuse(_vertices);

		if (_isUseCellData)
		{
			_mesh.SetColors(_cellWeights);
			ListPool<Color>.Refuse(_cellWeights);
			_mesh.SetUVs(2, _cellIndices);
			ListPool<Vector3>.Refuse(_cellIndices);
		}

		if (_isUseUv)
		{
			_mesh.SetUVs(0, _uvs);
			ListPool<Vector2>.Refuse(_uvs);
		}

		if (_isUseUv2)
		{
			_mesh.SetUVs(1, _uvs2);
			ListPool<Vector2>.Refuse(_uvs2);
		}

		_mesh.SetTriangles(_triangles, 0);
		ListPool<int>.Refuse(_triangles);

		_mesh.RecalculateNormals();

		if (_isUseCollider)
		{
			_meshCollider.sharedMesh = _mesh;
		}
	}


}
