using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public static class HexUnitFactory
{


	private static readonly Dictionary<string, HexUnitData> _unitOrigins = new Dictionary<string, HexUnitData>();



	public static void LoadAssets()
	{
		Addressables.LoadAssetsAsync<HexUnitData>("Units", HandleUnitAssetLoaded);
	}


	private static void HandleUnitAssetLoaded(HexUnitData unitData)
	{
		_unitOrigins.Add(unitData.name, unitData);
	}


	public static HexUnit Spawn(string key, int owner)
	{
		var unit = Object.Instantiate(_unitOrigins[key].prefab);
		unit.Init(_unitOrigins[key], owner);

		return unit;
	}


}
