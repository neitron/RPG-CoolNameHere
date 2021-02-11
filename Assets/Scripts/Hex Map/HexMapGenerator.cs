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


		// ReSharper disable once InconsistentNaming
		public bool enabled;

		// ReSharper disable once InconsistentNaming
		public int value;


	}



	private struct MapRegion
	{


		public int xMin;
		public int xMax;
		public int yMin;
		public int yMax;


	}


	private struct ClimateData
	{


		public float clouds;
		public float moisture;


	}


	private struct Biome
	{


		public int terrain;
		public int plant;


		public Biome(int terrain, int plant)
		{
			this.terrain = terrain;
			this.plant = plant;
		}


	}


	private enum HemisphereMode
	{


		Both,
		North,
		South


	}



	[SerializeField] private HexGrid _grid;

	[SerializeField] private Toggleable _fixedSeed;

	[MinMaxSlider(20, 200, true), SerializeField]
	private Vector2Int _chunkSizeRange = new Vector2Int(30, 100);

	[Range(5, 95), SerializeField] private int _landPercentage = 50;

	[Range(0, 5), SerializeField] private int _waterLevel = 3;

	[MinMaxSlider(-4, 10, true), SerializeField]
	private Vector2 _elevationRange = new Vector2(-2, 8);

	[Range(0, 10), FoldoutGroup("Land Border"), SerializeField]
	private int _mapBorderX = 5;

	[Range(0, 10), FoldoutGroup("Land Border"), SerializeField]
	private int _mapBorderY = 5;

	[Range(0, 10), FoldoutGroup("Land Border"), SerializeField]
	private int _regionBorder = 5;

	[Range(1, 4), SerializeField] private int _regionCount = 1;

	[Range(0, 100), SerializeField] private int _erosionPercentage = 50;

	[Range(0f, 0.5f), SerializeField] private float _jitterProbability = 0.25f;

	[Range(0f, 1f)]
	[SerializeField]
	[Tooltip("If a cell reach probability it will race 2 elevation instead of 1, blocking a path between cells")]
	private float _highRiseProbability = 0.25f;

	[Range(0f, 0.4f), SerializeField] private float _sinkProbability = 0.2f;

	[Range(0f, 1f), FoldoutGroup("Water Cycle"), SerializeField]
	private float _startingMoisture = 0.1f;

	[Range(0f, 1.0f), FoldoutGroup("Water Cycle"), SerializeField]
	private float _evaporationFactor = 0.5f;

	[Range(0f, 1.0f), FoldoutGroup("Water Cycle"), SerializeField]
	private float _precipitationFactor = 0.25f;

	[Range(0f, 1.0f), FoldoutGroup("Water Cycle"), SerializeField]
	private float _runOfFactor = 0.25f;

	[Range(0f, 1.0f), FoldoutGroup("Water Cycle"), SerializeField]
	private float _seepageFactor = 0.125f;

	[FoldoutGroup("Water Cycle"), SerializeField]
	private HexDirection _windDirection = HexDirection.Nw;

	[Range(1f, 10f), FoldoutGroup("Water Cycle"), SerializeField]
	private float _windStrength = 4f;

	[Range(0, 20), SerializeField] private float _riverPercentage = 10f;

	[Range(0f, 1f), SerializeField] private float _extraLakeProbability = 0.2f;

	[MinMaxSlider(0, 1, true), FoldoutGroup("Temperature"), SerializeField]
	private Vector2 _temperatureRange = new Vector2(0, 1);

	[SerializeField, FoldoutGroup("Temperature")]
	private HemisphereMode _hemisphere;

	[Range(0f, 1f), SerializeField, FoldoutGroup("Temperature")]
	private float _temperatureJitter = 0.1f;



	private int _cellCount;
	private int _landCells;
	private int _searchFrontierPhase;
	private int _temperatureJitterChannel;
	private HexCellPriorityQueue _searchFrontier;
	private List<MapRegion> _regions;
	private List<ClimateData> _climate;
	private List<ClimateData> _nextClimate;
	private readonly List<HexDirection> _flowDirections = new List<HexDirection>();

	private static float[] _temperatureBands = {0.1f, 0.3f, 0.6f};
	private static float[] _moistureBands = {0.12f, 0.28f, 0.85f};
	private static Biome[] _biomes =
	{
		new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0), 
		new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2), 
		new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2), 
		new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3),
	};


	public void GenerateMap(int x, int z, bool wrapping)
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
		_grid.CreateMap(x, z, wrapping);

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
		CreateClimate();
		CreateRivers();
		SetTerrainType();

		for (var i = 0; i < _cellCount; i++)
		{
			_grid[i].searchPhase = 0;
		}

		Random.state = originalRandomState;
	}


	private void CreateRivers()
	{
		var riversOrigins = ListPool<HexCell>.Get();

		for (var i = 0; i < _cellCount; i++)
		{
			var cell = _grid[i];
			if (cell.isUnderwater)
			{
				continue;
			}

			var data = _climate[i];
			var weight = 
				data.moisture * (cell.elevation - _waterLevel) / (_elevationRange.y - _waterLevel);
			if (weight > 0.75f)
			{
				riversOrigins.Add(cell);
				riversOrigins.Add(cell);
			}
			if (weight > 0.5f)
			{
				riversOrigins.Add(cell);
			}
			if (weight > 0.25f)
			{
				riversOrigins.Add(cell);
			}
		}

		var riverBudget = Mathf.RoundToInt(_landCells * _riverPercentage * 0.01f);
		while (riverBudget > 0 && riversOrigins.Count > 0)
		{
			var index = Random.Range(0, riversOrigins.Count);
			var lastIndex = riversOrigins.Count - 1;
			var origin = riversOrigins[index];
			
			// Optimized remove
			riversOrigins[index] = riversOrigins[lastIndex];
			riversOrigins.RemoveAt(lastIndex);

			if (!origin.isHasRiver)
			{
				var isValidOrigin = true;
				for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
				{
					var neighbor = origin[d];
					if (neighbor && (neighbor.isHasRiver || neighbor.isUnderwater))
					{
						isValidOrigin = false;
						break;
					}
				}

				if (isValidOrigin)
					riverBudget -= CreateRiver(origin);
			}
		}

		if (riverBudget > 0)
		{
			Debug.LogWarning("Failed to use up river budget.");
		}

		ListPool<HexCell>.Refuse(riversOrigins);
	}


	private int CreateRiver(HexCell origin)
	{
		var length = 1;
		var cell = origin;
		var direction = HexDirection.Ne;

		while (!cell.isUnderwater)
		{
			var minNeighborElevation = int.MaxValue;
			_flowDirections.Clear();

			for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
			{
				var neighbor = cell[d];

				if (!neighbor)
					continue;

				if (neighbor.elevation < minNeighborElevation)
				{
					minNeighborElevation = neighbor.elevation;
				}

				if (neighbor == origin || neighbor.isHasIncomingRiver)
					continue;

				var delta = neighbor.elevation - cell.elevation;
				if (delta > 0)
					continue;

				if (neighbor.isHasOutgoingRiver)
				{
					cell.SetOutgoingRiver(d);
					return length;
				}

				if (delta < 0)
				{
					_flowDirections.Add(d);
					_flowDirections.Add(d);
					_flowDirections.Add(d);
				}

				if (length == 1 || (d != direction.Next2() && d != direction.Previous2()))
				{
					_flowDirections.Add(d);
				}

				_flowDirections.Add(d);
			}

			
			if (_flowDirections.Count == 0)
			{
				if (length == 1)
					return 0;

				if (minNeighborElevation >= cell.elevation)
				{
					cell.waterLevel = minNeighborElevation;

					if (minNeighborElevation == cell.elevation)
					{
						cell.elevation = minNeighborElevation - 1;
					}
				}
				break;
			}

			direction = _flowDirections[Random.Range(0, _flowDirections.Count)];
			cell.SetOutgoingRiver(direction);
			length += 1;

			if (minNeighborElevation >= cell.elevation &&
			    Random.value < _extraLakeProbability)
			{
				cell.waterLevel = cell.elevation;
				cell.elevation -= 1;
			}

			cell = cell[direction];
		}
		return length;
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
		ListPool<HexCell>.Refuse(candidates);
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
			(int) (erodibleCells.Count * (100 - _erosionPercentage) * 0.01f);

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

		ListPool<HexCell>.Refuse(erodibleCells);
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

		var borderX = _grid.wrapping ? _regionBorder : _mapBorderX;
		MapRegion region;

		switch (_regionCount)
		{
			default: {
				if (_grid.wrapping)
				{
					borderX = 0;
				}
				region.xMin = borderX;
				region.xMax = _grid.cellCount.x - borderX;
				region.yMin = _mapBorderY;
				region.yMax = _grid.cellCount.y - _mapBorderY;
				_regions.Add(region);
				break;
			}

			case 2: {
				if (Random.value < 0.5f)
				{
					region.xMin = borderX;
					region.xMax = _grid.cellCount.x / 2 - _regionBorder;
					region.yMin = _mapBorderY;
					region.yMax = _grid.cellCount.y - _mapBorderY;
					_regions.Add(region);

					region.xMin = _grid.cellCount.x / 2 + _regionBorder;
					region.xMax = _grid.cellCount.x - borderX;
					_regions.Add(region);
				}
				else
				{
					if (_grid.wrapping)
					{
						borderX = 0;
					}
					region.xMin = borderX;
					region.xMax = _grid.cellCount.x - borderX;
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
				region.xMin = borderX;
				region.xMax = _grid.cellCount.x / 3 - _regionBorder;
				region.yMin = _mapBorderY;
				region.yMax = _grid.cellCount.y - _mapBorderY;
				_regions.Add(region);
				region.xMin = _grid.cellCount.x / 3 + _regionBorder;
				region.xMax = _grid.cellCount.x * 2 / 3 - _regionBorder;
				_regions.Add(region);
				region.xMin = _grid.cellCount.x * 2 / 3 + _regionBorder;
				region.xMax = _grid.cellCount.x - borderX;
				_regions.Add(region);
				break;
			}

			case 4:
			{
				region.xMin = borderX;
				region.xMax = _grid.cellCount.x / 2 - _regionBorder;
				region.yMin = _mapBorderY;
				region.yMax = _grid.cellCount.y / 2 - _regionBorder;
				_regions.Add(region);
				region.xMin = _grid.cellCount.x / 2 + _regionBorder;
				region.xMax = _grid.cellCount.x - borderX;
				_regions.Add(region);
				region.yMin = _grid.cellCount.y / 2 + _regionBorder;
				region.yMax = _grid.cellCount.y - _mapBorderY;
				_regions.Add(region);
				region.xMin = borderX;
				region.xMax = _grid.cellCount.x / 2 - _regionBorder;
				_regions.Add(region);
				break;
			}
		}
	}


	private void EvolveClimate(int cellIndex)
	{
		var cell = _grid[cellIndex];
		var cellClimate = _climate[cellIndex];

		if (cell.isUnderwater)
		{
			cellClimate.moisture = 1f;
			cellClimate.clouds += _evaporationFactor;
		}
		else
		{
			var evaporation = cellClimate.moisture * _evaporationFactor;
			cellClimate.moisture -= evaporation;
			cellClimate.clouds += evaporation;
		}

		var precipitation = cellClimate.clouds * _precipitationFactor;
		cellClimate.clouds -= precipitation;
		cellClimate.moisture += precipitation;

		var cloudMaximum = 1f - cell.viewElevation / (_elevationRange.y + 1);

		if (cellClimate.clouds > cloudMaximum)
		{
			cellClimate.moisture += cellClimate.clouds - cloudMaximum;
			cellClimate.clouds = cloudMaximum;
		}

		var mainDispersalDirection = _windDirection.Opposite();
		var cloudDispersal = cellClimate.clouds * (1f / (5f + _windStrength));
		var runOf = cellClimate.moisture * _runOfFactor * (1f / 6f);
		var seepage = cellClimate.moisture * _seepageFactor * (1f / 6f);
		for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
		{
			var neighbor = cell[d];
			if (neighbor == null)
				continue;

			var neighborClimate = _nextClimate[neighbor.index];

			if (d == mainDispersalDirection)
			{
				neighborClimate.clouds += cloudDispersal * _windStrength;
			}
			else
			{
				neighborClimate.clouds += cloudDispersal;
			}

			var elevationDelta = neighbor.viewElevation - cell.viewElevation;
			if (elevationDelta < 0)
			{
				cellClimate.moisture -= runOf;
				neighborClimate.moisture += runOf;
			}
			else if (elevationDelta == 0)
			{
				cellClimate.moisture -= seepage;
				neighborClimate.moisture += seepage;
			}

			_nextClimate[neighbor.index] = neighborClimate;
		}

		var nextCellClimate = _nextClimate[cellIndex];
		nextCellClimate.moisture += cellClimate.moisture;

		if (nextCellClimate.moisture > 1f)
		{
			nextCellClimate.moisture = 1.0f;
		}
		_nextClimate[cellIndex] = nextCellClimate;
		_climate[cellIndex] = new ClimateData();
	}


	private void CreateClimate()
	{
		if (_climate == null)
		{
			_climate = new List<ClimateData>();
		}
		else
		{
			_climate.Clear();
		}

		if (_nextClimate == null)
		{
			_nextClimate = new List<ClimateData>();
		}
		else
		{
			_nextClimate.Clear();
		}

		var initialData = new ClimateData
		{
			moisture = _startingMoisture
		};
		var clearData = new ClimateData();
		for (var i = 0; i < _cellCount; i++)
		{
			_climate.Add(initialData);
			_nextClimate.Add(clearData);
		}

		for (var cycle = 0; cycle < 40; cycle++)
		{
			for (var i = 0; i < _cellCount; i++)
			{
				EvolveClimate(i);
			}
			var swap = _climate;
			_climate = _nextClimate;
			_nextClimate = swap;
		}
	}


	private void CreateLand()
	{
		var landBudget = Mathf.RoundToInt(_cellCount * _landPercentage * 0.01f);
		_landCells = landBudget;

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
			_landCells -= landBudget;
		}
	}


	private float DetermineTemperature(HexCell cell)
	{
		var latitude = (float) cell.coordinates.z / _grid.cellCount.y;

		if (_hemisphere == HemisphereMode.Both)
		{
			latitude *= 2f;
			if (latitude > 1f)
			{
				latitude = 2f - latitude;
			}
		}
		else if (_hemisphere == HemisphereMode.North)
		{
			latitude = 1f - latitude;
		}

		var temperature = Mathf.LerpUnclamped(_temperatureRange.x, _temperatureRange.y, latitude);
		temperature *= 1f - (cell.viewElevation - _waterLevel) / (_elevationRange.y - _waterLevel + 1f);

		var jitter = HexMetrics.SampleNoise(cell.position * 0.1f) [_temperatureJitterChannel];
		temperature += (jitter * 2f - 1f) * _temperatureJitter;

		return temperature;
	}


	private void SetTerrainType()
	{
		_temperatureJitterChannel = Random.Range(0, 4);
		var rockDesertElevation = _elevationRange.y - (_elevationRange.y - _waterLevel) / 2;

		for (var i = 0; i < _cellCount; i++)
		{
			var cell = _grid[i];
			var temperature = DetermineTemperature(cell);
			
			var moisture = _climate[i].moisture;
			
			if (!cell.isUnderwater)
			{
				var t = 0;
				for (; t < _temperatureBands.Length; t++)
				{
					if (temperature < _temperatureBands[t])
					{
						break;
					}
				}

				var m = 0;
				for (; m < _moistureBands.Length; m++)
				{
					if (moisture < _moistureBands[m])
					{
						break;
					}
				}

				var cellBiome = _biomes[t * 4 + m];

				if (cellBiome.terrain == 0)
				{
					if (cell.elevation >= rockDesertElevation)
					{
						cellBiome.terrain = 3;
					}
				}
				else if (cell.elevation == (int)_elevationRange.y)
				{
					cellBiome.terrain = 4;
				}

				if (cellBiome.terrain == 4)
				{
					cellBiome.plant = 0;
				}
				else if (cellBiome.plant < 3 && cell.isHasRiver)
				{
					cellBiome.plant += 1;
				}

				cell.terrainTypeIndex = cellBiome.terrain;
				cell.plantLevel = cellBiome.plant;
			}
			else
			{
				int terrain;

				if (cell.elevation == _waterLevel - 1)
				{
					var cliffs = 0;
					var slopes = 0;
					for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++ )
					{
						var neighbor = cell[d];
						if (!neighbor)
						{
							continue;
						}
						var delta = neighbor.elevation - cell.waterLevel;
						if (delta == 0)
						{
							slopes += 1;
						}
						else if (delta > 0)
						{
							cliffs += 1;
						}
					}
					if (cliffs + slopes > 3)
					{
						terrain = 1;
					}
					else if (cliffs > 0)
					{
						terrain = 3;
					}
					else if (slopes > 0)
					{
						terrain = 0;
					}
					else
					{
						terrain = 1;
					}
				}
				else if (cell.elevation >= _waterLevel)
				{
					terrain = 1;
				}
				else if (cell.elevation < 0)
				{
					terrain = 3;
				}
				else
				{
					terrain = 2;
				}

				if (terrain == 1 && temperature < _temperatureBands[0])
				{
					terrain = 2;
				}

				cell.terrainTypeIndex = terrain;
			}
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
