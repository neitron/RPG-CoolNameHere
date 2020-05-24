using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{

	public static GameManager Instance;

	[SerializeField] private TextMeshProUGUI _populationView;
	[SerializeField] private TextMeshProUGUI _productsView;
	[SerializeField] private TextMeshProUGUI _creditsView;

	private float _gs = 0;
	public const float _GS = 4;

	public int countGs { get; private set; } = -1;
	public int population { get; set; } = 200;
	public int products { get; set; } = 300;
	public float credits { get; set; } = 500;
	public int populationLimit { get; set; } = 1000;
	public int populationPlus { get; set; } = 100;
	public float productsPlus { get; set; } = 100;
	public float creditPlus { get; set; } = 0.1f;

	public IEventQueue queue;


	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Debug.LogError("Singleton already initialized!", Instance);
		}
	}


	void Start()
	{
		Debug.Log("GameStart");
	}


	public void OnValCh(string value)
	{
		Debug.Log(value);
	}


	void Update()
	{
		if (_gs >= _GS)
		{
			if (population + populationPlus <= populationLimit)
				population += populationPlus;
			else
				population += populationLimit - population;
			products += Mathf.RoundToInt(productsPlus);
			credits += population * creditPlus;
			_gs = 0.0f;
			countGs++;
		}

		_populationView.text = $"{population}";
		_productsView.text = $"{products}";
		_creditsView.text = $"{credits}";

		_gs += Time.deltaTime;

	}

}
