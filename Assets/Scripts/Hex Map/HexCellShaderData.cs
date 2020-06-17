
using System.Collections.Generic;
using UnityEngine;



public class HexCellShaderData : MonoBehaviour
{


	private const float TRANSITION_SPEED = 255f;

	private static readonly int _hexCellData = Shader.PropertyToID("_HexCellData");
	private static readonly int _hexCellDataTexelSize = Shader.PropertyToID("_HexCellData_TexelSize");


	public bool immediateMode { get; set; }
	public HexGrid hexGrid { get; set; }


	private Texture2D _cellTexture;
	/// <summary>
	/// R - Visibility data chanel
	/// G - Exploration data chanel
	/// B - Debug data chanel (use for any debug purpose)
	/// A - Terrain type data chanel (refers to the terrain texture array)
	/// </summary>
	private Color32[] _cellTextureData;
	private readonly List<HexCell> _transitioningCells = new List<HexCell>();
	private bool _needsVisibilityReset;

	

	public void Initialize(int x, int z)
	{
		if (_cellTexture)
		{
			_cellTexture.Resize(x, z);
		}
		else
		{
			_cellTexture = new Texture2D(x, z, TextureFormat.RGBA32, false, true)
			{
				filterMode = FilterMode.Point, 
				wrapModeU = TextureWrapMode.Repeat, 
				wrapModeV = TextureWrapMode.Clamp
			};

			Shader.SetGlobalTexture(_hexCellData, _cellTexture);
		}

		Shader.SetGlobalVector(
			_hexCellDataTexelSize,
			new Vector4(1f / x, 1f / z, x, z)
		);

		if (_cellTextureData == null || _cellTextureData.Length != x * z)
		{
			_cellTextureData = new Color32[x * z];
		}
		else
		{
			for (var i = 0; i < _cellTextureData.Length; i++)
			{
				_cellTextureData[i] = new Color32(0, 0, 0, 0);
			}
		}

		_transitioningCells.Clear();
		enabled = true;
	}


	public void RefreshTerrain(HexCell cell)
	{
		_cellTextureData[cell.index].a = (byte) cell.terrainTypeIndex;
		enabled = true;
	}


	private void LateUpdate()
	{
		if (_needsVisibilityReset)
		{
			_needsVisibilityReset = false;
			hexGrid.ResetVisibility();
		}

		var delta = (int) (Time.deltaTime * TRANSITION_SPEED);
		if (delta == 0)
		{
			delta = 1;
		}

		for (var i = 0; i < _transitioningCells.Count; i++)
		{
			if (UpdateCellData(_transitioningCells[i], delta)) 
				continue;
			
			_transitioningCells[i--] = _transitioningCells[_transitioningCells.Count - 1];
			_transitioningCells.RemoveAt(_transitioningCells.Count - 1);
		}

		_cellTexture.SetPixels32(_cellTextureData);
		_cellTexture.Apply();
		enabled = _transitioningCells.Count > 0;
	}


	private bool UpdateCellData(HexCell cell, int delta)
	{
		var index = cell.index;
		var data = _cellTextureData[index];
		var stillUpdating = false;
		//if (cell.isExplored && data.g < 255)
		//{
		//	stillUpdating = true;
		//	var t = data.g + delta;
		//	data.g = t >= 255 ? (byte) 255 : (byte) t;
		//}

		var exploration = (byte)(data.g >> 4);
		var owner = (byte)(data.g & 15);
		if (cell.isExplored && exploration < 15 )
		{
			stillUpdating = true;
			var t = exploration + delta;
			exploration = t >= 15 ? (byte) 15 : (byte) t;
		}
		data.g = (byte)((exploration << 4) + owner);

		if (cell.isVisible)
		{
			if (data.r < 255)
			{
				stillUpdating = true;
				var t = data.r + delta;
				data.r = t >= 255 ? (byte)255 : (byte)t;
			}
		}
		else if (data.r > 0)
		{
			stillUpdating = true;
			var t = data.r - delta;
			data.r = t < 0 ? (byte)0 : (byte)t;
		}

		if (!stillUpdating)
		{
			data.b = 0;
		}

		_cellTextureData[index] = data;
		return stillUpdating;
	}


	public void SetMapData(HexCell cell, float data)
	{
		_cellTextureData[cell.index].b = data < 0f ? (byte)0 : (data < 1f ? (byte)(data * 254f) : (byte)254);
		enabled = true;
	}


	public void RefreshVisibility(HexCell cell)
	{
		var index = cell.index;

		if (immediateMode)
		{
			_cellTextureData[index].r = cell.isVisible ? (byte) 255 : (byte) 0;
			_cellTextureData[index].g = cell.isExplored ? (byte) 255 : (byte) 0;
		}
		else if (_cellTextureData[index].b != 255)
		{
			_cellTextureData[index].b = 255;
			_transitioningCells.Add(cell);
		}
		enabled = true;
	}


	public void RefreshOwner(HexCell cell, int owner)
	{
		var exploration = (byte) (_cellTextureData[cell.index].g >> 4);
		_cellTextureData[cell.index].g = (byte)((exploration << 4) + owner);

		enabled = true;
	}


	public void ViewElevationChanged()
	{
		_needsVisibilityReset = true;
		enabled = true;
	}


}
