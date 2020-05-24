using UnityEngine;



public class HexCellShaderData : MonoBehaviour
{


	private static readonly int _hexCellData = Shader.PropertyToID("_HexCellData");
	private static readonly int _hexCellDataTexelSize = Shader.PropertyToID("_HexCellData_TexelSize");

	private Texture2D _cellTexture;
	private Color32[] _cellTextureData;



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
				wrapMode = TextureWrapMode.Clamp
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
		enabled = true;
	}


	public void RefreshTerrain(HexCell cell)
	{
		_cellTextureData[cell.index].a = (byte) cell.terrainTypeIndex;
		enabled = true;
	}


	private void LateUpdate()
	{
		_cellTexture.SetPixels32(_cellTextureData);
		_cellTexture.Apply();
		enabled = false;
	}


	public void SetMapData(HexCell cell, float data)
	{
		_cellTextureData[cell.index].b =
			data < 0f ? (byte)0 : (data < 1f ? (byte)(data * 254f) : (byte)254);
		enabled = true;
	}


}
