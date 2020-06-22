using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.IO;
using UnityEngine;
using Sirenix.Utilities;

public class HexGrid : SerializedMonoBehaviour
{



	[OdinSerialize] public Vector2Int cellCount { get; private set; }
	[OdinSerialize] public bool wrapping { get; private set; }
	public bool hasPath => _currentPathExists;


	[SerializeField] private HexCell _cellPrefab;
	[SerializeField] private HexGridChunk _chunkPrefab;

	[SerializeField] private Texture2D _noiseSource;


	[OdinSerialize] private HexCell[] _cells;
	[OdinSerialize] private int _seed;
	
	[SerializeField, HideInInspector] private HexGridChunk[] _chunks;
	[SerializeField, HideInInspector] private Vector2Int _chunkCount;
	
	private HexCellPriorityQueue _searchFrontier;
	private HexCellShaderData _cellShaderData;
	private int _searchFrontierPhase;
	private Transform[] _columns;
	private int _currentCenterColumnIndex = -1;
	private HexCell _currentPathFrom;
	private HexCell _currentPathTo;
	private bool _currentPathExists;
	private List<HexUnit> _units = new List<HexUnit>();


	public HexCell this[Vector3 position] =>
		GetCell(position);

	public HexCell this[HexCoordinates coordinates] =>
		GetCell(coordinates);

	public HexCell this[int xOffset, int zOffset] =>
		_cells[xOffset + zOffset * cellCount.x];

	public HexCell this[int index] =>
		_cells[index];

	public HexCell this[Ray ray] =>
		GetCell(ray);


	private void Awake()
	{
		HexMetrics.wrapSize = wrapping ? cellCount.x : 0;
		HexMetrics.noiseSource = _noiseSource;
		HexMetrics.InitializeHashGrid(_seed);

		_cellShaderData = gameObject.AddComponent<HexCellShaderData>();
		_cellShaderData.hexGrid = this;

		CreateMap(cellCount.x, cellCount.y, wrapping);
	}


	private void OnEnable()
	{
		if (!HexMetrics.noiseSource)
		{
			HexMetrics.wrapSize = wrapping ? cellCount.x : 0;
			HexMetrics.noiseSource = _noiseSource;
			HexMetrics.InitializeHashGrid(_seed);
			ResetVisibility();
		}
	}


	public bool CreateMap(int x, int z, bool wrapMap)
	{
		if (
			x <= 0 || x % HexMetrics.CHUNK_SIZE_X != 0 ||
			z <= 0 || z % HexMetrics.CHUNK_SIZE_Z != 0
		)
		{
			Debug.LogError("Unsupported map size.");
			return false;
		}

		ClearPath();
		ClearUnits();

		if (_columns != null)
		{
			for (var i = 0; i < _columns.Length; i++)
			{
				Destroy(_columns[i].gameObject);
			}
		}

		cellCount = new Vector2Int(x, z);
		wrapping = wrapMap;
		_currentCenterColumnIndex = -1;
		HexMetrics.wrapSize = wrapping ? cellCount.x : 0;

		_chunkCount.x = cellCount.x / HexMetrics.CHUNK_SIZE_X;
		_chunkCount.y = cellCount.y / HexMetrics.CHUNK_SIZE_Z;
		
		_cellShaderData.Initialize(x, z);

		CreateChunks();
		CreateCells();

		return true;
	}


	private void Build()
	{
		foreach (var chunk in _chunks)
		{
			chunk.Build();
		}
	}


	private void CreateChunks()
	{
		_columns = new Transform[_chunkCount.x];
		for (var x = 0; x < _chunkCount.x; x++)
		{
			_columns[x] = new GameObject("Column").transform;
			_columns[x].SetParent(transform, false);
		}

		_chunks = new HexGridChunk[_chunkCount.y * _chunkCount.x];

		for (int z = 0, index = 0; z < _chunkCount.y; z++)
		{
			for (var x = 0; x < _chunkCount.x; x++)
			{
				var chunk = _chunks[index++] = Instantiate(_chunkPrefab, _columns[x], false);
				chunk.Init();
			}
		}
	}


	private void CreateCells()
	{
		_cells = new HexCell[cellCount.y * cellCount.x];

		for (int i = 0, index = 0; i < cellCount.y; i++)
		{
			for (var j = 0; j < cellCount.x; j++)
			{
				CreateCell(j, i, index++);
			}
		}
	}


