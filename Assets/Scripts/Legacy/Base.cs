using UnityEngine;



public class Base : MonoBehaviour
{


	[SerializeField] private BaseView _view;

	[SerializeField] private Factory _originalFactory;
	[SerializeField] private House _originalHouse;

	[SerializeField] private Barrack _barak;
	[SerializeField] private Walls _walls;

	[SerializeField] private Map _map;


	private const int _expansionPopulationPrice = 500;
	private const int _expansionProductsPrice = 1000;
	private const int _expansionCreditsPrice = 1000;

	private GameManager gm => GameManager.Instance;


	private void Start()
	{
		_map.cellSelected += OnCellSelect;

		_barak.unitSpawned += OnUnitSpawned;

		UnitSpawnEvent.Subscribe(OnUnitSpawned);

		_barak.Init(_map.startCell);

		_view.buyCellButtonPressed += TryBuyCell;
		_view.ShowBuyCellButton(false, Vector3.zero);
	}


	private void OnUnitSpawned(Unit unit)
	{
		_map.startCell.unit = unit;
	}


	private void OnCellSelect(Cell cell)
	{
		var isCellCanBeBought = _map.IsCellCanBeBought(cell);
		var cellPosition = isCellCanBeBought ? cell.transform.position : Vector3.zero;
		_view.ShowBuyCellButton(isCellCanBeBought, cellPosition);
	}


	private void TryBuyCell()
	{
		if (gm.products >= _expansionProductsPrice &&
			gm.credits >= _expansionCreditsPrice &&
			gm.population >= _expansionPopulationPrice)
		{
			_map.BuyCell(_map.selectedCell);
			
			gm.population -= _expansionPopulationPrice;
			gm.products -= _expansionProductsPrice;
			gm.credits -= _expansionCreditsPrice;

			ExpandBase();

			_walls.LevelDown(5);

			_view.ShowBuyCellButton(false, Vector3.zero);

			Debug.Log($"Base expansion, Houses and Factory added. Popelation limit = {gm.populationLimit}, units limit = {_barak.unitsLimit}");
		}
	}


	private void ExpandBase()
	{
		Building<BuildingView>.Spawn(_originalHouse, this.transform, _view.transform);
		Building<BuildingView>.Spawn(_originalFactory, this.transform, _view.transform);

		//OnBaseExpaned?.Invoke();

		gm.populationLimit += 1000;
		_barak.unitsLimit += 200;
	}


	
}
