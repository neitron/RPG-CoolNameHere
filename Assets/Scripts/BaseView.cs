using System;
using UnityEngine;



public class BaseView : MonoBehaviour
{
	[SerializeField] private RectTransform _buyCellButton;

	public event Action buyCellButtonPressed;

	private Vector3 _buyCellButtonWorldPos;



	public void BuyCellButtonPressed()
	{
		buyCellButtonPressed?.Invoke();
	}


	public void ShowBuyCellButton(bool isShow, Vector3 position)
	{
		_buyCellButton.gameObject.SetActive(isShow);
		_buyCellButtonWorldPos = position;
		
	}


	private void Update()
	{
		if(_buyCellButton.gameObject.activeInHierarchy)
			RefreshBuyButtonPos(_buyCellButtonWorldPos);
	}


	private void RefreshBuyButtonPos(Vector3 position)
	{
		Vector2 ViewportPosition = Camera.main.WorldToViewportPoint(position);
		var canvas = (RectTransform)_buyCellButton.parent;
		Vector2 WorldObject_ScreenPosition = new Vector2(
		((ViewportPosition.x * canvas.sizeDelta.x) - (canvas.sizeDelta.x * 0.5f)),
		((ViewportPosition.y * canvas.sizeDelta.y) - (canvas.sizeDelta.y * 0.5f)));

		_buyCellButton.anchoredPosition = WorldObject_ScreenPosition;
	}


}
