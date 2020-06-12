using System;
using System.Collections.Generic;
using UnityEngine;



public interface IEventQueue
{


	void Enqueue(GameEvent e);


}

public class GameEvent
{
	

}

public class UnitSpawnEvent : GameEvent
{
	private Barrack _this;
	private Unit _unit;

	public UnitSpawnEvent(Unit unit) => 
		_unit = unit;


	public static void Subscribe(Action<Unit> onUnitSpawned)
	{
		throw new NotImplementedException();
	}


}

public class EventQueue : IEventQueue
{

	private Queue<GameEvent> _queue;

	public void Enqueue(GameEvent e)
	{
		_queue.Enqueue(e);
	}


}




public class Barrack : Building<BarrackView>
{
	[SerializeField] private GameObject _unitsParent;
	[SerializeField] private FastUnit _originalFastUnit;
	[SerializeField] private AttackingUnit _originalAttackingUnit;
	[SerializeField] private ArmoredUnit _originalArmoredUnit;


	private Unit _currentTrainingUnit;
	private int _currentTrainingUnitCount;
	private float _trainingTime;

	private List<Unit> _units = new List<Unit>();

	private GameManager _gm => GameManager.Instance;


	public int unitsLimit { get; set; } = 100;

	private float _unitsAttack = 0.1f;
	private float _unitsArmor = 0.1f;
	private int _productsSpawnUnitPrice = 10;
	private int _creditsSpawnUnitPrice = 10;
	private Cell _spawnCell;

	public event Action<Unit> unitSpawned;



	protected override void Start()
	{
		base.Start();

		_view.spawnFastUnitButtonPressed += SpawnFastUnit;
		_view.spawnAttackingUnitButtonPressed += SpawnAttackingUnit;
		_view.spawnArmoredUnitButtonPressed += SpawnArmoredUnit;
	}


	internal void Init(Cell spawnCell)
	{
		_spawnCell = spawnCell;
	}


	protected override void Upgrade()
	{
		unitsLimit += 100;
		_unitsAttack += 0.1f;
		_unitsArmor += 0.1f;

		Debug.Log($"units limit = {unitsLimit}, units attack = {_unitsAttack}, units defence = {_unitsArmor}");
	}


	private void SpawnFastUnit()
	{
		if (IsCanTrainUnit())
		{
			_gm.population -= _currentTrainingUnitCount;
			_gm.products -= _productsSpawnUnitPrice * _currentTrainingUnitCount;
			_gm.credits -= _creditsSpawnUnitPrice * _currentTrainingUnitCount;

			_currentTrainingUnit = _originalFastUnit;

			_view._trainUnitButtonInterractive = false;
		}
	}


	private void SpawnAttackingUnit()
	{
		if (IsCanTrainUnit())
		{
			_gm.population -= _currentTrainingUnitCount;
			_gm.products -= _productsSpawnUnitPrice * _currentTrainingUnitCount;
			_gm.credits -= _creditsSpawnUnitPrice * _currentTrainingUnitCount;

			_currentTrainingUnit = _originalAttackingUnit;
			_view._trainUnitButtonInterractive = false;
		}
	}


	private void SpawnArmoredUnit()
	{
		if (IsCanTrainUnit())
		{
			_gm.population -= _currentTrainingUnitCount;
			_gm.products -= _productsSpawnUnitPrice * _currentTrainingUnitCount;
			_gm.credits -= _creditsSpawnUnitPrice * _currentTrainingUnitCount;

			_currentTrainingUnit = _originalArmoredUnit;
			_view._trainUnitButtonInterractive = false;
		}
	}


	private bool IsCanTrainUnit()
	{
		return
			_currentTrainingUnit == null &&
			_spawnCell.unit == null &&
			_currentTrainingUnitCount > 0 &&
			_gm.products >= _productsSpawnUnitPrice * _currentTrainingUnitCount &&
			_gm.credits >= _creditsSpawnUnitPrice * _currentTrainingUnitCount;
	}


	private void SpawnSquad(Unit unitOriginal, int unitCount)
	{
		var squad = Instantiate(unitOriginal, _spawnCell.transform.position, Quaternion.identity, _unitsParent.transform);
		squad.count = unitCount;
		squad.Deselect();
		_units.Add(squad);

		//_gm.queue.Enqueue(e: new UnitSpawnEvent(this, squad));

		unitSpawned?.Invoke(squad);
	}


	protected override void Update()
	{
		base.Update();

		if (_currentTrainingUnit != null)
		{
			_trainingTime += Time.deltaTime;
			_view._trainUnitProgressValue = _trainingTime / GameManager._GS;
			if (_trainingTime >= GameManager._GS)
			{
				_view._trainUnitButtonInterractive = true;
				SpawnSquad(_currentTrainingUnit, _currentTrainingUnitCount);
				_currentTrainingUnit = null;
				_currentTrainingUnitCount = 0;
				_trainingTime = 0.0f;
			}
		}
		else
		{
			if (int.TryParse(_view.spawnUnitsCount, out int uninCount))
			{
				_currentTrainingUnitCount = uninCount;
			}
			else
			{
				_currentTrainingUnitCount = 0;
			}
		}

	}


}
