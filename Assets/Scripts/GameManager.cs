
using System;
using UniRx;
using UnityEngine;



public class GameManager : MonoSingleton<GameManager>
{


	private int _currentPlayer;
	private IPlayer[] _players;
	private IPlayer _currentTurnPlayer;
	private IDisposable _currentPlayerUpdate;
	public static IPlayer currentPlayer => instance._currentTurnPlayer;
	private HexGrid _grid;



	public static void NewGame(int playerCount, HexGrid hexGrid)
	{
		instance.NewGameInternal(playerCount, hexGrid);
	}


	private void NewGameInternal(int playerCount, HexGrid hexGrid)
	{
		_grid = hexGrid;

		FindObjectOfType<HexGameUi>().endTurnButtonPressed
			.Do(HandlePLayerTurnFinished)
			.Subscribe();

		CreatePlayers(playerCount, hexGrid);
		SetCurrentPLayer(_currentPlayer);

		SpawnSettlers(hexGrid);
	}


	private void SetCurrentPLayer(int playerId)
	{
		Debug.Log($"CURRENT PLAYER ID : {playerId}");

		var player = _players[playerId];
		
		_currentTurnPlayer?.EndTurn();
		_currentPlayerUpdate?.Dispose();
		_currentTurnPlayer = player;
		_currentTurnPlayer.StartTurn();
		_currentPlayerUpdate = Observable.EveryUpdate()
			.Do(_ => _currentTurnPlayer.Tick())
			.Subscribe();
	}


	private void HandlePLayerTurnFinished(Unit obj)
	{
		_currentPlayer = (_currentPlayer + 1) % 2;
		SetCurrentPLayer(_currentPlayer);
		_grid.RefreshVisibility();
	}


	private void CreatePlayers(int playerCount, HexGrid hexGrid)
	{
		_players = new IPlayer[playerCount];
		for (var i = 0; i < playerCount; i++)
		{
			_players[i] = new Player(i, hexGrid);
		}

		hexGrid.BindPlayersData(playerCount);
	}


	private void SpawnSettlers(HexGrid grid)
	{
		var units = new HexUnit[_players.Length];
		var playerCounter = 0;
		for (var i = 0; i < grid.cellTotalCount && playerCounter < units.Length; i++)
		{
			var cell = grid[i];

			if (cell.isUnderwater || !cell.isExplorable)
				continue;

			var isFarEnoughFromOthers = true;
			for (var j = 0; j < playerCounter && isFarEnoughFromOthers; j++)
			{
				var unit = units[j];
				if (unit == null)
					break;

				isFarEnoughFromOthers &= cell.coordinates.DistanceTo(unit.location.coordinates) >= 10;
			}

			if (!isFarEnoughFromOthers)
			{
				continue;
			}

			var newUnit = HexUnitFactory.Spawn("Settler", _players[playerCounter].id);
			_players[playerCounter].AddUnit(newUnit);
			grid.AddUnit(newUnit, cell, UnityEngine.Random.Range(0f, 360f));
			units[playerCounter] = newUnit;
			playerCounter++;
		}

	}


}