using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;



public class DelaunayTriangulation : MonoBehaviour
{


	class Edge
	{
		private static Stack<Edge> _edges = new Stack<Edge>(n * 5);


		public Vector2 a;
		public Vector2 b;

		static Edge()
		{
			for (var i = 0; i < n * 5; i++)
			{
				_edges.Push(new Edge());
			}
		}


		private Edge()
		{
			
		}


		public static Edge Get(Vector2 point1, Vector2 point2)
		{
			Edge edge;
			if (_edges.Count > 0)
			{
				edge = _edges.Pop();
				edge.Init(point1, point2);
				return edge;
			}
			else
			{
				edge = new Edge();
				edge.Init(point1, point2);
			}
			return edge;
		}


		public static void Refuse(Edge edge)
		{
			_edges.Push(edge);
		}


		private void Init(Vector2 a, Vector2 b)
		{
			this.a = a;
			this.b = b;
		}


		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			if (obj.GetType() != GetType()) return false;
			var edge = obj as Edge;

			var samePoints = a == edge.a && b == edge.b;
			var samePointsReversed = a == edge.b && b == edge.a;
			return samePoints || samePointsReversed;
		}


		public override int GetHashCode()
		{
			var hCode = (int)a.x ^ (int)a.y ^ (int)b.x ^ (int)b.y;
			return hCode.GetHashCode();
		}


		public bool IntersectAny(List<Triangle> triangles)
		{
			foreach (var t in triangles)
			{
				if (Intersect(t)) return true;
			}
			return false;
		}


		public bool Intersect(Triangle t)
		{
			if (AreLinesIntersecting(a, b, t.a, t.b, false) ||
			    AreLinesIntersecting(a, b, t.b, t.c, false) ||
			    AreLinesIntersecting(a, b, t.c, t.a, false))
				return true;
			return false;
		}


