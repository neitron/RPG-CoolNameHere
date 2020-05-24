using System.IO;
using System.Linq;
using UnityEngine;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;


public class HexCell : SerializedMonoBehaviour
{


	[OdinSerialize]
	public HexGridChunk chunk { get; set; }

	[OdinSerialize]
	public HexCoordinates coordinates { get; set; }

	[OdinSerialize]
	public HexCell[] neighbors { get; private set; }
	
	[OdinSerialize]
	public bool[] roads { get; private set; }


	public HexDirection incomingRiverDirection { get; private set; }
	public HexDirection outgoingRiverDirection { get; private set; }
	public bool isHasIncomingRiver { get; private set; }
	public bool isHasOutgoingRiver { get; private set; }
	public HexCellShaderData shaderData { get; set; }
	public int index { get; set; }



	[ShowInInspector]
	public int terrainTypeIndex
	{
		get => _terrainTypeIndex;
		set
		{
			if (_terrainTypeIndex == value) 
				return;

			_terrainTypeIndex = value;
			shaderData.RefreshTerrain(this);
		}
	}

	[ShowInInspector]
	public int elevation
	{
		get => _elevation;
		set
		{
			if (_elevation == value)
			{
				return;
			}
			_elevation = value;

			RefreshPosition();

			ValidateRivers();
			ValidateRoads();

			Refresh();
		}
	}

	[ShowInInspector]
	public int waterLevel
	{
		get => _waterLevel;
		set
		{
			if (_waterLevel == value)
			{
				return;
			}

			_waterLevel = value;
			ValidateRivers();
			Refresh();
		}
	}

	[ShowInInspector]
	public int urbanLevel
	{
		get => _urbanLevel;
		set
		{
			if (_urbanLevel == value)
			{
				return;
			}

			_urbanLevel = value;
			RefreshSelfOnly();
		}
	}

	[ShowInInspector]
	public int farmLevel
	{
		get => _farmLevel;
		set
		{
			if (_farmLevel == value)
			{
				return;
			}

			_farmLevel = value;
			RefreshSelfOnly();
		}
	}

	[ShowInInspector]
	public int plantLevel
	{
		get => _plantLevel;
		set
		{
			if (_plantLevel == value)
			{
				return;
			}

			_plantLevel = value;
			RefreshSelfOnly();
		}
	}

	[ShowInInspector]
	public bool walled
	{
		get => _walled;
		set
		{
			if (_walled == value) 
				return;
			
			_walled = value;
			Refresh();
		}
	}
	
	[ShowInInspector]
	public int specialIndex
	{
		get => _specialIndex;
		set
		{
			if (_specialIndex == value)
			{
				return;
			}

			_specialIndex = value;

			if (_specialIndex > 0 && isHasRoad)
			{
				RemoveRoads();
			}

			RefreshSelfOnly();
		}
	}


	[ShowInInspector]
	public int distance
	{
		get => _distance;
		set
		{
			_distance = value;
			UpdateDistanceLabel();
		}
	}


	public HexCell nextWithSamePriority { get; set; }
	public int searchHeuristic { get; set; }
	public HexCell pathFrom { get; set; }
	public int searchPhase { get; set; }
	


	public int searchPriority => distance + searchHeuristic;


	/// <summary>
	/// Provides an access to it`s neighbors
	/// </summary>
	/// <param name="direction">The direction to extract a cell</param>
	/// <returns>Hex cell in the provided direction</returns>
	public HexCell this[HexDirection direction]
	{
		get => neighbors[(int)direction];
		set
		{
			neighbors[(int)direction] = value;
			value.neighbors[(int)direction.Opposite()] = this;
		}
	}
	
	public Vector3 position => 
		transform.localPosition;

	public bool isHasRiver => 
		isHasIncomingRiver || isHasOutgoingRiver;

	public bool isHasRoad => 
		roads.Any(road => road);

	public bool isRiverBeginOrEnd => 
		isHasIncomingRiver != isHasOutgoingRiver;

	public bool isUnderwater =>
		_waterLevel > _elevation;

	public float streamBedY => 
		(_elevation + HexMetrics.STREAM_BED_ELEVATION_OFFSET) * HexMetrics.ELEVATION_STEP;

	public float riverSurfaceY => 
		(_elevation + HexMetrics.WATER_ELEVATION_OFFSET) * HexMetrics.ELEVATION_STEP;

