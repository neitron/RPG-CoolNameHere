using System.Collections.Generic;
using UnityEngine;



public class Cell : MonoBehaviour
{
	private MeshRenderer _mr;
	private bool _isSelected;

	public bool isBought { get; private set; } = false;
	public bool isWalkable { get; private set; } = true;
	public Vector2Int coords { get; private set; }
	public Unit unit { get; set; }

	public readonly List<Cell> neighbours = new List<Cell>();
	public int gCost;
	public int hCost;
	public Cell parent;

	public int fCost => gCost + hCost;

	public Cell up;
	public Cell down;
	public Cell left;
	public Cell right;

	internal void Init(Map map, int i, int j)
	{
		_mr = GetComponent<MeshRenderer>();
		coords = new Vector2Int(i, j);
	}


	internal void Unselect()
	{
		_isSelected = false;

		if (!isBought)
			_mr.material.color = Color.white;
	}

	internal void Select()
	{
		_isSelected = true;

		if (!isBought)
			_mr.material.color = Color.green;
	}


	internal void Buy()
	{
		isBought = true;

		_mr.material.color = Color.blue;
	}



}