	private void CreateCell(int x, int z, int index)
	{
		// ReSharper disable once PossibleLossOfFraction
		var pos = new Vector3(
			(x + z * 0.5f - z / 2) * HexMetrics.INNER_DIAMETER, 
			0.0f, 
			z * (HexMetrics.OUTER_RADIUS * 1.5f));

		var cell = _cells[index] = Instantiate(_cellPrefab, pos, Quaternion.identity);
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.index = index;
		cell.columnIndex = x / HexMetrics.CHUNK_SIZE_X;
		cell.shaderData = _cellShaderData;

		if (wrapping)
		{
			cell.isExplorable = z > 0 && z < cellCount.y - 1;
		}
		else
		{
			cell.isExplorable = x > 0 && z > 0 && x < cellCount.x - 1 && z < cellCount.y - 1;
		}

		if (x > 0)
		{
			cell[HexDirection.W] = _cells[index - 1];

			if (wrapping && x == cellCount.x - 1)
			{
				cell[HexDirection.E] = _cells[index - x];
			} 
		}
		
		if (z > 0)
		{
			if ((z & 1) == 0)
			{ 
				cell[HexDirection.Se] = _cells[index - cellCount.x];
				if (x > 0)
					cell[HexDirection.Sw] = _cells[index - cellCount.x - 1];
				else if (wrapping)
					cell[HexDirection.Sw] = _cells[index - 1];
			}
			else
			{
				cell[HexDirection.Sw] = _cells[index - cellCount.x];
				if (x < cellCount.x - 1)
					cell[HexDirection.Se] = _cells[index - cellCount.x + 1];
				else if (wrapping)
					cell[HexDirection.Se] = _cells[index - cellCount.x * 2 + 1];
			}
		}

		cell.elevation = 0;

		AddCellToChunk(x, z, cell);
	}


	private void AddCellToChunk(int x, int z, HexCell cell)
	{
		var chunkX = x / HexMetrics.CHUNK_SIZE_X;
		var chunkZ = z / HexMetrics.CHUNK_SIZE_Z;
		var chunk = _chunks[chunkX + chunkZ * _chunkCount.x];

		var localX = x - chunkX * HexMetrics.CHUNK_SIZE_X;
		var localZ = z - chunkZ * HexMetrics.CHUNK_SIZE_Z;
		chunk.AddCell(localX + localZ * HexMetrics.CHUNK_SIZE_X, cell);
	}


	private HexCell GetCell(Vector3 position)
	{
		position = transform.InverseTransformPoint(position);
		var coords = HexCoordinates.FromPosition(position);

		return GetCell(coords);
	}


	private HexCell GetCell(HexCoordinates coordinates)
	{
		var z = coordinates.z;
		if (z < 0 || z >= cellCount.y)
		{
			return null;
		}
		var x = coordinates.x + z / 2;
		if (x < 0 || x >= cellCount.x)
		{
			return null;
		}
		return _cells[x + z * cellCount.x];
	}


	public void CenterMap(float xPosition)
	{
		var centerColumnIndex = (int) (xPosition / (HexMetrics.INNER_DIAMETER * HexMetrics.CHUNK_SIZE_X));

		if (_currentCenterColumnIndex == centerColumnIndex)
			return;

		_currentCenterColumnIndex = centerColumnIndex;

		var minColumnIndex = centerColumnIndex - _chunkCount.x / 2;
		var maxColumnIndex = centerColumnIndex + _chunkCount.x / 2;

		Vector3 position;
		position.y = position.z = 0f;
		for (var i = 0; i < _columns.Length; i++)
		{
			if (i < minColumnIndex)
				position.x = _chunkCount.x * (HexMetrics.INNER_DIAMETER * HexMetrics.CHUNK_SIZE_X);
			else if (i > maxColumnIndex)
				position.x = _chunkCount.x * -(HexMetrics.INNER_DIAMETER * HexMetrics.CHUNK_SIZE_X);
			else
				position.x = 0f;
			
			_columns[i].localPosition = position;
		}
	}


	public void ShowUi(bool isVisible)
	{
		foreach (var cell in _cells)
		{
			cell.isLabelVisible = isVisible;
		}
	}


