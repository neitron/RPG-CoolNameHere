using System.IO;
using System.Linq;
using UnityEngine;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using System.Dynamic;

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
	public bool isExplorable { get; set; }

	public HexCellShaderData shaderData { get; set; }

	public HexCell nextWithSamePriority { get; set; }
	public HexCell pathFrom { get; set; }
	public HexUnit unit { get; set; }

	public int searchHeuristic { get; set; }
	public int searchPhase { get; set; }
	public int columnIndex { get; set; }
	public int distance { get; set; }
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

			var originalViewElevation = viewElevation;
			_elevation = value;
			if (viewElevation != originalViewElevation)
			{
				shaderData.ViewElevationChanged();
			}

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
			var originalViewElevation = viewElevation;
			_waterLevel = value;
			if (viewElevation != originalViewElevation)
			{
				shaderData.ViewElevationChanged();
			}

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
	public bool isExplored
	{
		get => _isExplored && isExplorable;
		private set => _isExplored = value;
	}


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


	public int searchPriority => 
		distance + searchHeuristic;

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

	public bool isSpecial => 
		_specialIndex > 0;

	public bool isVisible =>
		_visibility > 0 && isExplorable;	

	public float streamBedY => 
		(_elevation + HexMetrics.STREAM_BED_ELEVATION_OFFSET) * HexMetrics.ELEVATION_STEP;

	public float riverSurfaceY => 
		(_elevation + HexMetrics.WATER_ELEVATION_OFFSET) * HexMetrics.ELEVATION_STEP;

	public float waterSurfaceY =>
		(_waterLevel + HexMetrics.WATER_ELEVATION_OFFSET) * HexMetrics.ELEVATION_STEP;

	public HexDirection riverBeginOrEndDirection =>
		isHasIncomingRiver ? incomingRiverDirection : outgoingRiverDirection;



	public int viewElevation =>
		_elevation >= _waterLevel ? _elevation : _waterLevel;



	private int _waterLevel = int.MinValue;
	private int _elevation = int.MinValue;
	private int _terrainTypeIndex;
	private int _specialIndex;
	private int _plantLevel;
	private int _urbanLevel;
	private int _visibility;
	private int _farmLevel;
	private bool _walled;



	public bool IsRiverGoesThroughEdge(HexDirection direction) => isHasIncomingRiver && incomingRiverDirection == direction || isHasOutgoingRiver && outgoingRiverDirection == direction;
	public bool IsValidRiverDestination(HexCell neighbor) => neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
	public HexEdgeType GetEdgeType(HexDirection direction) => HexMetrics.GetEdgeType(elevation, neighbors[(int) direction].elevation);
	public HexEdgeType GetEdgeType(HexCell otherCell) => HexMetrics.GetEdgeType(elevation, otherCell.elevation);
	public bool IsRoadGoesThroughEdge(HexDirection direction) => roads[(int) direction];
	
	
	public void IncreaseVisibility()
	{
		_visibility++;

		if (_visibility == 1)
		{
			isExplored = true;
			shaderData.RefreshVisibility(this);
		}
	}


	public void DecreaseVisibility()
	{
		_visibility--;
		if (_visibility == 0)
		{
			shaderData.RefreshVisibility(this);
		}
	}


	public int GetElevationDifference(HexDirection direction)
	{
		var difference = elevation - this[direction].elevation;
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
		var labelPosition = _label.rectTransform.position;
		labelPosition.y = pos.y + 0.001f;
		_label.rectTransform.position = labelPosition;
	}


	private void ValidateRivers()
	{
		if (isHasOutgoingRiver && !IsValidRiverDestination(this[outgoingRiverDirection]))
		{
			RemoveOutgoingRiver();
		}

		if (isHasIncomingRiver && !this[incomingRiverDirection].IsValidRiverDestination(this))
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
		neighbor.isHasIncomingRiver = false;
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
		if (unit)
			unit.ValidateLocation();
	}


	public void Refresh()
	{
		if (!chunk)
			return;

		chunk.Refresh();
		if (unit)
			unit.ValidateLocation();

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
		writer.Write(isExplored);
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

		isExplored = header >= 5 && reader.ReadBoolean();
		shaderData.RefreshVisibility(this);
	}


	public void SetMapData(float data)
	{
		shaderData.SetMapData(this, data);
	}


	#region Temporary UI region

	[SerializeField, UsedImplicitly]
	private TextMeshPro _label;

	private bool _isExplored;

	public string label
	{
		get => _label.text;
		set => _label.text = value;
	}


	public bool isLabelVisible
	{
		get => _label.enabled;
		set => _label.enabled = value;
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


	public void ResetVisibility()
	{
		if (_visibility > 0)
		{
			_visibility = 0;
			shaderData.RefreshVisibility(this);
		}
	}


}
