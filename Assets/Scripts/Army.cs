using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Army : MonoBehaviour
{
    [SerializeField] private Map _map;

    private Unit _selected;
    private Cell _selectedUnitCell;

    private A_StarPathFinder _pathFinder;



    void Start()
    {
        _map.cellSelected += SelectUnitSquad;
        _map.cellClicked += TryToMoveSelectedUnit;

        _pathFinder = new A_StarPathFinder(_map.GetCells(), _map.size.x, _map.size.y);
    }


    private void TryToMoveSelectedUnit(Cell cell)
    {
        if (_selected == null)
            return;

        MoveSelectedTo(cell);
    }


    private void SelectUnitSquad(Cell cell)
    {
        _selected?.Deselect();
        _selected = cell.unit;
        _selectedUnitCell = cell;
        _selected?.Select();
    }


    internal void MoveSelectedTo(Cell target)
    {
        if (target.unit != null)
        {
            return;
        }

        var path = _pathFinder.FindPath(_selectedUnitCell, target);

        _selected.Traverse(path);

        _selectedUnitCell.unit = null;
        _selectedUnitCell = target;
        _selectedUnitCell.unit = _selected;
    }



}
