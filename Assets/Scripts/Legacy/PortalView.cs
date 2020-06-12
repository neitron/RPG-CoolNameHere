using System;
using TMPro;
using UnityEngine;

public class PortalView : BuildingView
{
	[SerializeField] private TextMeshProUGUI _goodsForSellCout;

	public string goodsForSellCount =>
		_goodsForSellCout.text.Substring(0, _goodsForSellCout.textInfo.characterCount - 1);
	
	public event Action buyButtonPressed;
	public event Action sellButtonPressed;


	public void BuyButtonPressed()
	{
		buyButtonPressed?.Invoke();
	}


	public void SellButtonPressed()
	{
		sellButtonPressed?.Invoke();
	}


}
