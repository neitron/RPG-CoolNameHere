

using Geometry;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;



public static class Vector2Extension
{


	public static float Cros(this Vector2 v0, Vector2 v1)
	{
		return v0.x * v1.y - v0.y * v1.x;
	}


}




public class Player : IPlayer
{


	public int id { get; }

	private static HexCell _currentCell;
	private static HexUnit _selectedUnit;
	private static Vector3 _selectedPoint;


	private readonly HexGrid _grid;
	private readonly List<HexUnit> _units;
	private readonly CompositeDisposable _disposables = new CompositeDisposable();



	public Player(int id, HexGrid grid)
	{
		this.id = id;
		_grid = grid;

		_units = new List<HexUnit>();
	}



	public void Tick()
	{
		if (EventSystem.current.IsPointerOverGameObject()) return;

		DoNavigationTest();

		if (Input.GetMouseButtonDown(0))
		{
			DoSelection();
		}
		else if (_selectedUnit && _selectedUnit.playerId == id)
		{
			if (Input.GetMouseButtonDown(1))
			{
				DoMove();
			}
			else
			{
				DoPathfinding();
			}
		}
	}





	Triangle _selectedTriangle = null;
	Coroutine coroutine;
	private void DoNavigationTest()
	{
		if (coroutine != null)
			return;

		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		bool isIntersect = _grid.NavMeshRaycast(ray, out var hit, out var triangle);

		//if (_selectedTriangle != null)
		//{
		//	//DrawTriangle(_selectedTriangle, Color.blue);
		//	Debug.DrawLine(_selectedTriangle.a, _selectedTriangle.b, Color.blue); // right apex
		//	Debug.DrawLine(_selectedTriangle.a, _selectedTriangle.c, Color.red); // left apex
		//	Debug.DrawLine(_selectedTriangle.c, _selectedTriangle.b, Color.gray);

		//	var overLeftFactor = Triangle.SquaredArea(_selectedTriangle.a, _selectedTriangle.b, hit.point);
		//	var overRightFactor = Triangle.SquaredArea(_selectedTriangle.a, _selectedTriangle.c, hit.point);
		//	Debug.DrawLine(_selectedTriangle.a, hit.point, overLeftFactor >= 0f ? Color.yellow : (overRightFactor <= 0f ? Color.cyan : Color.green)); // portal apex
		//}

		if (Input.GetMouseButtonDown(0) && isIntersect)
		{
			_selectedTriangle = triangle;
			_selectedPoint = hit.point;
		}
		else if (_selectedTriangle != null && isIntersect)
		{
			var pathFinder = new A_StarPathFinder();
			var trianglePath = pathFinder.FindPath(_selectedTriangle, triangle);
			if (trianglePath != null && trianglePath.Count > 1)
			{
				foreach (var t in trianglePath)
				{
					DrawTriangle(t, Color.red * 0.7f);
				}

				//if (Input.GetKeyUp(KeyCode.F))
				//	coroutine = _grid.StartCoroutine(BuildPathThrougTrianglePathDebug(trianglePath, _selectedPoint, hit.point));

				var path = BuildPathThrougTrianglePath(trianglePath, _selectedPoint, hit.point);
				for (int j = 1; j < path.Count; j++)
				{
					Debug.DrawLine(path[j - 1], path[j], Color.magenta);
				}
				//PathNormalCorrection(path);
				//for (int j = 1; j < path.Count; j++)
				//{
				//	Debug.DrawLine(path[j - 1], path[j], Color.cyan);
				//}
			}
		}
		else if (isIntersect)
		{
			DrawTriangle(triangle, Color.red);
		}

		
	}


	private void PathNormalCorrection(List<Vector3> path)
	{
		var refPath = new List<Vector3>(path);
		for (int i = 1; i < path.Count - 1; i++)
		{
			var n0 = (refPath[i - 1] - refPath[i]).normalized;
			var n1 = (refPath[i + 1] - refPath[i]).normalized;
			n0.y = 0;
			n1.y = 0;
			Debug.DrawRay(refPath[i], n0, Color.white);
			Debug.DrawRay(refPath[i], n1, Color.white);
			var offset = -(n0 + n1).normalized * 1.5f;
			path[i] += offset;
		}
	}


