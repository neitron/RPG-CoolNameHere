using System;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
	[SerializeField] private GameObject _selectionSprite;

	public int count { get; set; }
	public int speed { get; protected set; }
	public int attack { get; protected set; }
	public int armor { get; protected set; }


	private List<Cell> _path;
	private Cell _target;
	private int _targetIndex;


	public void Select()
	{
		_selectionSprite.SetActive(true);
	}


	public void Deselect()
	{
		_selectionSprite.SetActive(false);
	}


	public void Traverse(List<Cell> path)
	{
		_path = path;

		_targetIndex = path.Count - 1;
		_target = path[_targetIndex];
	}

	private void Update()
	{
		if (_target != null)
		{
			if (Vector3.Distance(_target.transform.position, transform.position) < 0.01f)
			{
				transform.position = _target.transform.position;
				if (--_targetIndex >= 0)
					_target = _path[_targetIndex];
			}

			Vector3 dir = (_target.transform.position - transform.position).normalized;
			transform.position += ((dir * speed) / GameManager._GS) * Time.deltaTime;
		}
	}
}
