using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;



public class HexFeatureManager : SerializedMonoBehaviour
{


	[TableMatrix(IsReadOnly = false)]
	[OdinSerialize] private Transform[][] _urbanPrefabs;
	[OdinSerialize] private Transform[][] _farmPrefabs;
	[OdinSerialize] private Transform[][] _plantPrefabs;
	[OdinSerialize] private Transform[] _specialPrefabs;
	[OdinSerialize] private Transform _wallTowerPrefab;
	[OdinSerialize] private Transform _bridgePrefab;

	[SerializeField] private HexMesh _walls;

	[SerializeField, HideInInspector] private Transform _container;




	public void Clear()
	{
		DeleteAndSpawnContainer();

		if (_walls != null)
			_walls.Clear();
	}


	private void DeleteAndSpawnContainer()
	{
		if (_container != null)
		{
			if (Application.isPlaying)
				Destroy(_container.gameObject);
			else
				DestroyImmediate(_container.gameObject);
		}

		SpawnContainer();
	}


	public void Apply()
	{
		_walls.Apply();
	}


	public void AddFeature(HexCell cell, Vector3 position)
	{
		if (cell.isSpecial)
			return;

		var hash = HexMetrics.SampleHashGrid(position);

		var prefab = PickPrefab(_urbanPrefabs, cell.urbanLevel, hash.a, hash.d);
		var otherPrefab = PickPrefab(_farmPrefabs, cell.farmLevel, hash.b, hash.d);
		var useHash = hash.a;
		if (prefab != null)
		{
			if (otherPrefab != null && hash.b < hash.a)
			{
				prefab = otherPrefab;
				useHash = hash.b;
			}
		}
		else if (otherPrefab != null)
		{
			prefab = otherPrefab;
			useHash = hash.b;
		}


		otherPrefab = PickPrefab(_plantPrefabs, cell.plantLevel, hash.c, hash.d);
		if (prefab != null)
		{
			if (otherPrefab != null && hash.c < useHash)
			{
				prefab = otherPrefab;
			}
		}
		else if (otherPrefab != null)
		{
			prefab = otherPrefab;
		}
		else
		{
			return;
		}

		var featureInstance = Instantiate(prefab);
		position.y += featureInstance.localScale.y * 0.5f;
		featureInstance.position = HexMetrics.Perturb(position);
		featureInstance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
		featureInstance.SetParent(_container, false);
	}


	public void Init()
	{
		DeleteAndSpawnContainer();

		_walls.Init();
	}


	private void SpawnContainer()
	{
		_container = new GameObject("Feature Container").transform;
		_container.SetParent(transform, false);
	}


	private Transform PickPrefab(IReadOnlyList<Transform[]> featureCollection, int level, float hash, float choice)
	{
		if (level <= 0) 
			return null;
		
		var thresholds = HexMetrics.GetFeatureThresholds(level - 1);
		for (var i = 0; i < thresholds.Length; i++)
		{
			if (hash < thresholds[i])
			{
				return featureCollection[i][(int)(choice * featureCollection[i].Length)];
			}
		}

		return null;
	}


	public void AddWall(
		EdgeVertices near, HexCell nearCell,
		EdgeVertices far, HexCell farCell,
		bool hasRiver, bool hasRoad
		)
	{
		if (nearCell.walled != farCell.walled &&
		    !nearCell.isUnderwater && !farCell.isUnderwater &&
		    nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff)
		{
			AddWallSegment(near.v1, far.v1, near.v2, far.v2);
			if (hasRiver || hasRoad)
			{
				AddWallCap(near.v2, far.v2);
				AddWallCap(far.v4, near.v4);
			}
			else
			{
				AddWallSegment(near.v2, far.v2, near.v3, far.v3);
				AddWallSegment(near.v3, far.v3, near.v4, far.v4);
			}
			AddWallSegment(near.v4, far.v4, near.v5, far.v5);
		}
	}


