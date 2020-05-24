using System;
using System.Collections.Generic;
using UnityEngine;



public class Map : MonoBehaviour
{

	private Cell[,] cells;

	private const string _mapLayerName = "Map";

	[SerializeField] private Cell _originalCell;

	[SerializeField] private Vector2Int _size;
	[SerializeField] private Vector2Int _startCell;


	public Cell selectedCell { get; private set; }
	public Cell startCell => cells[_startCell.x, _startCell.y];
	public Vector2Int size => _size;

	public event Action<Cell> cellSelected;
	public event Action<Cell> cellClicked;




	void Start()
	{
		MapGenerator(_size.x, _size.y);
		BuyCell(cells[_startCell.x, _startCell.y]);
		Debug.Log($"Map created");
	}


	public void MapGenerator(int n, int m)
	{
		cells = new Cell[n, m];
		for (int i = 0; i < n; i++)
		{
			for (int j = 0; j < m; j++)
			{
				cells[i, j] = Instantiate(_originalCell, new Vector3(i, 0, j), Quaternion.identity);
				cells[i, j].Init(this, i, j);
				cells[i, j].gameObject.layer = LayerMask.NameToLayer(_mapLayerName);
				cells[i, j].gameObject.hideFlags = HideFlags.HideInHierarchy;
			}
		}

		
	}


	public Cell[,] GetCells()
	{
		return cells;
	}

	public void BuyCell(Cell cell)
	{
		cell.Buy();
	}


	public bool IsCellCanBeBought(Cell cell)
	{
		return cell != null && !cell.isBought && IsHasBoughtNeighbors(cell);
	}


	private bool IsHasBoughtNeighbors(Cell cell)
	{
		return
			(cell.up != null ? cell.up.isBought : false) ||
			(cell.down != null ? cell.down.isBought : false) ||
			(cell.left != null ? cell.left.isBought : false) ||
			(cell.right != null ? cell.right.isBought : false);
	}


	public void SelectCellWithLeftButton(Cell cell)
	{
		selectedCell?.Unselect();
		selectedCell = cell;
		selectedCell.Select();

		cellSelected?.Invoke(cell);
	}


	private void Update()
	{
		var leftClick = Input.GetMouseButtonUp(0);
		var rightClick = Input.GetMouseButtonUp(1);
		if (leftClick || rightClick)
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out var hitInfo, 100, ~LayerMask.NameToLayer(_mapLayerName)))
			{
				var cell = hitInfo.collider.gameObject.GetComponent<Cell>();
				if (cell != null)
				{
					if (leftClick)
						SelectCellWithLeftButton(cell);
					if (rightClick)
						SelectCellWithRightButton(cell);
				}
			}
		}
	}

	private void SelectCellWithRightButton(Cell cell)
	{
		cellClicked?.Invoke(cell);
	}
}
