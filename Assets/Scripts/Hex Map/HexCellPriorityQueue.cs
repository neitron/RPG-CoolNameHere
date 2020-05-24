using System.Collections.Generic;




public class HexCellPriorityQueue
{


	private readonly List<HexCell> _list = new List<HexCell>();
	private int _minimum = int.MaxValue;


	public int count { private set; get; }



	public void Enqueue(HexCell cell)
	{
		count++;
		var priority = cell.searchPriority;

		if (priority < _minimum)
		{
			_minimum = priority;
		}

		while (priority >= _list.Count)
		{
			_list.Add(null);
		}

		cell.nextWithSamePriority = _list[priority];
		_list[priority] = cell;
	}


	public HexCell Dequeue()
	{
		count--;

		for (; _minimum < _list.Count; _minimum++)
		{
			var cell = _list[_minimum];
			if (cell != null)
			{
				_list[_minimum] = cell.nextWithSamePriority;
				return cell;
			}
		}
		return null;
	}


	public void Change(HexCell cell, int oldPriority)
	{
		var current = _list[oldPriority];
		var next = current.nextWithSamePriority;

		if (current == cell)
		{
			_list[oldPriority] = next;
		}
		else
		{
			while (next != cell)
			{
				current = next;
				next = current.nextWithSamePriority;
			}

			current.nextWithSamePriority = cell.nextWithSamePriority;
			Enqueue(cell);
			count--;
		}
	}


	public void Clear()
	{
		_list.Clear();
		count = 0;
		_minimum = int.MaxValue;
	}


}
