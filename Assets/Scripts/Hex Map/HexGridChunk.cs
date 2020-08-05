using UnityEngine;
using UnityEngine.AI;

public class HexGridChunk : MonoBehaviour
{


	[SerializeField, /*HideInInspector*/] private HexCell[] _cells;
	//[SerializeField] private NavMeshSurface _navMesh;
	[SerializeField] private HexFeatureManager _features;
	[SerializeField] private HexMesh _waterShore;
	[SerializeField] private HexMesh _estuaries;
	[SerializeField] private HexMesh _terrain;
	[SerializeField] private HexMesh _rivers;
	[SerializeField] private HexMesh _roads;
	[SerializeField] private HexMesh _water;


	private static readonly Color _weights1 = new Color(1f, 0f, 0f, 1f);
	private static readonly Color _weights2 = new Color(0f, 1f, 0f, 1f);
	private static readonly Color _weights3 = new Color(0f, 0f, 1f, 1f);



	public void Init()
	{
		_cells = new HexCell[HexMetrics.CHUNK_SIZE_X * HexMetrics.CHUNK_SIZE_Z];

		_waterShore.Init();
		_estuaries.Init();
		_features.Init();
		_terrain.Init();
		_rivers.Init();
		_roads.Init();
		_water.Init();
	}


	public void Build()
	{
		Triangulate();
	}


	public void AddCell(int index, HexCell cell)
	{
		_cells[index] = cell;
		cell.chunk = this;
		cell.transform.SetParent(transform, false);
	}


	public void Refresh()
	{
		enabled = true;
	}


	private void LateUpdate()
	{
		Triangulate();
		enabled = false;
	}


	public void Triangulate()
	{
		_waterShore.Clear();
		_estuaries.Clear();
		_features.Clear();
		_terrain.Clear();
		_rivers.Clear();
		_roads.Clear();
		_water.Clear();

		foreach (var cell in _cells)
		{
			Triangulate(cell);
		}

		//BuildNavMesh();

		_waterShore.Apply();
		_estuaries.Apply();
		_features.Apply();
		_terrain.Apply();
		_rivers.Apply();
		_roads.Apply();
		_water.Apply();
	}


	private void BuildNavMesh()
	{
		//_navMesh.BuildNavMesh();

		foreach (var cell in _cells)
		{
			if (cell.isUnderwater)
				continue;

			//for (var d = HexDirection.Ne; d <= HexDirection.Se; d++)
			//{
			//	var neighbor = cell[d];

			//	if (neighbor is null || neighbor.isUnderwater)
			//		continue;

			//	if (cell.GetEdgeType(d) != HexEdgeType.Slope)
			//		continue;

			//	var c1 = cell.position;
			//	var c2 = neighbor.position;

			//	var e1 = new EdgeVertices(
			//		c1 + HexMetrics.GetFirstSolidCorner(d),
			//		c1 + HexMetrics.GetSecondSolidCorner(d));

			//	var bridge = HexMetrics.GetBridge(d);
			//	bridge.y = neighbor.position.y - cell.position.y;
			//	var e2 = new EdgeVertices(
			//		e1.v1 + bridge,
			//		e1.v5 + bridge);

			//	var link = _navMesh.gameObject.AddComponent<NavMeshLink>();
			//	link.startPoint = HexMetrics.Perturb(Vector3.Lerp(e1.v3, c1, 0.2f) + Vector3.up * 0.1f);
			//	link.endPoint = HexMetrics.Perturb(Vector3.Lerp(e2.v3, c2, 0.2f) + Vector3.up * 0.1f);

			//	link.width = Vector3.Distance(e1.v1, e1.v5);
			//	link.autoUpdate = false;
			//	link.costModifier = 10000;
			//}
		}
	}


	private void Triangulate(HexCell cell)
	{
		for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
		{
			Triangulate(d, cell);
		}

		if (!cell.isUnderwater)
		{
			if (!cell.isHasRiver && !cell.isHasRoad)
				_features.AddFeature(cell, cell.position);

			if (cell.isSpecial)
			{
				_features.AddSpecialFeature(cell, cell.position);
			}
		}
	}


