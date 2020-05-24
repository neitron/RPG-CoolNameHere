using System;
using UnityEngine;


[Serializable]
public struct HexCoordinates
{
	
	[SerializeField]
	private int _x;
	[SerializeField]
	private int _z;


	public int x => _x;
	public int z => _z;
	public int y => -x - z;



	public HexCoordinates(int x, int z) =>
		(_x, _z) = (x, z);


	public static HexCoordinates FromOffsetCoordinates(int x, int z) =>
		new HexCoordinates(x - z / 2, z);


	public override string ToString() =>
		$"({x}, {y}, {z})";


	public string ToOnStringSeparateLines() =>
		$"{x}\n{y}\n{z}";


	public static implicit operator Vector3Int(HexCoordinates c) =>
		new Vector3Int(c.x, c.y, c.z);


	public static HexCoordinates FromPosition(Vector3 position)
	{
		var x = position.x / (HexMetrics.INNER_RADIUS * 2.0f);
		var y = -x;

		var offset = position.z / (HexMetrics.OUTER_RADIUS * 3.0f);
		x -= offset;
		y -= offset;

		var ix = Mathf.RoundToInt(x);
		var iy = Mathf.RoundToInt(y);
		var iz = Mathf.RoundToInt(-x - y);

		if (ix + iy + iz != 0)
		{
			var dx = Mathf.Abs(x - ix);
			var dy = Mathf.Abs(y - iy);
			var dz = Mathf.Abs(-x - y - iz);

			if (dx > dy && dx > dz)
			{
				ix = -iy - iz;
			}
			else if (dz > dy)
			{
				iz = -ix - iy;
			}
		}

		return new HexCoordinates(ix, iz);
	}



	public int DistanceTo(HexCoordinates other)
	{
		return 
			((x < other.x ? other.x - x : x - other.x) + 
			(y < other.y ? other.y - y : y - other.y) + 
			(z < other.z ? other.z - z : z - other.z)) / 2;
	}


}