	private List<Vector3> BuildPathThrougTrianglePath(List<Triangle> strip, Vector3 start, Vector3 end)
	{
		var path = new List<Vector3>() { end };
		if (strip.Count == 1)
		{
			path.Add(start);
			return path;
		}

		var anchor = end;
		var anchorIndex = 0;
		var ti = 1;
		var curr = strip[0];
		var next = strip[1];
		int rightIndex = 0;
		int leftIndex = 0;
		var prevPortal = curr.GetPortal(next);
		var left = prevPortal.leftShort;
		var right = prevPortal.rightShort;
		while (!IsSame(path[path.Count - 1], start) && ti < strip.Count)
		{
			Vector3 apex;
			Portal portal = default;
			curr = strip[ti];
			var isRightApex = false;

			if (ti == strip.Count - 1) 
				apex = start;
			else
			{
				next = strip[ti + 1];
				portal = curr.GetPortal(next);
				isRightApex = IsSame(prevPortal.left, portal.left);
				apex = isRightApex ? portal.rightShort : portal.leftShort;
			}

			var overLeftFactor = Triangle.SquaredArea(anchor, left, apex);
			var overRightFactor = Triangle.SquaredArea(anchor, right, apex);

			bool isApexOverLeft = overLeftFactor >= 0f;
			bool isApexOverRight = overRightFactor <= 0f;

			bool isObstacle = isApexOverLeft || isApexOverRight;

			if (ti == strip.Count - 1)
			{
				var newAnchor = isObstacle ? (isApexOverLeft ? left : right) : start;
				AddExtraPointsToRepeatTerrain(ref path, strip, anchorIndex, ti, anchor, newAnchor);

				if (isObstacle)
					path.Add(newAnchor);

				path.Add(start);

				break;
			}


			if ((isApexOverLeft && isRightApex) || (isApexOverRight && !isRightApex))
			{
				var newAnchor = isRightApex ? left : right;

				ti = isRightApex ? leftIndex : rightIndex;

				AddExtraPointsToRepeatTerrain(ref path, strip, anchorIndex, ti, anchor, newAnchor);

				path.Add(newAnchor);
				anchor = newAnchor;
				anchorIndex = ti;

				do
					portal = strip[ti].GetPortal(strip[++ti]);
				while (IsSame(newAnchor, isRightApex ? portal.leftShort : portal.rightShort));

				ti--;
				leftIndex = ti;
				rightIndex = ti;

				left = portal.leftShort;
				right = portal.rightShort;
			}
			else if (!isObstacle)
			{
				if (isRightApex)
				{
					rightIndex = ti;
					right = apex;
				}
				else
				{
					leftIndex = ti;
					left = apex;
				}
			}

			ti++;
			prevPortal = portal;
		}

		return path;
	}


	private IEnumerator BuildPathThrougTrianglePathDebug(List<Triangle> strip, Vector3 start, Vector3 end)
	{
		var path = new List<Vector3>() { end };
		if(strip.Count == 1)
		{
			path.Add(start);
			yield break;
		}

		var anchor = end;
		var anchorIndex = 0;
		var ti = 1;
		var curr = strip[0];
		var next = strip[1];
		int rightIndex = 0;
		int leftIndex = 0;
		var prevPortal = curr.GetPortal(next);
		var left = prevPortal.leftShort;
		var right = prevPortal.rightShort;
		while (!IsSame(path[path.Count - 1], start) && ti < strip.Count)
		{
			curr = strip[ti];
			DrawTriangle(curr, Color.gray);

			// If the last triangle we gonna process the last point
			Vector3 apex;
			var isRightApex = false;
			Portal portal = default;
			if (ti == strip.Count - 1)
			{
				apex = start;
			}
			else
			{
				next = strip[ti + 1];
				portal = curr.GetPortal(next);
				isRightApex = IsSame(prevPortal.left, portal.left);
				apex = isRightApex ? portal.rightShort : portal.leftShort;

				DrawTriangle(next, Color.white);
				Debug.DrawLine(portal.left, portal.right, Color.red);
			}

			Debug.DrawLine(anchor, left, isRightApex ? Color.yellow * 0.4f : Color.yellow);
			Debug.DrawLine(anchor, right, isRightApex ? Color.cyan : Color.cyan * 0.4f);

			var overLeftFactor = Triangle.SquaredArea(anchor, left, apex);
			var overRightFactor = Triangle.SquaredArea(anchor, right, apex);

			bool isApexOverLeft = overLeftFactor >= 0f;
			bool isApexOverRight = overRightFactor <= 0f;

			Debug.DrawLine(anchor, apex, isApexOverLeft ? Color.yellow : (isApexOverRight ? Color.cyan : Color.green));
			Debug.Break();
			yield return null;

			bool isObstacle = isApexOverLeft || isApexOverRight;
			
			if (ti == strip.Count - 1)
			{
				var newAnchor = isObstacle ? (isApexOverLeft ? left : right) : start;
				AddExtraPointsToRepeatTerrain(ref path, strip, anchorIndex, ti, anchor, newAnchor);

				if (isObstacle)
					path.Add(newAnchor);
				
				path.Add(start);
				
				break;
			}
			

			if ((isApexOverLeft && isRightApex) || (isApexOverRight && !isRightApex))
			{
				var newAnchor = isRightApex ? left : right;

				ti = isRightApex ? leftIndex : rightIndex;

				AddExtraPointsToRepeatTerrain(ref path, strip, anchorIndex, ti, anchor, newAnchor);

				path.Add(newAnchor);
				anchor = newAnchor;
				anchorIndex = ti;

				do 
					portal = strip[ti].GetPortal(strip[++ti]); 
				while (IsSame(newAnchor, isRightApex ? portal.leftShort : portal.rightShort));
				
				ti--;
				leftIndex = ti;
				rightIndex = ti;

				left = portal.leftShort;
				right = portal.rightShort;
			}
			else if (!isObstacle)
			{
				if (isRightApex)
				{
					rightIndex = ti;
					right = apex;
				}
				else
				{
					leftIndex = ti;
					left = apex;
				}
			}

			ti++;
			prevPortal = portal;

			if (path.Count > 1)
			{
				for (int j = 1; j < path.Count; j++)
				{
					DebugExtension.DrawPoint(path[j], 0.5f, Color.yellow, 0.15f);
					Debug.DrawLine(path[j - 1], path[j], Color.magenta);
				}

				Debug.Break();
				yield return null;
			}
		}
		if (path.Count > 1)
		{
			for (int j = 1; j < path.Count; j++)
			{
				DebugExtension.DrawPoint(path[j], 0.5f, Color.yellow, 0.15f);
				Debug.DrawLine(path[j - 1], path[j], Color.magenta);
			}

			Debug.Break();
			yield return null;
		}
		coroutine = null;
	}