	private void Triangulate(HexDirection direction, HexCell cell)
	{
		var center = cell.position;
		var e = new EdgeVertices(
			center + HexMetrics.GetFirstSolidCorner(direction),
			center + HexMetrics.GetSecondSolidCorner(direction));

		if (cell.isHasRiver)
		{
			if (cell.IsRiverGoesThroughEdge(direction))
			{
				e.v3.y = cell.streamBedY;

				if (cell.isRiverBeginOrEnd)
				{
					TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
				}
				else
				{
					TriangulateWithRiver(direction, cell, center, e);
				}
			}
			else
			{
				TriangulateAdjacentToRiver(direction, cell, center, e);
			}
		}
		else
		{
			TriangulateWithoutRiver(direction, cell, center, e);

			if (!cell.isUnderwater && !cell.IsRoadGoesThroughEdge(direction))
			{
				_features.AddFeature( cell, (center + e.v1 + e.v5) * (1f / 3f));
			}
		}

		if (direction <= HexDirection.Se)
		{
			TriangulateConnection(direction, cell, e);
		}

		if (cell.isUnderwater)
		{
			TriangulateWater(direction, cell, center);
		}
	}


	private void TriangulateWater(HexDirection direction, HexCell cell, Vector3 center)
	{
		center.y = cell.waterSurfaceY;

		var neighbor = cell[direction];
		if (neighbor != null && !neighbor.isUnderwater)
		{
			TriangulateWaterShore(direction, cell, neighbor, center);
		}
		else
		{
			TriangulateOpenWater(direction, cell, neighbor, center);
		}

		
	}


	private void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
	{
		var e1 = new EdgeVertices(
			center + HexMetrics.GetFirstWaterCorner(direction),
			center + HexMetrics.GetSecondWaterCorner(direction));
		_water.AddTriangle(center, e1.v1, e1.v2);
		_water.AddTriangle(center, e1.v2, e1.v3);
		_water.AddTriangle(center, e1.v3, e1.v4);
		_water.AddTriangle(center, e1.v4, e1.v5);
		Vector3 indices;
		indices.x = indices.z = cell.index;
		indices.y = neighbor.index;
		_water.AddTriangleCellData(indices, _weights1);
		_water.AddTriangleCellData(indices, _weights1);
		_water.AddTriangleCellData(indices, _weights1);
		_water.AddTriangleCellData(indices, _weights1);

		//var bridge = HexMetrics.GetWaterBridge(direction);
		var center2 = neighbor.position;
		if (neighbor.columnIndex < cell.columnIndex - 1)
		{
			center2.x += HexMetrics.wrapSize * HexMetrics.INNER_DIAMETER;
		}
		else if (neighbor.columnIndex > cell.columnIndex + 1)
		{
			center2.x -= HexMetrics.wrapSize * HexMetrics.INNER_DIAMETER;
		}
		center2.y = center.y;

		var e2 = new EdgeVertices(
			center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
			center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite()));
		
		if (cell.IsRiverGoesThroughEdge(direction))
		{
			TriangulateEstuary(e1, e2, cell.isHasIncomingRiver && cell.incomingRiverDirection == direction, indices);
		}
		else
		{
			_waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
			_waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
			_waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
			_waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
			_waterShore.AddQuadUv(0f, 0f, 0f, 1f);
			_waterShore.AddQuadUv(0f, 0f, 0f, 1f);
			_waterShore.AddQuadUv(0f, 0f, 0f, 1f);
			_waterShore.AddQuadUv(0f, 0f, 0f, 1f);
			_waterShore.AddQuadCellData(indices, _weights1, _weights2);
			_waterShore.AddQuadCellData(indices, _weights1, _weights2);
			_waterShore.AddQuadCellData(indices, _weights1, _weights2);
			_waterShore.AddQuadCellData(indices, _weights1, _weights2);
		}

