using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _level;
    [SerializeField] private TextMeshProUGUI _lvlUpProductsPrice;
    [SerializeField] private TextMeshProUGUI _lvlUpCreditsPrice;
    [SerializeField] private Button _lvlUpButton;
    [SerializeField] private Image _lvlUpButtonFill;

    public event Action levelUpButtonPressed;

    public void LevelUpButtonPressed()
    {
        levelUpButtonPressed?.Invoke();
    }

    public int level
    {
        set
        {
            _level.text = $"Level {value}";
        }
    }

    public int leveUpProductPrice
    {
        set
        {
            _lvlUpProductsPrice.text = value.ToString();
        }
    }

    public int leveUpCreditPrice
    {
        set
        {
            _lvlUpCreditsPrice.text = value.ToString();
        }
    }


    public bool _lvlUpButtonInterractive
    {
        set
        {
            _lvlUpButton.interactable = value;
            _lvlUpButtonFill.enabled = !value;
        }
    }

    public float _lvlUpProgressValue
    {
        set
        {
            _lvlUpButtonFill.fillAmount = 1.0f - value;
        }
    }
}