		public static bool AreLinesIntersecting(Vector2 l1_p1, Vector2 l1_p2, Vector2 l2_p1, Vector2 l2_p2, bool shouldIncludeEndPoints)
		{
			//To avoid floating point precision issues we can add a small value
			float epsilon = 0.00001f;

			bool isIntersecting = false;

			float denominator = (l2_p2.y - l2_p1.y) * (l1_p2.x - l1_p1.x) - (l2_p2.x - l2_p1.x) * (l1_p2.y - l1_p1.y);

			//Make sure the denominator is > 0, if not the lines are parallel
			if (denominator != 0f)
			{
				float u_a = ((l2_p2.x - l2_p1.x) * (l1_p1.y - l2_p1.y) - (l2_p2.y - l2_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;
				float u_b = ((l1_p2.x - l1_p1.x) * (l1_p1.y - l2_p1.y) - (l1_p2.y - l1_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;

				//Are the line segments intersecting if the end points are the same
				if (shouldIncludeEndPoints)
				{
					//Is intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1
					if (u_a >= 0f + epsilon && u_a <= 1f - epsilon && u_b >= 0f + epsilon && u_b <= 1f - epsilon)
					{
						isIntersecting = true;
					}
				}
				else
				{
					//Is intersecting if u_a and u_b are between 0 and 1
					if (u_a > 0f + epsilon && u_a < 1f - epsilon && u_b > 0f + epsilon && u_b < 1f - epsilon)
					{
						isIntersecting = true;
					}
				}
			}

			return isIntersecting;
		}
	}


	class Triangle
	{
		private static Stack<Triangle> _triangles = new Stack<Triangle>(n);
		public Vector2 a;
		public Vector2 b;
		public Vector2 c;

		public Vector2 circumcenter;
		public float radiusSquared;


		static Triangle()
		{
			for (var i = 0; i < n; i++)
			{
				_triangles.Push(new Triangle());
			}
		}


		public bool isCompleted { get; set; } = false;


		public static Triangle Get(Vector2 point1, Vector2 point2, Vector2 point3)
		{
			Triangle triangle;
			if (_triangles.Count > 0)
			{
				triangle = _triangles.Pop();
				triangle.Init(point1, point2, point3);
				return triangle;
			}
			else
			{
				triangle = new Triangle();
				triangle.Init(point1, point2, point3);
			}

			return triangle;
		}


		public static void Refuse(Triangle triangle)
		{
			triangle.isCompleted = false;
			_triangles.Push(triangle);
		}


		private Triangle()
		{
			
		}

		private void Init(Vector2 point1, Vector2 point2, Vector2 point3)
		{
			// In theory this shouldn't happen, but it was at one point so this at least makes sure we're getting a
			// relatively easily-recognized error message, and provides a handy breakpoint for debugging.
			if (point1 == point2 || point1 == point3 || point2 == point3)
			{
				throw new ArgumentException("Must be 3 distinct points");
			}

			a = point1;
			if (IsCounterClockwise(point1, point2, point3))
			{
				b = point2;
				c = point3;
			}
			else
			{
				b = point3;
				c = point2;
			}

			UpdateCircumcircle();
		}


		private bool IsCounterClockwise(Vector2 point1, Vector2 point2, Vector2 point3)
		{
			var result = 
				(point2.x - point1.x) * (point3.y - point1.y) -
				(point3.x - point1.x) * (point2.y - point1.y);
			return result > 0;
		}


		private void UpdateCircumcircle()
		{
			// https://codefound.wordpress.com/2013/02/21/how-to-compute-a-circumcircle/#more-58
			// https://en.wikipedia.org/wiki/Circumscribed_circle
			var p0 = a;
			var p1 = b;
			var p2 = c;
			var dA = p0.x * p0.x + p0.y * p0.y;
			var dB = p1.x * p1.x + p1.y * p1.y;
			var dC = p2.x * p2.x + p2.y * p2.y;

			var aux1 = (dA * (p2.y - p1.y) + dB * (p0.y - p2.y) + dC * (p1.y - p0.y));
			var aux2 = -(dA * (p2.x - p1.x) + dB * (p0.x - p2.x) + dC * (p1.x - p0.x));
			var div = (2 * (p0.x * (p2.y - p1.y) + p1.x * (p0.y - p2.y) + p2.x * (p1.y - p0.y)));

			if (Math.Abs(div) < 0.00000001f)
			{
				throw new DivideByZeroException();
			}

			var center = new Vector2(aux1 / div, aux2 / div);
			circumcenter = center;
			radiusSquared = (center.x - p0.x) * (center.x - p0.x) + (center.y - p0.y) * (center.y - p0.y);
		}


		public bool IsPointInsideCircumcircle(Vector2 point)
		{
			var dSquared = (point.x - circumcenter.x) * (point.x - circumcenter.x) +
			                (point.y - circumcenter.y) * (point.y - circumcenter.y);
			return dSquared < radiusSquared;
		}


		public static Triangle GetUnderPointer(List<Triangle> triangles, Vector2 point)
		{
			foreach (var triangle in triangles)
			{
				if (triangle.IsPointInsideCircumcircle(point))
				{
					if (triangle.IsPointerInside(point, triangle.a, triangle.b, triangle.c))
						return triangle;
				}
			}
			return null;
		}


		static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
		{
			return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
		}


		private bool IsPointerInside(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
		{
			bool hasNeg, hasPos;

			var d1 = Sign(pt, v1, v2);
			var d2 = Sign(pt, v2, v3);
			var d3 = Sign(pt, v3, v1);

			hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
			hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

			return !(hasNeg && hasPos);
		}


	}


	private Camera _camera;
	private Plane _floorPlane;

	private List<Vector2> _vertices = new List<Vector2>(n);
	private List<Triangle> _triangles = new List<Triangle>(n);
	private HashSet<Edge> _edges = new HashSet<Edge>();
	public const int n = 10000;

	private void Awake()
	{
		_camera = Camera.main;
		_floorPlane = new Plane(Vector3.up, 0f);
	}


	private void Start()
	{
		StartCoroutine(BowyerWatson());
		//BowyerWatson();
	}


	// https://www.newcastle.edu.au/__data/assets/pdf_file/0018/22482/07_An-implementation-of-Watsons-algorithm-for-computing-two-dimensional-Delaunay-triangulations.pdf
	private IEnumerator BowyerWatson()
	{
		var min = -10f;
		var max = 10f;

		var seed = UnityEngine.Random.state;
		UnityEngine.Random.InitState(1);
		// Step 1 - sort by x
		for (var i = 0; i < n; i++)
		{
			_vertices.Add(new Vector2(UnityEngine.Random.Range(min, max), UnityEngine.Random.Range(min, max)));
		}

		UnityEngine.Random.state = seed;
		_vertices.Sort((v1, v2) => v1.x > v2.x ? 1 : -1);

		// Step 2 - Define the vertices of the super-triangles
		_vertices.Add(new Vector2(min - 1, min - 1));
		_vertices.Add(new Vector2(max + 1, min - 1));
		_vertices.Add(new Vector2(min - 1, max + 1));
		_vertices.Add(new Vector2(max + 1, max + 1));

		// Run static constructors
		var type = typeof(Edge);
		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
		type = typeof(Triangle);
		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);

		// Step 2.1 - Add super-triangle to a list
		var triangle = Triangle.Get(_vertices[n], _vertices[n + 1], _vertices[n + 2]);
		_triangles.Add(triangle);
		triangle = Triangle.Get(_vertices[n + 3], _vertices[n + 1], _vertices[n + 2]);
		_triangles.Add(triangle);

		Debug.Break();
		yield return null;
		//System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		//sw.Start();
		_completedTriangles = 0;
		for (int i = 0; i < n; i++)
		{
			InsertPoint(_vertices[i]);
			if (i % 1 == 0)
				yield return null;
		}
		//sw.Stop();
		//Debug.Log(sw.ElapsedMilliseconds);
	}


	private Vector2? _mousePos0;
	private Triangle _selectedTriangle;
	private void Update()
    {
	    if (Input.GetMouseButtonDown(0))
	    {
		    if (ScreenPointToGround(Input.mousePosition, out var point)) 
			    return;
			
		    if (Input.GetKey(KeyCode.LeftControl))
			{
				_selectedTriangle = Triangle.GetUnderPointer(_triangles, point);
				return;
			}

		    _vertices.Add(point);
			InsertPoint(point);
	    }

	    if (Input.GetMouseButtonDown(1))
	    {
			if (_mousePos0 == null)
				_mousePos0 = Input.mousePosition;
			else
			{
				ScreenPointToGround(_mousePos0.Value, out var p0);
				ScreenPointToGround(Input.mousePosition, out var p1);

				var e = Edge.Get(p0, p1);

				for (var i = _triangles.Count - 1; i >= 0; i--)
				{
					var t = _triangles[i];

					if (e.Intersect(t))
					{
						_triangles.RemoveAt(i);
					}
				}

				_mousePos0 = null;
			}
	    }
	}


	private bool ScreenPointToGround(Vector3 mousePosition, out Vector2 point)
	{
		point = Vector2.zero;
		var ray = _camera.ScreenPointToRay(mousePosition);
		if (!_floorPlane.Raycast(ray, out var distance))
			return true;

		var p = ray.GetPoint(distance);
		point = new Vector2(p.x, p.z);
		return false;
	}


	private int _completedTriangles;
	private void InsertPoint(Vector2 point)
	{
		// Step 3 - new point from sorted list
		var d = point;

		foreach (var edge in _edges)
		{
			Edge.Refuse(edge);
		}
		_edges.Clear();

		// Step 4 - Examine the list of triangles
		int toRemove = _triangles.Count; 
		for (var i = _triangles.Count - 1; i >= _completedTriangles; i--)
		{
			var t = _triangles[i];

			var dxSquared = t.circumcenter.x - point.x;
			dxSquared *= dxSquared;

			if (dxSquared >= t.radiusSquared + 0.05f)
			{
				t.isCompleted = true;

				_triangles[i] = _triangles[_completedTriangles];
				_triangles[_completedTriangles] = t;

				_completedTriangles++;
				i++;
				continue;
			}

			var dySquared = t.circumcenter.y - point.y;
			dySquared *= dySquared;
			var dSquared = dxSquared + dySquared;

			if (dSquared >= t.radiusSquared) 
				continue;

			var e1 = Edge.Get(t.a, t.b);
			var e2 = Edge.Get(t.b, t.c);
			var e3 = Edge.Get(t.c, t.a);

			if (!_edges.Add(e1))
			{
				Edge.Refuse(e1); 
				_edges.Remove(e1);
			}

			if (!_edges.Add(e2))
			{
				Edge.Refuse(e2);
				_edges.Remove(e2);
			}

			if (!_edges.Add(e3))
			{
				Edge.Refuse(e3);
				_edges.Remove(e3);
			}

			Triangle.Refuse(t);

			toRemove--;
			_triangles[i] = _triangles[toRemove];
			_triangles[toRemove] = t;
		}

		if (toRemove < _triangles.Count)
		{
			_triangles.RemoveRange(toRemove, _triangles.Count - toRemove);
		}

		foreach (var e in _edges)
		{
			_triangles.Add(Triangle.Get(e.a, e.b, point));
		}
	}


	private void OnDrawGizmos()
	{
		Gizmos.color = Color.white;
		//int i = 0;
		//foreach (var vertex in _vertices)
		//{
		//	Gizmos.color = Color.HSVToRGB(i++ / (float)_vertices.Count, 1, 1);
		//	Gizmos.DrawSphere(new Vector3(vertex.x, 0, vertex.y), 0.02f);
		//}

		for (var i = _triangles.Count - 1; i >= 0; i--)
		{
			var t = _triangles[i];
			Gizmos.color = t.isCompleted ? Color.green : Color.white;
			Gizmos.DrawLine(new Vector3(t.a.x, 0, t.a.y), new Vector3(t.b.x, 0, t.b.y));
			Gizmos.DrawLine(new Vector3(t.b.x, 0, t.b.y), new Vector3(t.c.x, 0, t.c.y));
			Gizmos.DrawLine(new Vector3(t.c.x, 0, t.c.y), new Vector3(t.a.x, 0, t.a.y));

			//Gizmos.color = Color.red;
			//Gizmos.DrawSphere(new Vector3(t.a.x, 0, t.a.y), 0.02f);
			//Gizmos.color = Color.green;
			//Gizmos.DrawSphere(new Vector3(t.b.x, 0, t.b.y), 0.02f);
			//Gizmos.color = Color.blue;
			//Gizmos.DrawSphere(new Vector3(t.c.x, 0, t.c.y), 0.02f);

			//if (t.isCompleted)
			//	continue;

			//Gizmos.color = Color.cyan * 0.3f;
			//Gizmos.DrawWireSphere(new Vector3(t.circumcenter.x, 0, t.circumcenter.y), Mathf.Sqrt(t.radiusSquared));
		}

		Gizmos.color = Color.red;
		//foreach (var e in _edges)
		//{
		//	Gizmos.DrawLine(new Vector3(e.a.x, 0, e.a.y), new Vector3(e.b.x, 0, e.b.y));
		//}

		if (_selectedTriangle != null)
		{
			var t = _selectedTriangle;

			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(new Vector3(t.a.x, 0, t.a.y), new Vector3(t.b.x, 0, t.b.y));
			Gizmos.DrawLine(new Vector3(t.b.x, 0, t.b.y), new Vector3(t.c.x, 0, t.c.y));
			Gizmos.DrawLine(new Vector3(t.c.x, 0, t.c.y), new Vector3(t.a.x, 0, t.a.y));

			Gizmos.color = Color.cyan * 0.5f;
			Gizmos.DrawWireSphere(new Vector3(t.circumcenter.x, 0, t.circumcenter.y), Mathf.Sqrt(t.radiusSquared));
		}

		if (_mousePos0 != null)
		{
			ScreenPointToGround(_mousePos0.Value, out var p0);
			ScreenPointToGround(Input.mousePosition, out var p1);
			Gizmos.DrawLine(new Vector3(p0.x, 0, p0.y), new Vector3(p1.x, 0, p1.y));
		}
	}


}
