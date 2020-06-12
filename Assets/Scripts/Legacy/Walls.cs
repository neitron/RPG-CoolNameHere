using UnityEngine;

public class Walls : Building<WallsView>
{

	private float _wallDefence = 0.05f;


	protected override void Upgrade()
	{
		_wallDefence += 0.05f;
		Debug.Log($"walls defence = {_wallDefence}");
	}


	protected override void Downgrade(int level)
	{
		_wallDefence -= _wallDefence * level;
		
		Debug.Log($"walls defence = {_wallDefence}");
	}

}