	public float waterSurfaceY =>
		(_waterLevel + HexMetrics.WATER_ELEVATION_OFFSET) * HexMetrics.ELEVATION_STEP;

	public HexDirection riverBeginOrEndDirection =>
		isHasIncomingRiver ? incomingRiverDirection : outgoingRiverDirection;

	public bool isSpecial => 
		_specialIndex > 0;


	public int viewElevation =>
		_elevation >= _waterLevel ? _elevation : _waterLevel;



	[SerializeField, HideInInspector]
	private int _terrainTypeIndex;
	
	[SerializeField, HideInInspector]
	private int _elevation = int.MinValue;

	[SerializeField, HideInInspector]
	private int _waterLevel = int.MinValue;

	[SerializeField, HideInInspector]
	private int _urbanLevel;

	[SerializeField, HideInInspector]
	private int _farmLevel;

	[SerializeField, HideInInspector]
	private int _plantLevel;

	[SerializeField, HideInInspector]
	private bool _walled;

	[SerializeField, HideInInspector]
	private int _specialIndex;

	[SerializeField, HideInInspector] 
	private int _distance;



	public HexEdgeType GetEdgeType(HexDirection direction) =>
		HexMetrics.GetEdgeType(elevation, neighbors[(int) direction].elevation);


	public HexEdgeType GetEdgeType(HexCell otherCell) =>
		HexMetrics.GetEdgeType(elevation, otherCell.elevation);
	

	public bool IsRiverGoesThroughEdge(HexDirection direction) =>
		isHasIncomingRiver && incomingRiverDirection == direction ||
		isHasOutgoingRiver && outgoingRiverDirection == direction;


	public bool IsRoadGoesThroughEdge(HexDirection direction) =>
		roads[(int) direction];


