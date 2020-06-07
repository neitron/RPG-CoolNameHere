using UnityEngine;



public class NewMapMenu : MonoBehaviour
{


	[SerializeField] private HexGrid _hexGrid;
	[SerializeField] private HexMapGenerator _mapGenerator;
	
	
	private Canvas _canvas;
	private bool _generateMaps = true;
	private bool _wrapMaps = true;



	private void Awake()
	{
		_canvas = GetComponent<Canvas>();
	}


	public void Open()
	{
		_canvas.enabled = true;
		HexMapCamera.isLocked = true;
	}


	public void Close()
	{
		_canvas.enabled = false;
		HexMapCamera.isLocked = false;
	}


	private void CreateMap(int x, int z)
	{
		if (_generateMaps)
		{
			_mapGenerator.GenerateMap(x, z, _wrapMaps);
		}
		else
		{
			_hexGrid.CreateMap(x, z, _wrapMaps);
		}

		HexMapCamera.ValidatePosition();
		Close();
	}


	public void ToggleMapGeneration(bool toggle)
	{
		_generateMaps = toggle;
	}


	public void ToggleMapWrapping(bool toggle)
	{
		_wrapMaps = toggle;
	}


	public void CreateSmallMap()
	{
		CreateMap(20, 15);
	}


	public void CreateMediumMap()
	{
		CreateMap(40, 30);
	}


	public void CreateLargeMap()
	{
		CreateMap(80, 60);
	}


}