	private void AddExtraPointsToRepeatTerrain(ref List<Vector3> path, List<Triangle> strip, 
		int startIndex, int endIndex, Vector3 start, Vector3 end)
	{
		// TODO: Add elevated points
		for (int i = startIndex + 1; i <= endIndex; i++)
		{
			var t0 = strip[i - 1];
			var t1 = strip[i];

			// Scip coliniar normals
			if (Mathf.Abs(Vector3.Dot(t0.normal, t1.normal)) > 0.9999f)
				continue;

			var port = t0.GetPortal(t1);
			var v0 = new Vector2(port.left.x, port.left.z);
			var v1 = new Vector2(port.right.x, port.right.z);
			var v2 = new Vector2(start.x, start.z);
			var v3 = new Vector2(end.x, end.z);

			// No intersaction => scip
			if (!Math2d.LineSegmentsIntersection(v0, v1, v2, v3, out var p))
				continue;

			// Find the real 3d point where path intersects the portal-edge
			var pFlat = new Vector3(p.x, 0f, p.y);

			var lp = pFlat - new Vector3(port.left.x, 0f, port.left.z);
			var lr = port.right - port.left;
			var lrFlat = new Vector3(lr.x, 0f, lr.z);

			// According to similar triangles rule
			var pLocal = lr.normalized * ((lr.magnitude * lp.magnitude) / lrFlat.magnitude);

			var extraAnchor = port.left + pLocal;
			path.Add(extraAnchor);
		}
	}

	
	private bool IsSame(Vector3 a, Vector3 b)
	{
		return (a - b).sqrMagnitude <= 0.000_001f;
	}



	private void DrawTriangle(Triangle t, Color color)
	{
		Debug.DrawLine(t.a, t.b, color);
		Debug.DrawLine(t.b, t.c, color);
		Debug.DrawLine(t.c, t.a, color);
	}


	public void AddUnit(HexUnit hexUnit)
	{
		_units.Add(hexUnit);
	}


	private void DoPathfinding()
	{
		if (UpdateCurrentCell())
		{
			if (_currentCell && _selectedUnit.IsValidDestination(_currentCell))
			{
				_grid.FindPath(_selectedUnit.location, _currentCell, _selectedUnit);
			}
			else
			{
				_grid.ClearPath();
			}
		}
	}


	public bool UpdateCurrentCell()
	{
		if (Camera.main == null)
			return false;

		var cell = _grid[Camera.main.ScreenPointToRay(Input.mousePosition)];
		if (cell == _currentCell)
			return false;

		_currentCell = cell;
		return true;
	}


	private void DoSelection()
	{
		_grid.ClearPath();
		UpdateCurrentCell();
		if (_currentCell)
		{
			_selectedUnit = _currentCell.unit;
		}
	}


	private void DoMove()
	{
		if (_grid.hasPath)
		{
			_selectedUnit.Travel(_grid.GetPath());
			_grid.ClearPath();
		}
	}


	private void OccupyCell()
	{
		if (!_selectedUnit || _selectedUnit.playerId != id)
			return;

		var cell = _selectedUnit.location;

		var hasNeighborWithSameOwner = false;
		for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
		{
			var neighbor = cell[d];
			hasNeighborWithSameOwner |= neighbor.isHasOwner && neighbor.owner == _selectedUnit.playerId;
		}

		if (hasNeighborWithSameOwner)
		{
			cell.owner = _selectedUnit.playerId;
			cell.shaderData.RefreshOwner(cell, _selectedUnit.playerId);
		}
	}


	public void StartTurn()
	{
		var gameView = Object.FindObjectOfType<HexGameUi>();
		gameView.cellOccupyButtonPressed.Do(_ => OccupyCell()).Subscribe().AddTo(_disposables);
	}


	public void EndTurn()
	{
		for (var i = 0; i < _units.Count; i++)
		{
			_units[i].ResetTravelBudget();
		}

		_disposables.Clear();
	}


}