	public bool IsValidRiverDestination(HexCell neighbor) =>
		neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);


	public int GetElevationDifference(HexDirection direction)
	{
		var difference = elevation - this[direction].elevation;  // TODO: NULL REF HERE
		return difference >= 0 ? difference : -difference;
	}


	private void ValidateRoads()
	{
		for (var i = 0; i < roads.Length; i++)
		{
			if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
			{
				SetRoad(i, false);
			}
		}
	}


	private void RefreshPosition()
	{
		var pos = transform.localPosition;
		pos.y = _elevation * HexMetrics.ELEVATION_STEP;
		pos.y += (HexMetrics.SampleNoise(pos).y * 2.0f - 1.0f) * HexMetrics.ELEVATION_PERTURB_STRENGTH;
		transform.localPosition = pos;

		// TODO: remove UI helper labels from here
		var labelPosition = _coords.rectTransform.position;
		labelPosition.y = pos.y + 0.001f;
		_coords.rectTransform.position = labelPosition;
	}



	private void ValidateRivers()
	{
		if (isHasOutgoingRiver && !IsValidRiverDestination(this[outgoingRiverDirection]))
		{
			RemoveOutgoingRiver();
		}

		if (isHasIncomingRiver && this[incomingRiverDirection].IsValidRiverDestination(this))
		{
			RemoveIncomingRiver();
		}
	}


	public void SetOutgoingRiver(HexDirection direction)
	{
		if (isHasOutgoingRiver && outgoingRiverDirection == direction)
		{
			return;
		}

		var neighbor = this[direction];

		if (!IsValidRiverDestination(neighbor))
		{
			return;
		}

		RemoveOutgoingRiver();
		if (isHasIncomingRiver && incomingRiverDirection == direction)
		{
			RemoveIncomingRiver();
		}

		isHasOutgoingRiver = true;
		outgoingRiverDirection = direction;
		specialIndex = 0;

		neighbor.RemoveIncomingRiver();
		neighbor.isHasIncomingRiver = true;
		neighbor.incomingRiverDirection = direction.Opposite();
		neighbor.specialIndex = 0;

		SetRoad((int) direction, false);
	}


	public void RemoveOutgoingRiver()
	{
		if (!isHasOutgoingRiver)
		{
			return;
		}
		isHasOutgoingRiver = false;
		RefreshSelfOnly();

		var neighbor = this[outgoingRiverDirection];
		neighbor.isHasIncomingRiver = false; // TODO: NULL REF HERE
		neighbor.RefreshSelfOnly();
	}


	public void RemoveIncomingRiver()
	{
		if (!isHasIncomingRiver)
		{
			return;
		}
		isHasIncomingRiver = false;
		RefreshSelfOnly();

		var neighbor = this[incomingRiverDirection];
		neighbor.isHasOutgoingRiver = false;
		neighbor.RefreshSelfOnly();
	}


	public void RemoveRiver()
	{
		RemoveIncomingRiver();
		RemoveOutgoingRiver();
	}


	public void RemoveRoads()
	{
		for (var i = 0; i < roads.Length; i++)
		{
			if (roads[i])
				SetRoad(i, false);
		}
	}


	public void AddRoad(HexDirection direction)
	{
		var index = (int) direction;
		if (!roads[index] &&
			!isSpecial && !this[direction].isSpecial && 
			!IsRiverGoesThroughEdge(direction) && GetElevationDifference(direction) <= 1)
		{
			SetRoad(index, true);
		}
	}


	public void SetRoad(int index, bool state)
	{ 
		roads[index] = state;

		neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
		neighbors[index].RefreshSelfOnly();
		RefreshSelfOnly();
	}


	private void RefreshSelfOnly()
	{
		chunk.Refresh();
	}


	public void Refresh()
	{
		if (!chunk)
			return;

		chunk.Refresh();

		foreach (var neighbor in neighbors)
		{
			if (neighbor != null && neighbor.chunk != chunk)
			{
				neighbor.chunk.Refresh();
			}
		}
	}


	public void Save(BinaryWriter writer)
	{
		writer.Write((byte)_terrainTypeIndex);
		writer.Write((byte)(_elevation + 127));
		writer.Write((byte)_waterLevel);
		writer.Write((byte)_urbanLevel);
		writer.Write((byte)_farmLevel);
		writer.Write((byte)_plantLevel);
		writer.Write((byte)_specialIndex);
		writer.Write(_walled);

		if (isHasIncomingRiver)
		{
			writer.Write((byte)(incomingRiverDirection + 128));
		}
		else
		{
			writer.Write((byte)0);
		}

		if (isHasOutgoingRiver)
		{
			writer.Write((byte)(outgoingRiverDirection + 128));
		}
		else
		{
			writer.Write((byte)0);
		}

		var roadFlags = 0;
		for (var i = 0; i < roads.Length; i++)
		{
			if (roads[i])
			{
				roadFlags |= 1 << i;
			}
		}
		writer.Write((byte)roadFlags);
	}


	public void Load(BinaryReader reader, int header)
	{
		_terrainTypeIndex = reader.ReadByte();
		shaderData.RefreshTerrain(this);

		_elevation = reader.ReadByte();
		if (header >= 2)
		{
			_elevation -= 127;
		}
		RefreshPosition();

		_waterLevel = reader.ReadByte();
		_urbanLevel = reader.ReadByte();
		_farmLevel = reader.ReadByte();
		_plantLevel = reader.ReadByte();
		_specialIndex = reader.ReadByte();
		_walled = reader.ReadBoolean();

		var riverData = reader.ReadByte();
		if (riverData >= 128)
		{
			isHasIncomingRiver = true;
			incomingRiverDirection = (HexDirection)(riverData - 128);
		}
		else
		{
			isHasIncomingRiver = false;
		}

		riverData = reader.ReadByte();
		if (riverData >= 128)
		{
			isHasOutgoingRiver = true;
			outgoingRiverDirection = (HexDirection)(riverData - 128);
		}
		else
		{
			isHasOutgoingRiver = false;
		}

		int roadFlags = reader.ReadByte();
		for (var i = 0; i < roads.Length; i++)
		{
			roads[i] = (roadFlags & (1 << i)) != 0;
		}
	}


	public void SetMapData(float data)
	{
		shaderData.SetMapData(this, data);
	}


	#region Temporary UI region

	[SerializeField, UsedImplicitly]
	private TextMeshPro _coords;
	public string label
	{
		get => _coords.text;
		set => _coords.text = value;
	}


	public bool isLabelVisible
	{
		get => _coords.enabled;
		set => _coords.enabled = value;
	}


	void UpdateDistanceLabel()
	{
		label = _distance == int.MaxValue ? "" : _distance.ToString();
	}


	public void DisableHighlight()
	{
		var highlight = transform.GetChild(1).GetComponent<SpriteRenderer>();
		highlight.enabled = false;
	}


	public void EnableHighlight(Color color)
	{
		var highlight = transform.GetChild(1).GetComponent<SpriteRenderer>();
		highlight.enabled = true;
		highlight.color = color;
	}

	#endregion


}
