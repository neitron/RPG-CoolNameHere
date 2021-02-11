// https://catlikecoding.com/unity/tutorials/hex-map/part-2/cell-neighbors/directions.png
public enum HexDirection
{
	/// <summary>
	/// Top - Right
	/// </summary>
	Ne,
	/// <summary>
	/// Right
	/// </summary>
	E,
	/// <summary>
	/// Bottom - Right
	/// </summary>
	Se,
	/// <summary>
	/// Bottom - Left
	/// </summary>
	Sw,
	/// <summary>
	/// Left
	/// </summary>
	W,
	/// <summary>
	/// Top - Left
	/// </summary>
	Nw

}



public static class HexDirectionExtension
{


	public static HexDirection Opposite(this HexDirection direction) =>
		(int) direction < 3 ? (direction + 3) : (direction - 3);
	

	public static HexDirection Previous(this HexDirection direction) => 
		direction == HexDirection.Ne ? HexDirection.Nw : (direction - 1);


	public static HexDirection Previous2(this HexDirection direction)
	{
		direction -= 2;
		return direction >= HexDirection.Ne ? direction : (direction + 6);
	}


	public static HexDirection Next(this HexDirection direction) => 
		direction == HexDirection.Nw ? HexDirection.Ne : (direction + 1);


	public static HexDirection Next2(this HexDirection direction)
	{
		direction += 2;
		return direction <= HexDirection.Nw ? direction : (direction - 6);
	}


}