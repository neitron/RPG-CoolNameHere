

using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;



public class Player : IPlayer
{


	public int id { get; }

	private static HexCell _currentCell;
	private static HexUnit _selectedUnit;

	private readonly HexGrid _grid;
	private readonly List<HexUnit> _units;
	private readonly CompositeDisposable _disposables = new CompositeDisposable();



	public Player(int id, HexGrid grid)
	{
		this.id = id;
		_grid = grid;

		_units = new List<HexUnit>();
	}



	public void Tick()
	{
		if (EventSystem.current.IsPointerOverGameObject()) return;

		if (Input.GetMouseButtonDown(0))
		{
			DoSelection();
		}
		else if (_selectedUnit && _selectedUnit.playerId == id)
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


	public void AddUnit(HexUnit hexUnit)
	{
		_units.Add(hexUnit);
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


	private void OccupyCell()
	{
		if (!_selectedUnit || _selectedUnit.playerId != id)
			return;

		var cell = _selectedUnit.location;

		var hasNeighborWithSameOwner = false;
		for (var d = HexDirection.Ne; d <= HexDirection.Nw; d++)
		{
			var neighbor = cell[d];
			hasNeighborWithSameOwner |= neighbor.isHasOwner && neighbor.owner == _selectedUnit.playerId;
		}

		if (hasNeighborWithSameOwner)
		{
			cell.owner = _selectedUnit.playerId;
			cell.shaderData.RefreshOwner(cell, _selectedUnit.playerId);
		}
	}

	public void StartTurn()
	{
		var gameView = Object.FindObjectOfType<HexGameUi>();
		gameView.cellOccupyButtonPressed.Do(_ => OccupyCell()).Subscribe().AddTo(_disposables);
	}


	public void EndTurn()
	{
		for (var i = 0; i < _units.Count; i++)
		{
			_units[i].ResetTravelBudget();
		}

		_disposables.Clear();
	}


}