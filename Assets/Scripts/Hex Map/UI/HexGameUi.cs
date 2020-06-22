using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;


public class HexGameUi : MonoBehaviour
{


	[SerializeField] private HexGrid _grid;
	[SerializeField] private Canvas _canvas;
	[SerializeField] private TextMeshProUGUI _cellView;


	private HexCell _currentCell;
	private HexUnit _selectedUnit;



	[UsedImplicitly]
	public void SetEditMode(bool toggle)
	{
		_canvas.enabled = !toggle;
		enabled = !toggle;
		_grid.ShowUi(!toggle);
		_grid.ClearPath();

		if (toggle)
		{
			Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
		}
		else
		{
			Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
		}
	}


	void Update()
	{
		ShowCurrentCellData();
		
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			if (Input.GetMouseButtonDown(0))
			{
				DoSelection();
			}
			else if (_selectedUnit)
			{
				if (Input.GetMouseButtonDown(1))
				{
					DoMove();
				}
				else
				{
					DoPathfinding();
				}
			}
		}
	}


	private void ShowCurrentCellData()
	{
		if (Camera.main == null)
			return;

		var cell = _grid[Camera.main.ScreenPointToRay(Input.mousePosition)];
		if (cell != null && cell.isExplored)
		{
			var terrainType = (TerrainType)cell.terrainTypeIndex;
			var forest = cell.plantLevel > 0 ? "Forest, " : "";
			var urban = cell.urbanLevel > 0 ? "Urban, " : "";
			var farms = cell.farmLevel > 0 ? "Farms, " : "";
			var river = cell.isHasRiver ? "River, " : "";
			var road = cell.isHasRoad ? "Road, " : "";
			var walls = cell.walled ? "Walls, " : "";
			var unit = cell.unit != null ? "\n\nUnit: " + cell.unit.name + $" {cell.unit.owner}" : "";
			
			_cellView.text = cell.isUnderwater ? "Water" :
				terrainType +
				forest + 
				urban + 
				farms +
				river + 
				road + 
				walls +
				unit;
		}
	}


	private void DoPathfinding()
	{
		if (UpdateCurrentCell())
		{
			if (_currentCell && _selectedUnit.IsValidDestination(_currentCell))
			{
				_grid.FindPath(_selectedUnit.location, _currentCell, _selectedUnit);
			}
			else
			{
				_grid.ClearPath();
			}
		}
	}


	public bool UpdateCurrentCell()
	{
		if (Camera.main == null)
			return false;
		
		var cell = _grid[Camera.main.ScreenPointToRay(Input.mousePosition)];
		if (cell == _currentCell) 
			return false;
		
		_currentCell = cell;
		return true;
	}


	private void DoSelection()
	{
		_grid.ClearPath();
		UpdateCurrentCell();
		if (_currentCell)
		{
			_selectedUnit = _currentCell.unit;
		}
	}


	private void DoMove()
	{
		if (_grid.hasPath)
		{
			_selectedUnit.Travel(_grid.GetPath());
			_grid.ClearPath();
		}
	}


	public void OccupyCell()
	{
		if (!_selectedUnit) 
			return;
		
		var cell = _selectedUnit.location;

		var hasNeighborWithSameOwner = false;
		for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
		{
			var neighbor = cell[d];
			hasNeighborWithSameOwner |= neighbor.isHasOwner && neighbor.owner == _selectedUnit.owner;
		}

		if (hasNeighborWithSameOwner)
		{
			cell.owner = _selectedUnit.owner;
			cell.shaderData.RefreshOwner(cell, _selectedUnit.owner);
		}
	}

}
