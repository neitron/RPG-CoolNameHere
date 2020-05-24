using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;



public class HexMapGenerator : MonoBehaviour
{



	[Serializable, Toggle("enabled")]
	public class Toggleable
	{
		public bool enabled;
		public int value;
	}



	private struct MapRegion
	{

		public int xMin;
		public int xMax;
		public int yMin;
		public int yMax;

	}



	[SerializeField] 
	private HexGrid _grid;

	[SerializeField]
	private Toggleable _fixedSeed;

	[MinMaxSlider(20, 200, true), SerializeField]
	private Vector2Int _chunkSizeRange = new Vector2Int(30, 100);

	[Range(5, 95), SerializeField]
	private int _landPercentage = 50;

	[Range(0, 5), SerializeField]
	private int _waterLevel = 3;

	[MinMaxSlider(-4, 10, true), SerializeField]
	private Vector2 _elevationRange = new Vector2(-2, 8);

	[Range(0, 10), FoldoutGroup("Border"), SerializeField]
	private int _mapBorderX = 5;

	[Range(0, 10), FoldoutGroup("Border"), SerializeField]
	private int _mapBorderY = 5;

	[Range(0, 10), FoldoutGroup("Border"), SerializeField]
	private int _regionBorder = 5;

	[Range(1, 4), SerializeField]
	private int _regionCount = 1;

	[Range(0, 100), SerializeField]
	private int _erosionPercentage = 50;

	[Range(0f, 0.5f), SerializeField] 
	private float _jitterProbability = 0.25f;

	[Range(0f, 1f)]
	[SerializeField]
	[Tooltip("If a cell reach probability it will race 2 elevation instead of 1, blocking a path between cells")]
	private float _highRiseProbability = 0.25f;

	[Range(0f, 0.4f)]
	private float _sinkProbability = 0.2f;


	private int _cellCount;
	private int _searchFrontierPhase;
	private HexCellPriorityQueue _searchFrontier;
	private List<MapRegion> _regions;
	

	public void GenerateMap(int x, int z)
	{
		var originalRandomState = Random.state;
		if (!_fixedSeed.enabled)
		{
			_fixedSeed.value = Random.Range(0, int.MaxValue);
			_fixedSeed.value ^= (int)DateTime.Now.Ticks;
			_fixedSeed.value ^= (int)Time.unscaledTime;
			_fixedSeed.value &= int.MaxValue;
		}
		Random.InitState(_fixedSeed.value);

		_cellCount = x * z;
		_grid.CreateMap(x, z);

		if (_searchFrontier == null)
		{
			_searchFrontier = new HexCellPriorityQueue();
		}

		for (var i = 0; i < _cellCount; i++)
		{
			_grid[i].waterLevel = _waterLevel;
		}

		CreateRegions();
		CreateLand();
		ErodeLand();
		SetTerrainType();

		for (var i = 0; i < _cellCount; i++)
		{
			_grid[i].searchPhase = 0;
		}

		Random.state = originalRandomState;
	}


	private bool IsErodible(HexCell cell)
	{
		var erodibleElevation = cell.elevation - 2;
		for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
		{
			var neighbor = cell[d];

			if (neighbor != null && neighbor.elevation <= erodibleElevation)
			{
				return true;
			}
		}
		return false;
	}


	private HexCell GetErosionTarget(HexCell cell)
	{
		var candidates = ListPool<HexCell>.Get();
		
		var erodibleElevation = cell.elevation - 2;
		for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
		{
			var neighbor = cell[d];
			if (neighbor != null && neighbor.elevation <= erodibleElevation)
			{
				candidates.Add(neighbor);
			}
		}

		var target = candidates[Random.Range(0, candidates.Count)];
		ListPool<HexCell>.Add(candidates);
		return target;
	}


