using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;



public class SliverWithValue : Slider
{

    
	[SerializeField] private TextMeshProUGUI _valueView;



    protected override void OnValidate()
    {
	    base.OnValidate();

	    _valueView = GetComponentInChildren<TextMeshProUGUI>();

	    onValueChanged.RemoveListener(HandleOnValueChanged);
	    onValueChanged.AddListener(HandleOnValueChanged);
    }


    private void HandleOnValueChanged(float newValue)
    {
	    _valueView.text = newValue.ToString(CultureInfo.InvariantCulture);
    }
    

}
