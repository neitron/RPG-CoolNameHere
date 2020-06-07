using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;



public class HexUnit : MonoBehaviour
{

	public static HexUnit unitPrefab;
	
	private const int VISION_RANGE = 3;
	private const float TRAVEL_SPEED = 4f;
	private const float ROTATION_SPEED = 180f;



	public HexGrid grid { get; set; }

	public HexCell location
	{
		get => _location;
		set
		{
			if (_location)
			{
				grid.DecreaseVisibility(_location, VISION_RANGE);
				_location.unit = null;
			}
			_location = value;
			_location.unit = this;
			grid.IncreaseVisibility(_location, VISION_RANGE);
			transform.localPosition = _location.position;
			grid.MakeChildOfColumn(transform, value.columnIndex);
		}
	}

	public float orientation
	{
		get => _orientation;
		set
		{
			_orientation = value;
			transform.localRotation = Quaternion.Euler(0f, value, 0f);
		}
	}

	public int speed => 24;
	public int visionRange => VISION_RANGE;


	private HexCell _location;
	private HexCell _currentTravelLocation;
	private float _orientation;
	private List<HexCell> _pathToTravel;



	private void OnEnable()
	{
		if (_location)
		{
			transform.localPosition = _location.position;

			if (_currentTravelLocation)
			{
				grid.IncreaseVisibility(_location, VISION_RANGE);
				grid.DecreaseVisibility(_currentTravelLocation, VISION_RANGE);
				_currentTravelLocation = null;
			}
		}
	}


	public void ValidateLocation()
	{
		transform.localPosition = _location.position;
	}


	public void Die()
	{
		if (_location)
		{
			grid.DecreaseVisibility(_location, VISION_RANGE);
		}
		_location.unit = null;
		Destroy(gameObject);
	}


	public void Save(BinaryWriter writer)
	{
		location.coordinates.Save(writer);
		writer.Write(orientation);
	}


	public static void Load(BinaryReader reader, HexGrid grid)
	{
		var coordinates = HexCoordinates.Load(reader);
		var orientation = reader.ReadSingle();
		grid.AddUnit(Instantiate(unitPrefab), grid[coordinates], orientation);
	}


	public bool IsValidDestination(HexCell cell)
	{
		return cell.isExplored && !cell.isUnderwater && !cell.unit;
	}


	public void Travel(List<HexCell> path)
	{
		_location.unit = null;
		_location = path[path.Count - 1];
		_location.unit = this;
		_pathToTravel = path;
		StopAllCoroutines();
		StartCoroutine(TravelPath());
	}


	private IEnumerator TravelPath()
	{
		Vector3 a;
		Vector3 b;
		var c = _pathToTravel[0].position;
		
		yield return LookAt(_pathToTravel[1].position);

		if (!_currentTravelLocation)
		{
			_currentTravelLocation = _pathToTravel[0];
		}
		grid.DecreaseVisibility(_currentTravelLocation, VISION_RANGE);
		var currentColumn = _currentTravelLocation.columnIndex;

		var t = Time.deltaTime * TRAVEL_SPEED;
		for (var i = 1; i < _pathToTravel.Count; i++)
		{
			_currentTravelLocation = _pathToTravel[i];

			a = c;
			b = _pathToTravel[i - 1].position;
			
			var nextColumn = _currentTravelLocation.columnIndex;
			if (currentColumn != nextColumn)
			{
				if (nextColumn < currentColumn - 1)
				{
					a.x -= HexMetrics.INNER_DIAMETER * HexMetrics.wrapSize;
					b.x -= HexMetrics.INNER_DIAMETER * HexMetrics.wrapSize;
				}
				else if (nextColumn > currentColumn + 1)
				{
					a.x += HexMetrics.INNER_DIAMETER * HexMetrics.wrapSize;
					b.x += HexMetrics.INNER_DIAMETER * HexMetrics.wrapSize;
				}
				grid.MakeChildOfColumn(transform, nextColumn);
				currentColumn = nextColumn;
			}

			c = (b + _currentTravelLocation.position) * 0.5f;
			grid.IncreaseVisibility(_pathToTravel[i], visionRange);

			for (; t < 1f; t += Time.deltaTime * TRAVEL_SPEED)
			{
				transform.localPosition = Bezier.GetPoint(a, b, c, t);
				var d = Bezier.GetDerivative(a,b,c,t);
				d.y = 0;
				transform.localRotation = Quaternion.LookRotation(d);

				yield return null;
			}

			grid.DecreaseVisibility(_pathToTravel[i], VISION_RANGE);
			
			t -= 1f;
		}

		_currentTravelLocation = null;
		
		a = c;
		b = _location.position;
		c = b;
		grid.IncreaseVisibility(_location, VISION_RANGE);
		for (; t < 1f; t += Time.deltaTime * TRAVEL_SPEED)
		{
			transform.localPosition = Bezier.GetPoint(a, b, c, t);
			var d = Bezier.GetDerivative(a, b, c, t);
			d.y = 0;
			transform.localRotation = Quaternion.LookRotation(d);

			yield return null;
		}

		transform.localPosition = _location.position;
		_orientation = transform.localRotation.eulerAngles.y;

		ListPool<HexCell>.Add(_pathToTravel);
		_pathToTravel = null;
	}


	private IEnumerator LookAt(Vector3 point)
	{
		if (HexMetrics.wrapping)
		{
			var xDistance = point.x - transform.localPosition.x;
			if (xDistance < -HexMetrics.INNER_RADIUS * HexMetrics.wrapSize)
			{
				point.x += HexMetrics.INNER_DIAMETER * HexMetrics.wrapSize;
			}
			else if (xDistance > HexMetrics.INNER_RADIUS * HexMetrics.wrapSize)
			{
				point.x -= HexMetrics.INNER_DIAMETER * HexMetrics.wrapSize;
			}
		}

		point.y = transform.localPosition.y;

		var fromRotation = transform.localRotation;
		var toRotation = Quaternion.LookRotation(point - transform.localPosition);
		var angle = Quaternion.Angle(fromRotation, toRotation);

		if (angle > 0)
		{
			var speed = ROTATION_SPEED / angle;
			for (var t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
			{
				transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
				yield return null;
			}
		}

		transform.LookAt(point);
		_orientation = transform.localRotation.eulerAngles.y;
	}


	public int GetMoveCost(HexCell fromCell, HexCell toCell, HexDirection direction)
	{
		var edgeType = fromCell.GetEdgeType(toCell);
		if (edgeType == HexEdgeType.Cliff)
			return -1;

		int moveCost;
		if (fromCell.IsRoadGoesThroughEdge(direction))
		{
			moveCost = 1;
		}
		else if (fromCell.walled != toCell.walled)
		{
			return -1;
		}
		else
		{
			moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
			moveCost += toCell.urbanLevel + toCell.farmLevel + toCell.plantLevel;
		}
		return moveCost;
	}

}