	private void ErodeLand()
	{
		var erodibleCells = ListPool<HexCell>.Get();
		for (var i = 0; i < _cellCount; i++)
		{
			var cell = _grid[i];
			if (IsErodible(cell))
				erodibleCells.Add(cell);
		}

		var targetErodibleCount =
			(int) (erodibleCells.Count * (100 - _erosionPercentage) * 0.001f);

		while (erodibleCells.Count > targetErodibleCount)
		{
			var index = Random.Range(0, erodibleCells.Count);
			var cell = erodibleCells[index];
			var targetCell = GetErosionTarget(cell);

			cell.elevation--;
			targetCell.elevation++;

			if (!IsErodible(cell))
			{
				erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
				erodibleCells.RemoveAt(erodibleCells.Count - 1);
			}

			for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
			{
				var neighbor = cell[d];
				if (neighbor && neighbor.elevation == cell.elevation + 2 && 
				    !erodibleCells.Contains(neighbor))
				{
					erodibleCells.Add(neighbor);
				}
			}

			if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell))
			{
				erodibleCells.Add(targetCell);
			}

			for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
			{
				var neighbor = targetCell[d];
				if (neighbor && neighbor != cell &&
					neighbor.elevation == targetCell.elevation + 1 && 
					!IsErodible(neighbor))
				{
					erodibleCells.Remove(neighbor);
				}
			}
		}

		ListPool<HexCell>.Add(erodibleCells);
	}


	private void CreateRegions()
	{
		if (_regions == null)
		{
			_regions = new List<MapRegion>();
		}
		else
		{
			_regions.Clear();
		}

		MapRegion region;

		switch (_regionCount)
		{
			default: {
				region.xMin = _mapBorderX;
				region.xMax = _grid.cellCount.x - _mapBorderX;
				region.yMin = _mapBorderY;
				region.yMax = _grid.cellCount.y - _mapBorderY;
				_regions.Add(region);
				break;
			}

			case 2: {
				if (Random.value < 0.5f)
				{
					region.xMin = _mapBorderX;
					region.xMax = _grid.cellCount.x / 2 - _regionBorder;
					region.yMin = _mapBorderY;
					region.yMax = _grid.cellCount.y - _mapBorderY;
					_regions.Add(region);

					region.xMin = _grid.cellCount.x / 2 + _regionBorder;
					region.xMax = _grid.cellCount.x - _mapBorderX;
					_regions.Add(region);
				}
				else
				{
					region.xMin = _mapBorderX;
					region.xMax = _grid.cellCount.x - _mapBorderX;
					region.yMin = _mapBorderY;
					region.yMax = _grid.cellCount.y / 2 - _regionBorder;
					_regions.Add(region);
					region.yMin = _grid.cellCount.y / 2 + _regionBorder;
					region.yMax = _grid.cellCount.y - _mapBorderY;
					_regions.Add(region);
				}
				break;
			}

			case 3: {
				region.xMin = _mapBorderX;
				region.xMax = _grid.cellCount.x / 3 - _regionBorder;
				region.yMin = _mapBorderY;
				region.yMax = _grid.cellCount.y - _mapBorderY;
				_regions.Add(region);
				region.xMin = _grid.cellCount.x / 3 + _regionBorder;
				region.xMax = _grid.cellCount.x * 2 / 3 - _regionBorder;
				_regions.Add(region);
				region.xMin = _grid.cellCount.x * 2 / 3 + _regionBorder;
				region.xMax = _grid.cellCount.x - _mapBorderX;
				_regions.Add(region);
				break;
			}

			case 4:
			{
				region.xMin = _mapBorderX;
				region.xMax = _grid.cellCount.x / 2 - _regionBorder;
				region.yMin = _mapBorderY;
				region.yMax = _grid.cellCount.y / 2 - _regionBorder;
				_regions.Add(region);
				region.xMin = _grid.cellCount.x / 2 + _regionBorder;
				region.xMax = _grid.cellCount.x - _mapBorderX;
				_regions.Add(region);
				region.yMin = _grid.cellCount.y / 2 + _regionBorder;
				region.yMax = _grid.cellCount.y - _mapBorderY;
				_regions.Add(region);
				region.xMin = _mapBorderX;
				region.xMax = _grid.cellCount.x / 2 - _regionBorder;
				_regions.Add(region);
				break;
			}
		}
	}


	private void CreateLand()
	{
		var landBudget = Mathf.RoundToInt(_cellCount * _landPercentage * 0.01f);

		for (var guard = 0; guard < 10000; guard++)
		{
			var sink = Random.value < _sinkProbability;

			foreach (var region in _regions)
			{
				var chunkSize = Random.Range(_chunkSizeRange.x, _chunkSizeRange.y - 1);

				landBudget = sink ? 
					SinkTerrain(chunkSize, landBudget, region) : 
					RaiseTerrain(chunkSize, landBudget, region);

				if (landBudget == 0)
				{
					return;
				}
			}
		}

		if (landBudget > 0)
		{
			Debug.LogWarning("Failed to use up " + landBudget + " land budget.");
		}
	}


	private void SetTerrainType()
	{
		for (var i = 0; i < _cellCount; i++)
		{
			var cell = _grid[i];
			if(!cell.isUnderwater)
				cell.terrainTypeIndex = cell.elevation - cell.waterLevel;
			cell.SetMapData(
				(cell.elevation - _elevationRange.x) /
				(float)(_elevationRange.y - _elevationRange.x)
			);
		}
	}


	private int RaiseTerrain(int chunkSize, int budget, MapRegion region)
	{
		_searchFrontierPhase++;
		var firstCell = GetRandomCell(region);
		firstCell.searchPhase = _searchFrontierPhase;
		firstCell.distance = 0;
		firstCell.searchHeuristic = 0;
		_searchFrontier.Enqueue(firstCell);

		var center = firstCell.coordinates;

		var rise = Random.value < _highRiseProbability ? 2 : 1;
		var size = 0;
		while (size < chunkSize && _searchFrontier.count > 0)
		{
			var current = _searchFrontier.Dequeue();
			var originalElevation = current.elevation;
			var newElevation = originalElevation + rise;
			if (newElevation > _elevationRange.y)
				continue;

			current.elevation = newElevation;

			if (originalElevation < _waterLevel &&
				newElevation >= _waterLevel && --budget == 0)
			{
				break;
			}

			size += 1;

			for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
			{
				var neighbor = current[d];
				if (neighbor && neighbor.searchPhase < _searchFrontierPhase)
				{
					neighbor.searchPhase = _searchFrontierPhase;
					neighbor.distance = neighbor.coordinates.DistanceTo(center);
					neighbor.searchHeuristic = Random.value < _jitterProbability ? 1 : 0;
					_searchFrontier.Enqueue(neighbor);
				}
			}
		}
		_searchFrontier.Clear();

		return budget;
	}

	private int SinkTerrain(int chunkSize, int budget, MapRegion region)
	{
		_searchFrontierPhase++;
		var firstCell = GetRandomCell(region);
		firstCell.searchPhase = _searchFrontierPhase;
		firstCell.distance = 0;
		firstCell.searchHeuristic = 0;
		_searchFrontier.Enqueue(firstCell);

		var center = firstCell.coordinates;

		var sink = Random.value < _highRiseProbability ? 2 : 1;
		var size = 0;
		while (size < chunkSize && _searchFrontier.count > 0)
		{
			var current = _searchFrontier.Dequeue();
			var originalElevation = current.elevation;
			var newElevation = originalElevation - sink;
			if (newElevation < _elevationRange.x)
				continue;

			current.elevation = newElevation;

			if (originalElevation >= _waterLevel &&
			    newElevation < _waterLevel)
			{
				budget += 1;
			}

			size += 1;

			for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
			{
				var neighbor = current[d];
				if (neighbor && neighbor.searchPhase < _searchFrontierPhase)
				{
					neighbor.searchPhase = _searchFrontierPhase;
					neighbor.distance = neighbor.coordinates.DistanceTo(center);
					neighbor.searchHeuristic = Random.value < _jitterProbability ? 1 : 0;
					_searchFrontier.Enqueue(neighbor);
				}
			}
		}
		_searchFrontier.Clear();

		return budget;
	}


	private HexCell GetRandomCell(MapRegion region) =>
		_grid[
			Random.Range(region.xMin, region.xMax), 
			Random.Range(region.yMin, region.yMax)];
}
