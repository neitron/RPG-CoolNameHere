using System.Collections;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;



public class HexGrid : SerializedMonoBehaviour
{


	[OdinSerialize]
	public Vector2Int cellCount { get; private set; }


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



	public HexCell this[Vector3 position] =>
		GetCell(position);

	public HexCell this[HexCoordinates coordinates] =>
		GetCell(coordinates);

	public HexCell this[int xOffset, int zOffset] =>
		_cells[xOffset + zOffset * cellCount.x];

	public HexCell this[int index] =>
		_cells[index];



	private void Awake()
	{
		InitInternal();
		_cellShaderData = gameObject.AddComponent<HexCellShaderData>();
		CreateMap(cellCount.x, cellCount.y);
	}


	public bool CreateMap(int x, int z)
	{
		if (
			x <= 0 || x % HexMetrics.CHUNK_SIZE_X != 0 ||
			z <= 0 || z % HexMetrics.CHUNK_SIZE_Z != 0
		)
		{
			Debug.LogError("Unsupported map size.");
			return false;
		}

		if (_chunks != null)
		{
			for (var i = 0; i < _chunks.Length; i++)
			{
				if (_chunks[i] != null)
					Destroy(_chunks[i].gameObject);
			}
		}
		_chunks = null;
		if (_cells != null)
		{
			Clear();
			_cells = null;
		}

		cellCount = new Vector2Int(x, z);
		_chunkCount.x = cellCount.x / HexMetrics.CHUNK_SIZE_X;
		_chunkCount.y = cellCount.y / HexMetrics.CHUNK_SIZE_Z;

		_cellShaderData.Initialize(x, z);

		CreateChunks();
		CreateCells();

		return true;
	}


	[Button]
	private void Generate()
	{
		InitInternal();

		CreateMap(cellCount.x, cellCount.y);

		Build();
	}


	private void InitInternal()
	{
		if (HexMetrics.noiseSource == null)
		{
			HexMetrics.noiseSource = _noiseSource;
			HexMetrics.InitializeHashGrid(_seed);
		}
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
		_chunks = new HexGridChunk[_chunkCount.y * _chunkCount.x];

		for (int i = 0, index = 0; i < _chunkCount.y; i++)
		{
			for (var j = 0; j < _chunkCount.x; j++)
			{
				var chunk = _chunks[index++] = Instantiate(_chunkPrefab, transform, true);
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
			(x + z * 0.5f - z / 2) * (HexMetrics.INNER_RADIUS * 2.0f), 
			0.0f, 
			z * (HexMetrics.OUTER_RADIUS * 1.5f));

		var cell = _cells[index] = Instantiate(_cellPrefab, pos, Quaternion.identity);
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.index = index;
		cell.shaderData = _cellShaderData;

		if (x > 0)
		{
			cell[HexDirection.W] = _cells[index - 1];
		}
		
		if (z > 0)
		{
			if ((z & 1) == 0)
			{ 
				cell[HexDirection.Se] = _cells[index - cellCount.x];
				if (x > 0)
					cell[HexDirection.Sw] = _cells[index - cellCount.x - 1];
			}
			else
			{
				cell[HexDirection.Sw] = _cells[index - cellCount.x];
				if (x < cellCount.x - 1)
					cell[HexDirection.Se] = _cells[index - cellCount.x + 1];
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


	[Button]
	private void Clear()
	{
		if (_chunks == null)
			return;

		foreach (var chunk in _chunks)
			if(chunk != null)
				DestroyImmediate(chunk.gameObject);
	}


	private HexCell GetCell(Vector3 position)
	{
		position = transform.InverseTransformPoint(position);
		var coords = HexCoordinates.FromPosition(position);
		var index = coords.x + coords.z * cellCount.x + coords.z / 2;
		return _cells[index];
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

		for (var i = 0; i < _cells.Length; i++)
		{
			_cells[i].Save(writer);
		}
	}

	public void Load(BinaryReader reader, int header)
	{
		StopAllCoroutines();

		var x = 20;
		var y = 15;

		if (header >= 1)
		{
			x = reader.ReadInt32();
			y = reader.ReadInt32();
		}

		if ((x != cellCount.x || y != cellCount.y) && !CreateMap(x, y))
		{
			return;
		}

		for (var i = 0; i < _cells.Length; i++)
		{
			_cells[i].Load(reader, header);
		}
		for (int i = 0; i < _chunks.Length; i++)
		{
			_chunks[i].Refresh();
		}
	}


	public void FindPath(HexCell fromCell, HexCell toCell)
	{
		StopAllCoroutines();
		StartCoroutine(Search(fromCell, toCell));
	}


	private IEnumerator Search(HexCell fromCell, HexCell toCell)
	{
		_searchFrontierPhase += 2;

		if (_searchFrontier == null)
		{
			_searchFrontier = new HexCellPriorityQueue();
		}
		else
		{
			_searchFrontier.Clear();
		}

		for (var i = 0; i < _cells.Length; i++)
		{
			_cells[i].distance = int.MaxValue;
			_cells[i].DisableHighlight();
		}

		fromCell.EnableHighlight(Color.blue);
		toCell.EnableHighlight(Color.red);

		var delay = new WaitForSeconds(1f / 60f);

		fromCell.searchPhase = _searchFrontierPhase;
		fromCell.distance = 0;
		_searchFrontier.Enqueue(fromCell);

		while (_searchFrontier.count > 0)
		{
			//yield return delay;
			
			var current = _searchFrontier.Dequeue();
			current.searchPhase++; // TODO: NULL REF HERE
			
			if (current == toCell)
			{
				current = current.pathFrom;
				while (current != fromCell)
				{
					current.EnableHighlight(Color.white);
					current = current.pathFrom;
				}
				break;
			}

			for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
			{
				var neighbor = current[d];

				if (neighbor == null ||
				    neighbor.searchPhase > _searchFrontierPhase)
					continue;
				
				if (neighbor.isUnderwater)
					continue;

				var edgeType = current.GetEdgeType(neighbor);
				if (edgeType == HexEdgeType.Cliff)
					continue;

				var distance = current.distance;
				if (current.IsRoadGoesThroughEdge(d))
				{
					distance += 1;
				}
				else if (current.walled != neighbor.walled)
				{
					continue;
				}
				else
				{
					distance += edgeType == HexEdgeType.Flat ? 5 : 10;
					distance += neighbor.plantLevel;
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
		yield break;
	}


}