		var nextNeighbor = cell[direction.Next()];
		if (nextNeighbor != null)
		{
			var center3 = nextNeighbor.position;
			if (nextNeighbor.columnIndex < cell.columnIndex - 1)
			{
				center3.x += HexMetrics.wrapSize * HexMetrics.INNER_DIAMETER;
			}
			else if (nextNeighbor.columnIndex > cell.columnIndex + 1)
			{
				center3.x -= HexMetrics.wrapSize * HexMetrics.INNER_DIAMETER;
			}
			var v3 = center3 + (nextNeighbor.isUnderwater ?
				HexMetrics.GetFirstWaterCorner(direction.Previous()) :
				HexMetrics.GetFirstSolidCorner(direction.Previous()));
			v3.y = center.y;
			_waterShore.AddTriangle(e1.v5, e2.v5, v3);

			_waterShore.AddTriangleUv(
				new Vector2(0f, 0f),
				new Vector2(0f, 1f),
				new Vector2(0f, nextNeighbor.isUnderwater ? 0f : 1f)
			);

			indices.z = nextNeighbor.index;
			_waterShore.AddTriangleCellData(
				indices, _weights1, _weights2, _weights3
			);
		}
	}


	private void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool isIncomingRiver, Vector3 indices)
	{
		_waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
		_waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
		_waterShore.AddTriangleUv(
			new Vector2(0f,1f), 
			new Vector2(0f,0f), 
			new Vector2(0f, 0f));
		_waterShore.AddTriangleUv(
			new Vector2(0f, 1f),
			new Vector2(0f, 0f),
			new Vector2(0f, 0f));

		_waterShore.AddTriangleCellData(indices, _weights2, _weights1, _weights1);
		_waterShore.AddTriangleCellData(indices, _weights2, _weights1, _weights1);

		_estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
		_estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
		_estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

		_estuaries.AddQuadUv(
			new Vector2(0f, 1f), 
			new Vector2(0f, 0f),
			new Vector2(1f, 1f), 
			new Vector2(0f, 0f));
		_estuaries.AddTriangleUv(
			new Vector2(0f, 0f),
			new Vector2(1f, 1f),
			new Vector2(1f, 1f));
		_estuaries.AddQuadUv(
			new Vector2(0f, 0f),
			new Vector2(0f, 0f),
			new Vector2(1f, 1f),
			new Vector2(0f, 1f));

		_estuaries.AddQuadCellData(indices, _weights2, _weights1, _weights2, _weights1);
		_estuaries.AddTriangleCellData(indices, _weights1, _weights2, _weights2);
		_estuaries.AddQuadCellData(indices, _weights1, _weights2);

		if (isIncomingRiver)
		{
			_estuaries.AddQuadUv2(
				new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f),
				new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f)
			);
			_estuaries.AddTriangleUv2(
				new Vector2(0.5f, 1.1f),
				new Vector2(1f, 0.8f),
				new Vector2(0f, 0.8f)
			);
			_estuaries.AddQuadUv2(
				new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
				new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f)
			);
		}
		else
		{
			_estuaries.AddQuadUv2(
				new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
				new Vector2(0f, 0f), new Vector2(0.5f, -0.3f)
			);
			_estuaries.AddTriangleUv2(
				new Vector2(0.5f, -0.3f),
				new Vector2(0f, 0f),
				new Vector2(1f, 0f)
			);
			_estuaries.AddQuadUv2(
				new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
				new Vector2(1f, 0f), new Vector2(1.5f, -0.2f)
			);
		}
	}


	private void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
	{
		var c1 = center + HexMetrics.GetFirstWaterCorner(direction);
		var c2 = center + HexMetrics.GetSecondWaterCorner(direction);

		_water.AddTriangle(center, c1, c2);
		Vector3 indices;
		indices.x = indices.y = indices.z = cell.index;
		_water.AddTriangleCellData(indices, _weights1);

		if (direction <= HexDirection.Se && neighbor != null)
		{
			var bridge = HexMetrics.GetWaterBridge(direction);
			var e1 = c1 + bridge;
			var e2 = c2 + bridge;

			_water.AddQuad(c1, c2, e1, e2);
			indices.y = neighbor.index;
			_water.AddQuadCellData(indices, _weights1, _weights2);

			if (direction <= HexDirection.E)
			{
				var nextNeighbor = cell[direction.Next()];

				if (nextNeighbor == null || !nextNeighbor.isUnderwater)
				{
					return;
				}

				_water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
				indices.z = nextNeighbor.index;
				_water.AddTriangleCellData(indices, _weights1, _weights2, _weights3);
			}
		}
	}


	private void TriangulateWithoutRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
	{
		TriangulateEdgeFan(center, e, cell.index);

		if (!cell.isHasRoad)
			return;

		var interpolators = GetRoadInterpolators(direction, cell);
		TriangulateRoad(
			center,
			Vector3.Lerp(center, e.v1, interpolators.x),
			Vector3.Lerp(center, e.v5, interpolators.y),
			e, cell.IsRoadGoesThroughEdge(direction), cell.index);
	}


	private void TriangulateRoadAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
	{
		var isHasRoadThroughEdge = cell.IsRoadGoesThroughEdge(direction);
		var previousHasRiver = cell.IsRiverGoesThroughEdge(direction.Previous());
		var nextHasRiver = cell.IsRiverGoesThroughEdge(direction.Next());
		var interpolators = GetRoadInterpolators(direction, cell);

		var roadCenter = center;

		if (cell.isRiverBeginOrEnd)
		{
			roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.riverBeginOrEndDirection.Opposite()) * (1f / 3f);
		}
		else if (cell.incomingRiverDirection == cell.outgoingRiverDirection.Opposite())
		{
			Vector3 corner;
			if (previousHasRiver)
			{
				if (!isHasRoadThroughEdge && !cell.IsRoadGoesThroughEdge(direction.Next()))
				{
					return;
				}
				corner = HexMetrics.GetSecondSolidCorner(direction);
			}
			else
			{
				if (!isHasRoadThroughEdge && !cell.IsRoadGoesThroughEdge(direction.Previous()))
				{
					return;
				}
				corner = HexMetrics.GetFirstSolidCorner(direction);
			}
			roadCenter += corner * 0.5f;
			if (cell.incomingRiverDirection == direction.Next() && (
				cell.IsRoadGoesThroughEdge(direction.Next2()) ||
				cell.IsRoadGoesThroughEdge(direction.Opposite())))
			{
				_features.AddBridge(roadCenter, center - corner * 0.5f);
			}
			center += corner * 0.25f;
		}
		else if (cell.incomingRiverDirection == cell.outgoingRiverDirection.Previous())
		{
			roadCenter -= HexMetrics.GetSecondCorner(cell.incomingRiverDirection) * 0.2f;
		}
		else if (cell.incomingRiverDirection == cell.outgoingRiverDirection.Next())
		{
			roadCenter -= HexMetrics.GetFirstCorner(cell.incomingRiverDirection) * 0.2f;
		}
		else if (previousHasRiver && nextHasRiver)
		{
			if (!isHasRoadThroughEdge)
				return;
			
			var offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.INNER_TO_OUTER;
			roadCenter += offset * 0.7f;
			center += offset * 0.5f;
		}
		else
		{
			HexDirection middle;
			if (previousHasRiver)
			{
				middle = direction.Next();
			}
			else if (nextHasRiver)
			{
				middle = direction.Previous();
			}
			else
			{
				middle = direction;
			}

			if (!cell.IsRoadGoesThroughEdge(middle) &&
				!cell.IsRoadGoesThroughEdge(middle.Previous()) &&
				!cell.IsRoadGoesThroughEdge(middle.Next()) )
				return;
			var offset = HexMetrics.GetSolidEdgeMiddle(middle);
			roadCenter += offset * 0.25f;
			if (direction == middle &&
			    cell.IsRoadGoesThroughEdge(direction.Opposite()))
			{
				_features.AddBridge(
					roadCenter,
					center - offset * (HexMetrics.INNER_TO_OUTER * 0.7f)
				);
			}
		}

		var mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
		var mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);

		TriangulateRoad(roadCenter, mL, mR, e, isHasRoadThroughEdge, cell.index);
		if (previousHasRiver)
		{
			TriangulateRoadEdge(roadCenter, center, mL, cell.index);
		}
		if (nextHasRiver)
		{
			TriangulateRoadEdge(roadCenter, mR, center, cell.index);
		}
	}


	private Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
	{
		Vector2 interpolators;

		if (cell.IsRoadGoesThroughEdge(direction))
		{
			interpolators.x = 0.5f;
			interpolators.y = 0.5f;
		}
		else
		{
			interpolators.x = cell.IsRoadGoesThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
			interpolators.y = cell.IsRoadGoesThroughEdge(direction.Next()) ? 0.5f : 0.25f;
		}

		return interpolators;
	}


	private void TriangulateRoad(
		Vector3 center, Vector3 mL, Vector3 mR, 
		EdgeVertices e, bool isRoadGoesTroughCellEdge, float index)
	{
		if (isRoadGoesTroughCellEdge)
		{
			Vector3 indices;
			indices.x = indices.y = indices.z = index;
			var mC = Vector3.Lerp(mL, mR, 0.5f);
			
			TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4, _weights1, _weights2, indices);
			
			_roads.AddTriangle(center, mL, mC);
			_roads.AddTriangle(center, mC, mR);

			_roads.AddTriangleUv(new Vector2(1, 0), new Vector2(0, 0), new Vector2(1, 0));
			_roads.AddTriangleUv(new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 0));

			_roads.AddTriangleCellData(indices, _weights1);
			_roads.AddTriangleCellData(indices, _weights1);
		}
		else
		{
			TriangulateRoadEdge(center, mL, mR, index);
		}
	}


	private void TriangulateRoadSegment(
		Vector3 v1, Vector3 v2, Vector3 v3,
		Vector3 v4, Vector3 v5, Vector3 v6,
		Color w1, Color w2, Vector3 indices)
	{
		_roads.AddQuad(v1, v2, v4, v5);
		_roads.AddQuad(v2, v3, v5, v6);

		_roads.AddQuadUv(0, 1, 0, 0);
		_roads.AddQuadUv(1, 0, 0, 0);

		_roads.AddQuadCellData(indices, w1, w2);
		_roads.AddQuadCellData(indices, w1, w2);
	}


	private void TriangulateRoadEdge(
		Vector3 center, Vector3 mL, Vector3 mR, float index)
	{
		_roads.AddTriangle(center, mL, mR);
		_roads.AddTriangleUv(new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 0));

		Vector3 indices;
		indices.x = indices.y = indices.z = index;
		_roads.AddTriangleCellData(indices, _weights1);
	}


	private void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
	{
		if (cell.isHasRoad)
		{
			TriangulateRoadAdjacentToRiver(direction, cell, center, e);
		}

		if (cell.IsRiverGoesThroughEdge(direction.Next()))
		{
			if (cell.IsRiverGoesThroughEdge(direction.Previous()))
			{
				center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.INNER_TO_OUTER * 0.5f);
			}
			else if (cell.IsRiverGoesThroughEdge(direction.Previous2()))
			{
				center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
			}
		}
		else if (
			cell.IsRiverGoesThroughEdge(direction.Previous()) &&
			cell.IsRiverGoesThroughEdge(direction.Next2())
		)
		{
			center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
		}

		var m = new EdgeVertices(
			Vector3.Lerp(center, e.v1, 0.5f),
			Vector3.Lerp(center, e.v5, 0.5f)
		);

		TriangulateEdgeStrip(
			m, _weights1, cell.index, 
			e, _weights1, cell.index);
		TriangulateEdgeFan(center, m, cell.index);

		if (!cell.isUnderwater && !cell.IsRoadGoesThroughEdge(direction))
		{
			_features.AddFeature(cell, (center + e.v1 + e.v2) * (1f / 3f));
		}
	}


	private void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
	{
		var m = new EdgeVertices(
			Vector3.Lerp(center, e.v1, 0.5f),
			Vector3.Lerp(center, e.v5, 0.5f));

		m.v3.y = e.v3.y;

		TriangulateEdgeStrip(
			m, _weights1, cell.index, 
			e, _weights1, cell.index);
		TriangulateEdgeFan(center, m, cell.index);

		if (cell.isUnderwater)
			return;

		var isReversed = cell.isHasIncomingRiver;
		Vector3 indices;
		indices.x = indices.y = indices.z = cell.index;
		TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.riverSurfaceY, 0.6f, isReversed, indices);

		center.y = m.v2.y = m.v4.y = cell.riverSurfaceY;
		_rivers.AddTriangle(center, m.v2, m.v4);
		if (isReversed)
		{
			_rivers.AddTriangleUv(
				new Vector2(0.5f, 0.4f),
				new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
		}
		else
		{
			_rivers.AddTriangleUv(
				new Vector2(0.5f, 0.4f),
				new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
		}

		_rivers.AddTriangleCellData(indices, _weights1);
	}


	private void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
	{
		Vector3 centerL;
		Vector3 centerR;

		if (cell.IsRiverGoesThroughEdge(direction.Opposite()))
		{
			centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
			centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
		}
		else if (cell.IsRiverGoesThroughEdge(direction.Next()))
		{
			centerL = center;
			centerR = Vector3.Lerp(center, e.v5, 2.0f / 3.0f);
		}
		else if (cell.IsRiverGoesThroughEdge(direction.Previous()))
		{
			centerR = center;
			centerL = Vector3.Lerp(center, e.v1, 2.0f / 3.0f);
		}
		else if (cell.IsRiverGoesThroughEdge(direction.Next2()))
		{
			centerL = center;
			centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.INNER_TO_OUTER);
		}
		else
		{
			centerR = center;
			centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.INNER_TO_OUTER);
		}
		center = Vector3.Lerp(centerL, centerR, 0.5f);

		var m = new EdgeVertices(
			Vector3.Lerp(centerL, e.v1, 0.5f),
			Vector3.Lerp(centerR, e.v5, 0.5f),
			1.0f / 6.0f);

		center.y = e.v3.y;
		m.v3.y = e.v3.y;

		TriangulateEdgeStrip(
			m, _weights1, cell.index, 
			e, _weights1, cell.index);

		_terrain.AddTriangle(centerL, m.v1, m.v2);
		_terrain.AddQuad(centerL, center, m.v2, m.v3);
		_terrain.AddQuad(center, centerR, m.v3, m.v4);
		_terrain.AddTriangle(centerR, m.v4, m.v5);

		Vector3 indices;
		indices.x = indices.y = indices.z = cell.index;
		_terrain.AddTriangleCellData(indices, _weights1);
		_terrain.AddQuadCellData(indices, _weights1);
		_terrain.AddQuadCellData(indices, _weights1);
		_terrain.AddTriangleCellData(indices, _weights1);

		if (cell.isUnderwater)
			return;

		var isReversed = cell.incomingRiverDirection == direction;
		TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.riverSurfaceY, 0.4f, isReversed, indices);
		TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.riverSurfaceY, 0.6f, isReversed, indices);
	}


	void TriangulateWaterfallInWater(
		Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
		float y1, float y2, float waterY, Vector3 indices
	)
	{
		v1.y = v2.y = y1;
		v3.y = v4.y = y2;
		v1 = HexMetrics.Perturb(v1);
		v2 = HexMetrics.Perturb(v2);
		v3 = HexMetrics.Perturb(v3);
		v4 = HexMetrics.Perturb(v4);
		var t = (waterY - y2) / (y1 - y2);
		v3 = Vector3.Lerp(v3, v1, t);
		v4 = Vector3.Lerp(v4, v2, t);
		_rivers.AddQuadUnperturbed(v1, v2, v3, v4);
		_rivers.AddQuadUv(0f, 1f, 0.8f, 1f);
		_rivers.AddQuadCellData(indices, _weights1, _weights2);
	}


	private void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
	{
		var neighbor = cell[direction];
		if (neighbor == null)
			return;

		var bridge = HexMetrics.GetBridge(direction);
		bridge.y = neighbor.position.y - cell.position.y;
		var e2 = new EdgeVertices(
			e1.v1 + bridge,
			e1.v5 + bridge);

		var isHasRiver = cell.IsRiverGoesThroughEdge(direction);
		var isHasRoad = cell.IsRoadGoesThroughEdge(direction);

		if (isHasRiver)
		{
			e2.v3.y = neighbor.streamBedY;
			Vector3 indices;
			indices.x = indices.z = cell.index;
			indices.y = neighbor.index;

			if (!cell.isUnderwater)
			{
				if (!neighbor.isUnderwater)
				{
					TriangulateRiverQuad(
						e1.v2, e1.v4, e2.v2, e2.v4,
						cell.riverSurfaceY, neighbor.riverSurfaceY, 0.8f,
						cell.isHasIncomingRiver && cell.incomingRiverDirection == direction, indices);
				}
				else if (cell.elevation > neighbor.waterLevel)
				{
					TriangulateWaterfallInWater(
						e1.v2, e1.v4, e2.v2, e2.v4,
						cell.riverSurfaceY, neighbor.riverSurfaceY,
						neighbor.waterSurfaceY, indices);
				}
			}
			else if (!neighbor.isUnderwater && neighbor.elevation > cell.waterLevel )
			{
				TriangulateWaterfallInWater(
					e2.v4, e2.v2, e1.v4, e1.v2,
					neighbor.riverSurfaceY, cell.riverSurfaceY,
					cell.waterSurfaceY, indices);
			}
		}

		if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
		{
			TriangulateEdgeTerraces(e1, cell, e2, neighbor, isHasRoad);
		}
		else
		{
			TriangulateEdgeStrip(
				e1, _weights1, cell.index, 
				e2, _weights2, neighbor.index, isHasRoad);
		}

		_features.AddWall(e1, cell, e2, neighbor, isHasRiver, isHasRoad);

		var nextNeighbor = cell[direction.Next()];
		if (direction > HexDirection.E || nextNeighbor == null)
			return;

		var v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
		v5.y = nextNeighbor.position.y;

		if (cell.elevation <= neighbor.elevation)
		{
			if (cell.elevation <= nextNeighbor.elevation)
			{
				TriangulateCorner(e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor);
			}
			else
			{
				TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
			}
		}
		else if (neighbor.elevation <= nextNeighbor.elevation)
		{
			TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell);
		}
		else
		{
			TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
		}

	}


	private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float index)
	{
		_terrain.AddTriangle(center, edge.v1, edge.v2);
		_terrain.AddTriangle(center, edge.v2, edge.v3);
		_terrain.AddTriangle(center, edge.v3, edge.v4);
		_terrain.AddTriangle(center, edge.v4, edge.v5);

		Vector3 indices;
		indices.x = indices.y = indices.z = index;
		_terrain.AddTriangleCellData(indices, _weights1);
		_terrain.AddTriangleCellData(indices, _weights1);
		_terrain.AddTriangleCellData(indices, _weights1);
		_terrain.AddTriangleCellData(indices, _weights1);
	}


	void TriangulateEdgeStrip(
		EdgeVertices e1, Color w1, float i1,
		EdgeVertices e2, Color w2, float i2,
		bool isHasRoad = false
	)
	{
		_terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
		_terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
		_terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
		_terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

		Vector3 indices;
		indices.x = indices.z = i1;
		indices.y = i2;
		_terrain.AddQuadCellData(indices, w1, w2);
		_terrain.AddQuadCellData(indices, w1, w2);
		_terrain.AddQuadCellData(indices, w1, w2);
		_terrain.AddQuadCellData(indices, w1, w2);

		if (isHasRoad)
		{
			TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4, w1, w2, indices);
		}
	}


	private void TriangulateCorner(
		Vector3 bottom, HexCell bottomCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell)
	{
		var leftEdgeType = bottomCell.GetEdgeType(leftCell);
		var rightEdgeType = bottomCell.GetEdgeType(rightCell);

		if (leftEdgeType == HexEdgeType.Slope)
		{
			if (rightEdgeType == HexEdgeType.Slope)
			{
				TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
			}
			else if (rightEdgeType == HexEdgeType.Flat)
			{
				TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
			}
			else
			{
				TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
			}
		}
		else if (rightEdgeType == HexEdgeType.Slope)
		{
			if (leftEdgeType == HexEdgeType.Flat)
			{
				TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
			}
			else
			{
				TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
			}
		}
		else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
		{
			if (leftCell.elevation < rightCell.elevation)
			{
				TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
			}
			else
			{
				TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
			}
		}
		else
		{
			_terrain.AddTriangle(bottom, left, right);
			
			Vector3 indices;
			indices.x = bottomCell.index;
			indices.y = leftCell.index;
			indices.z = rightCell.index;
			_terrain.AddTriangleCellData(indices, _weights1, _weights2, _weights3);
		}

		_features.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);
	}


	private void TriangulateCornerTerracesCliff(
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell)
	{
		var b = 1f / (rightCell.elevation - beginCell.elevation);
		b = b < 0 ? -b : b;

		var boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
		var boundaryWeights = Color.Lerp(_weights1, _weights3, b);

		Vector3 indices;
		indices.x = beginCell.index;
		indices.y = leftCell.index;
		indices.z = rightCell.index;

		TriangulateBoundaryTriangle(
			begin, _weights1, left, _weights2, boundary, boundaryWeights, indices
		);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
		{
			TriangulateBoundaryTriangle(left, _weights2, right, _weights3, boundary, boundaryWeights, indices);
		}
		else
		{
			_terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
			_terrain.AddTriangleCellData(indices, _weights2, _weights3, boundaryWeights);
		}
	}


	private void TriangulateCornerCliffTerraces(
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell)
	{
		var b = 1f / (leftCell.elevation - beginCell.elevation);
		b = b < 0 ? -b : b;

		var boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
		var boundaryWeights = Color.Lerp(_weights1, _weights2, b);

		Vector3 indices;
		indices.x = beginCell.index;
		indices.y = leftCell.index;
		indices.z = rightCell.index;

		TriangulateBoundaryTriangle(
			right, _weights3, begin, _weights1, boundary, boundaryWeights, indices
		);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
		{
			TriangulateBoundaryTriangle(left, _weights2, right, _weights3, boundary, boundaryWeights, indices);
		}
		else
		{
			_terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
			_terrain.AddTriangleCellData(indices, _weights2, _weights3, boundaryWeights);
		}
	}


	private void TriangulateBoundaryTriangle(
		Vector3 begin, Color beginWeights,
		Vector3 left, Color leftWeights,
		Vector3 boundary, Color boundaryWeights, Vector3 indices)
	{
		var v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
		var w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, 1);

		_terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
		_terrain.AddTriangleCellData(indices, beginWeights, w2, boundaryWeights);

		for (var i = 2; i < HexMetrics.TERRACE_STEP; i++)
		{
			var v1 = v2;
			var w1 = w2;
			v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
			w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, i);
			_terrain.AddTriangleUnperturbed(v1, v2, boundary);
			_terrain.AddTriangleCellData(indices, w1, w2, boundaryWeights);
		}

		_terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
		_terrain.AddTriangleCellData(indices, w2, leftWeights, boundaryWeights);
	}


	private void TriangulateCornerTerraces(
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell)
	{
		var v3 = HexMetrics.TerraceLerp(begin, left, 1);
		var v4 = HexMetrics.TerraceLerp(begin, right, 1);
		var w3 = HexMetrics.TerraceLerp(_weights1, _weights2, 1);
		var w4 = HexMetrics.TerraceLerp(_weights1, _weights3, 1);

		Vector3 indices;
		indices.x = beginCell.index;
		indices.y = leftCell.index;
		indices.z = rightCell.index;

		_terrain.AddTriangle(begin, v3, v4);
		_terrain.AddTriangleCellData(indices, _weights1, w3, w4);

		for (var i = 2; i < HexMetrics.TERRACE_STEP; i++)
		{
			var v1 = v3;
			var v2 = v4;
			var w1 = w3;
			var w2 = w4;
			v3 = HexMetrics.TerraceLerp(begin, left, i);
			v4 = HexMetrics.TerraceLerp(begin, right, i);
			w3 = HexMetrics.TerraceLerp(_weights1, _weights2, i);
			w4 = HexMetrics.TerraceLerp(_weights1, _weights3, i);
			_terrain.AddQuad(v1, v2, v3, v4);
			_terrain.AddQuadCellData(indices, w1, w2, w3, w4);
		}

		_terrain.AddQuad(v3, v4, left, right);
		_terrain.AddQuadCellData(indices, w3, w4, _weights2, _weights3);
	}


	private void TriangulateEdgeTerraces(
		EdgeVertices begin, HexCell beginCell,
		EdgeVertices end, HexCell endCell,
		bool isHasRoad)
	{
		var e2 = EdgeVertices.TerraceLerp(begin, end, 1);
		var w2 = HexMetrics.TerraceLerp(_weights1, _weights2, 1);
		var i1 = beginCell.index;
		var i2 = endCell.index;

		TriangulateEdgeStrip(begin, _weights1, i1, e2, w2, i2, isHasRoad);

		for (var i = 2; i < HexMetrics.TERRACE_STEP; i++)
		{
			var e1 = e2;
			var w1 = w2;

			e2 = EdgeVertices.TerraceLerp(begin, end, i);
			w2 = HexMetrics.TerraceLerp(_weights1, _weights2, i);

			TriangulateEdgeStrip(e1, w1, i1, e2, w2, i2, isHasRoad);
		}

		TriangulateEdgeStrip(e2, w2, i1, end, _weights2, i2, isHasRoad);
	}


	private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool isReversed, Vector3 indices)
	{
		TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, isReversed, indices);
	}


	private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, bool isReversed, Vector3 indices)
	{
		v1.y = y1;
		v2.y = y1;
		v3.y = y2;
		v4.y = y2;

		_rivers.AddQuad(v1, v2, v3, v4);

		if (isReversed)
		{
			_rivers.AddQuadUv(1.0f, 0.0f, 0.8f - v, 0.6f - v);
		}
		else
		{
			_rivers.AddQuadUv(0.0f, 1.0f, v, v + 0.2f);
		}

		_rivers.AddQuadCellData(indices, _weights1, _weights2);
	}


}