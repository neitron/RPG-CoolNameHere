using JetBrains.Annotations;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;


public class HexGameUi : MonoBehaviour
{


	[SerializeField] private HexGrid _grid;
	[SerializeField] private Canvas _canvas;
	[SerializeField] private TextMeshProUGUI _cellView;


	public Subject<Unit> cellOccupyButtonPressed = new Subject<Unit>();
	public Subject<Unit> endTurnButtonPressed = new Subject<Unit>();



	[UsedImplicitly]
	public void SetEditMode(bool toggle)
	{
		_canvas.enabled = !toggle;
		enabled = !toggle;
		_grid.ShowUi(!toggle);
		_grid.ClearPath();

		if (toggle)
		{
			Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
		}
		else
		{
			Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
		}
	}


	void Update()
	{
		ShowCurrentCellData();
	}


	private void ShowCurrentCellData()
	{
		if (Camera.main == null)
			return;

		var cell = _grid[Camera.main.ScreenPointToRay(Input.mousePosition)];
		if (cell != null && cell.isExplored)
		{
			var terrainType = (TerrainType)cell.terrainTypeIndex + ", ";
			var forest = cell.plantLevel > 0 ? "Forest, " : "";
			var urban = cell.urbanLevel > 0 ? "Urban, " : "";
			var farms = cell.farmLevel > 0 ? "Farms, " : "";
			var river = cell.isHasRiver ? "River, " : "";
			var road = cell.isHasRoad ? "Road, " : "";
			var walls = cell.walled ? "Walls, " : "";
			var unit = cell.unit != null ? "\nUnit: " + cell.unit.name + $" {cell.unit.playerId} " : "";
			var cellOwner = cell.isHasOwner ? $"\nOwner : Player-{cell.owner}, " : $"No Owner, ";
			_cellView.text = cell.isUnderwater ? "Water" :
				terrainType +
				forest + 
				urban + 
				farms +
				river + 
				road + 
				walls +
				unit +
				cellOwner;
		}
	}


	[UsedImplicitly]
	public void OccupyCellExternal()
	{
		cellOccupyButtonPressed.OnNext(new Unit());
	}


	[UsedImplicitly]
	public void EndTurnExternal()
	{
		endTurnButtonPressed.OnNext(new Unit());
	}

}
