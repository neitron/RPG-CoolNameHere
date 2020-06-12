using UnityEngine;

public class Portal : Building<PortalView>
{

	private float _deltaSell = 0.5f;
	private float _deltaBuy = 1.5f;

	private GameManager _gm => GameManager.Instance;



	protected override void Start()
	{
		base.Start();

		_view.buyButtonPressed += BuyGoods;
		_view.sellButtonPressed += SellGoods;
	}


	protected override void Upgrade()
	{
		_deltaSell += 0.5f;
		_deltaBuy -= 0.5f;
		_gm.productsPlus += _gm.productsPlus * 0.0025f;
		_gm.creditPlus += 0.0025f;
		Debug.Log($"delta sell = {_deltaSell}, delta buy = {_deltaBuy}, product plus = {_gm.productsPlus}, credit plus = {_gm.creditPlus}");
	}


	private void SellGoods()
	{
		Debug.Log("Sell");
		if (int.TryParse(_view.goodsForSellCount, out int productsCount) &&
			_gm.products >= productsCount)
		{
			Debug.Log($"Sell {productsCount} products and recived {productsCount * _deltaSell} credist");
			_gm.products -= productsCount;
			_gm.credits += productsCount * _deltaSell;
		}

	}


	private void BuyGoods()
	{
		Debug.Log("Buy");
		if (int.TryParse(_view.goodsForSellCount, out int productsCount) &&
			_gm.credits >= productsCount * _deltaBuy)
		{
			Debug.Log($"Buy {productsCount} products and paid {productsCount * _deltaBuy} credits");
			_gm.products += productsCount;
			_gm.credits -= productsCount * _deltaBuy;
		}
	}

}