	public void AddWall(
		Vector3 c1, HexCell cell1,
		Vector3 c2, HexCell cell2,
		Vector3 c3, HexCell cell3
	)
	{
		if (cell1.walled)
		{
			if (cell2.walled)
			{
				if (!cell3.walled)
				{
					AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
				}
			}
			else if (cell3.walled)
			{
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
			else
			{
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
		}
		else if (cell2.walled)
		{
			if (cell3.walled)
			{
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
			else
			{
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
		}
		else if (cell3.walled)
		{
			AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
		}
	}


	void AddWallSegment(
		Vector3 pivot, HexCell pivotCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	)
	{
		if (pivotCell.isUnderwater)
		{
			return;
		}

		var hasLeftWall = !leftCell.isUnderwater &&
		                   pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
		var hasRightWall = !rightCell.isUnderwater &&
		                   pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

		if (hasLeftWall)
		{
			if (hasRightWall)
			{
				var isHasTower = false;
				if (leftCell.elevation == rightCell.elevation)
				{
					var hash = HexMetrics.SampleHashGrid( (pivot + left + right) * (1f / 3f) );
					isHasTower = hash.e < HexMetrics.WALL_TOWER_THRESHOLD;
				}
				AddWallSegment(pivot, left, pivot, right, isHasTower);
			}
			else if (leftCell.elevation < rightCell.elevation)
			{
				AddWallWedge(pivot, left, right);
			}
			else
			{
				AddWallCap(pivot, left);
			}
		}
		else if (hasRightWall)
		{
			if (rightCell.elevation < leftCell.elevation)
			{
				AddWallWedge(right, pivot, left);
			}
			else
			{
				AddWallCap(right, pivot);
			}
		}
	}


	private void AddWallSegment(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight, bool isAddTower = false)
	{
		nearLeft = HexMetrics.Perturb(nearLeft);
		farLeft = HexMetrics.Perturb(farLeft);
		nearRight = HexMetrics.Perturb(nearRight);
		farRight = HexMetrics.Perturb(farRight);

		var left = HexMetrics.WallLerp(nearLeft, farLeft);
		var right = HexMetrics.WallLerp(nearRight, farRight);

		var leftThicknessOffset =
			HexMetrics.WallThicknessOffset(nearLeft, farLeft);
		var rightThicknessOffset =
			HexMetrics.WallThicknessOffset(nearRight, farRight);

		var leftTop = left.y + HexMetrics.WALL_HEIGHT;
		var rightTop = right.y + HexMetrics.WALL_HEIGHT;


		Vector3 v3;
		Vector3 v4;

		var v1 = v3 = left - leftThicknessOffset;
		var v2 = v4 = right - rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		_walls.AddQuadUnperturbed(v1, v2, v3, v4);

		var t1 = v3;
		var t2 = v4;

		v1 = v3 = left + leftThicknessOffset;
		v2 = v4 = right + rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		_walls.AddQuadUnperturbed(v2, v1, v4, v3);

		_walls.AddQuadUnperturbed(t1, t2, v3, v4);

		if (isAddTower)
		{
			var towerInstance = Instantiate(_wallTowerPrefab, _container, false);
			towerInstance.localPosition = (left + right) * 0.5f;
			var rightDirection = right - left;
			rightDirection.y = 0f;
			towerInstance.right = rightDirection;
		}
	}


	void AddWallCap(Vector3 near, Vector3 far)
	{
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb(far);

		var center = HexMetrics.WallLerp(near, far);
		var thickness = HexMetrics.WallThicknessOffset(near, far);

		Vector3 v3, v4;

		var v1 = v3 = center - thickness;
		var v2 = v4 = center + thickness;
		v3.y = v4.y = center.y + HexMetrics.WALL_HEIGHT;
		_walls.AddQuadUnperturbed(v1, v2, v3, v4);
	}


	void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
	{
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb(far);
		point = HexMetrics.Perturb(point);

		var center = HexMetrics.WallLerp(near, far);
		var thickness = HexMetrics.WallThicknessOffset(near, far);

		Vector3 v3, v4;
		var pointTop = point;
		point.y = center.y;

		var v1 = v3 = center - thickness;
		var v2 = v4 = center + thickness;
		v3.y = v4.y = pointTop.y = center.y + HexMetrics.WALL_HEIGHT;

		_walls.AddQuadUnperturbed(v1, point, v3, pointTop);
		_walls.AddQuadUnperturbed(point, v2, pointTop, v4);
		_walls.AddTriangleUnperturbed(pointTop, v3, v4);
	}


	public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
	{
		var instance = Instantiate(_bridgePrefab, _container, false);

		roadCenter1 = HexMetrics.Perturb(roadCenter1);
		roadCenter2 = HexMetrics.Perturb(roadCenter2);
		instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f; 
		instance.forward = roadCenter2 - roadCenter1;
		var length = Vector3.Distance(roadCenter1, roadCenter2);
		instance.localScale = new Vector3(
			1f, 1f, length * (1f / HexMetrics.BRIDGE_DESIGN_LENGTH)
		);
	}


	public void AddSpecialFeature(HexCell cell, Vector3 position)
	{
		var instance = Instantiate(_specialPrefabs[cell.specialIndex - 1]);
		instance.localPosition = HexMetrics.Perturb(position);
		var hash = HexMetrics.SampleHashGrid(position);
		instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
		instance.SetParent(_container, false);
	}
}