	public void Save(BinaryWriter writer)
	{
		writer.Write(cellCount.x);
		writer.Write(cellCount.y);
		writer.Write(wrapping);

		for (var i = 0; i < _cells.Length; i++)
		{
			_cells[i].Save(writer);
		}

		writer.Write(_units.Count);
		for (var i = 0; i < _units.Count; i++)
		{
			_units[i].Save(writer);
		}
	}

	public void Load(BinaryReader reader, int header)
	{
		ClearPath();
		ClearUnits();

		StopAllCoroutines();

		var x = 20;
		var y = 15;

		if (header >= 1)
		{
			x = reader.ReadInt32();
			y = reader.ReadInt32();
		}

		var wrapMap = header >= 3 && reader.ReadBoolean();

		if ((x != cellCount.x || y != cellCount.y || this.wrapping != wrapMap) && !CreateMap(x, y, wrapping))
		{
			return;
		}

		var originalImmediateMode = _cellShaderData.immediateMode;
		_cellShaderData.immediateMode = true;
		for (var i = 0; i < _cells.Length; i++)
		{
			_cells[i].Load(reader, header);
		}
		
		for (int i = 0; i < _chunks.Length; i++)
		{
			_chunks[i].Refresh();
		}

		if (header >= 4)
		{
			int unitCount = reader.ReadInt32();
			for (int i = 0; i < unitCount; i++)
			{
				HexUnit.Load(reader, this, header);
			}
		}

		_cellShaderData.immediateMode = originalImmediateMode;
	}


	public HexCell GetCell(Ray ray)
	{
		if (Physics.Raycast(ray, out var hit))
		{
			return this[hit.point];
		}
		return null;
	}


	private void ClearUnits()
	{
		foreach (var unit in _units)
		{
			unit.Die();
		}

		_units.Clear();
	}


	public void AddUnit(HexUnit unit, HexCell location, float orientation)
	{
		_units.Add(unit);
		unit.grid = this;
		unit.location = location;
		unit.orientation = orientation;

		location.owner = unit.owner;
		location.shaderData.RefreshOwner(location, unit.owner);
	}


	public void MakeChildOfColumn(Transform child, int columnIndex)
	{
		child.SetParent(_columns[columnIndex], false);
	}


	public void RemoveUnit(HexUnit unit)
	{
		_units.Remove(unit);
		unit.Die();
	}


