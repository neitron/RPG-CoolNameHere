using UnityEngine;

public class House : Building<HousesView>
{

	

	protected override void Upgrade()
	{
		GameManager gm = GameManager.Instance;
		gm.populationLimit += 200;
		gm.populationPlus += Mathf.RoundToInt(gm.populationPlus * 0.05f);
		Debug.Log($"Population limit = {gm.populationLimit}, Population plus = {gm.populationPlus}");
	}

}
