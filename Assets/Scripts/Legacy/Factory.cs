using UnityEngine;

public class Factory : Building<FactoryView>
{

	protected override void Upgrade()
	{
		GameManager gm = GameManager.Instance;
		gm.productsPlus += gm.productsPlus * 0.0175f;
		Debug.Log($"product plus = {gm.productsPlus}");
	}

}