	public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit)
	{
		ClearPath();
		_currentPathFrom = fromCell;
		_currentPathTo = toCell;
		_currentPathExists = Search(fromCell, toCell, unit);
		ShowPath(unit.speed);
	}


	private bool Search(HexCell fromCell, HexCell toCell, HexUnit unit)
	{
		var speed = unit.speed;
		_searchFrontierPhase += 2;
		if (_searchFrontier == null)
		{
			_searchFrontier = new HexCellPriorityQueue();
		}
		else
		{
			_searchFrontier.Clear();
		}

		fromCell.searchPhase = _searchFrontierPhase;
		fromCell.distance = 0;
		_searchFrontier.Enqueue(fromCell);

		while (_searchFrontier.count > 0)
		{
			var current = _searchFrontier.Dequeue();
			current.searchPhase++; // TODO: NULL REF HERE
			
			if (current == toCell)
			{
				return true;
			}

			var currentTurn = (current.distance - 1) / speed;

			for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
			{
				var neighbor = current[d];

				if (neighbor == null ||
				    neighbor.searchPhase > _searchFrontierPhase)
					continue;

				if (!unit.IsValidDestination(neighbor))
					continue;

				var moveCost = unit.GetMoveCost(current, neighbor, d);
				if (moveCost < 0)
					continue;

				var distance = current.distance + moveCost;
				var turn = (distance - 1) / speed;
				if (turn > currentTurn)
				{
					distance = turn * speed + moveCost;
				}

				if (neighbor.searchPhase < _searchFrontierPhase)
				{
					neighbor.searchPhase = _searchFrontierPhase;
					neighbor.distance = distance;
					neighbor.pathFrom = current;
					neighbor.searchHeuristic = neighbor.coordinates.DistanceTo(toCell.coordinates);
					_searchFrontier.Enqueue(neighbor);
				}
				else if (neighbor.distance > distance)
				{
					var oldPriority = neighbor.searchPriority;
					neighbor.distance = distance;
					neighbor.pathFrom = current;
					_searchFrontier.Change(neighbor, oldPriority);
				}
			}
		}
		return false;
	}


	private List<HexCell> GetVisibleCells(HexCell fromCell, int range)
	{
		var visibleCells = ListPool<HexCell>.Get();

		_searchFrontierPhase += 2;
		if (_searchFrontier == null)
		{
			_searchFrontier = new HexCellPriorityQueue();
		}
		else
		{
			_searchFrontier.Clear();
		}

		range += fromCell.viewElevation;
		fromCell.searchPhase = _searchFrontierPhase;
		fromCell.distance = 0;
		_searchFrontier.Enqueue(fromCell);

		var fromCoordinates = fromCell.coordinates;
		while (_searchFrontier.count > 0)
		{
			var current = _searchFrontier.Dequeue();
			current.searchPhase++;

			visibleCells.Add(current);

			for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
			{
				var neighbor = current[d];

				if (neighbor == null ||
					neighbor.searchPhase > _searchFrontierPhase ||
					!neighbor.isExplorable)
					continue;

				var distance = current.distance + 1;
				if (distance + neighbor.viewElevation > range ||
				    distance > fromCoordinates.DistanceTo(neighbor.coordinates))
				{
					continue;
				}

				if (neighbor.searchPhase < _searchFrontierPhase)
				{
					neighbor.searchPhase = _searchFrontierPhase;
					neighbor.distance = distance;
					neighbor.searchHeuristic = 0;
					_searchFrontier.Enqueue(neighbor);
				}
				else if (neighbor.distance > distance)
				{
					var oldPriority = neighbor.searchPriority;
					neighbor.distance = distance;
					_searchFrontier.Change(neighbor, oldPriority);
				}
			}
		}
		return visibleCells;
	}


	public void IncreaseVisibility(HexCell fromCell, int range)
	{
		var cells = GetVisibleCells(fromCell, range);

		foreach (var cell in cells)
		{
			cell.IncreaseVisibility();
		}

		ListPool<HexCell>.Add(cells);
	}


	public void DecreaseVisibility(HexCell fromCell, int range)
	{
		var cells = GetVisibleCells(fromCell, range);

		foreach (var cell in cells)
		{
			cell.DecreaseVisibility();
		}

		ListPool<HexCell>.Add(cells);
	}


	private void ShowPath(int speed)
	{
		var startColor = new Color(0.32f, 0.46f, 1f);
		var endColor = new Color(1f, 0.46f, 0.4f);
		
		if (_currentPathExists)
		{
			var remainingColor = new Color(1f, 1f, 1f, 0.50f);
			var availableColor = Color.white;

			var current = _currentPathTo;
			while (current != _currentPathFrom)
			{
				var turn = (current.distance - 1) / speed;
				current.label = turn.ToString();
				current.EnableHighlight(turn == 0 ? availableColor : remainingColor);
				current = current.pathFrom;
			}
		}
		
		_currentPathFrom.EnableHighlight(startColor);
		_currentPathTo.EnableHighlight(endColor);
	}


	public void ClearPath()
	{
		if (_currentPathExists)
		{
			var current = _currentPathTo;
			while (current != _currentPathFrom)
			{
				current.label = null;
				current.DisableHighlight();
				current = current.pathFrom;
			}
			current.DisableHighlight();
			_currentPathExists = false;
		}
		else if (_currentPathFrom)
		{
			_currentPathFrom.DisableHighlight();
			_currentPathTo.DisableHighlight();
		}
		_currentPathFrom = _currentPathTo = null;
	}


	public List<HexCell> GetPath()
	{
		if (!_currentPathExists)
		{
			return null;
		}

		var path = ListPool<HexCell>.Get();
		for (var c = _currentPathTo; c != _currentPathFrom; c = c.pathFrom)
		{
			path.Add(c);
		}
		path.Add(_currentPathFrom);
		path.Reverse();

		return path;
	}


	public void ResetVisibility()
	{
		for (int i = 0; i < _cells.Length; i++)
		{
			_cells[i].ResetVisibility();
		}
		for (int i = 0; i < _units.Count; i++)
		{
			var unit = _units[i];
			IncreaseVisibility(unit.location, unit.visionRange);
		}
	}


}
