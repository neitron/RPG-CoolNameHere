using Sirenix.OdinInspector;
using UnityEngine;


[HideMonoScript]
public class HexMapCamera : MonoBehaviour
{


	private static HexMapCamera _instance;

	
	[HorizontalGroup("Split", LabelWidth = 90)]
	[SerializeField, LabelText("For Min Zoom"), BoxGroup("Split/Move Speed")] private float _moveSpeedMinZoom;
	[SerializeField, LabelText("For Max Zoom"), BoxGroup("Split/Move Speed")] private float _moveSpeedMaxZoom;
	[SerializeField, LabelText("Min"), BoxGroup("Split/Stick Zoom")] private float _stickMinZoom;
	[SerializeField, LabelText("Max"), BoxGroup("Split/Stick Zoom")] private float _stickMaxZoom;
	[SerializeField, LabelText("Min"), BoxGroup("Split/Swivel Zoom")] private float _swivelMinZoom;
	[SerializeField, LabelText("Max"), BoxGroup("Split/Swivel Zoom")] private float _swivelMaxZoom;

	[SerializeField] private HexGrid _grid;


	private Transform _swivel;
	private Transform _stick;

	private float _zoom = 1.0f;


	public static bool isLocked
	{
		set => _instance.enabled = !value;
	}



	private void Awake()
	{
		_instance = this;
		_swivel = transform.GetChild(0);
		_stick = _swivel.GetChild(0);
	}


	private void Update()
	{
		// TODO: consider to use new Unity`s Input system

		// Zoom
		var zoomDelta = Input.GetAxis("Mouse ScrollWheel");
		if (!Mathf.Approximately(zoomDelta, 0.0f))
		{
			AdjustZoom(zoomDelta);
		}

		// Moving
		var xDelta = Input.GetAxis("Horizontal");
		var zDelta = Input.GetAxis("Vertical");
		if (!Mathf.Approximately(xDelta, 0.0f) || !Mathf.Approximately(zDelta, 0.0f))
		{
			AdjustPosition(xDelta, zDelta);
		}
	}


	private void AdjustPosition(float xDelta, float zDelta)
	{
		var direction = new Vector3(xDelta, 0.0f, zDelta).normalized;
		var damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
		var distance = Mathf.Lerp(_moveSpeedMinZoom, _moveSpeedMaxZoom, _zoom) * damping * Time.deltaTime;

		var position = transform.localPosition;
		position += direction * distance;
		transform.localPosition = ClampPosition(position);
	}


	private Vector3 ClampPosition(Vector3 position)
	{
		var xMax = (_grid.cellCount.x - 0.5f) * (2.0f * HexMetrics.INNER_RADIUS);
		position.x = Mathf.Clamp(position.x, 0.0f, xMax);

		var zMax = (_grid.cellCount.y -1.0f) * (1.5f * HexMetrics.OUTER_RADIUS);
		position.z = Mathf.Clamp(position.z, 0.0f, zMax);

		return position;
	}


	private void AdjustZoom(float delta)
	{
		_zoom = Mathf.Clamp01(_zoom + delta);

		var distance = Mathf.Lerp(_stickMinZoom, _stickMaxZoom, _zoom);
		_stick.localPosition = new Vector3(0.0f, 0.0f, distance);

		var angle = Mathf.LerpAngle(_swivelMinZoom, _swivelMaxZoom, _zoom);
		_swivel.localRotation = Quaternion.Euler(angle, 0.0f, 0.0f);
	}


	public static void ValidatePosition()
	{
		_instance.AdjustPosition(0f, 0f);
	}


}
