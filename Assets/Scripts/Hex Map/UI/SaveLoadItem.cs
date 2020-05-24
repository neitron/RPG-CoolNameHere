using TMPro;
using UnityEngine;



public class SaveLoadItem : MonoBehaviour
{


	[SerializeField] private SaveLoadMenu _menu;
	private string _mapName;



	public string mapName
	{
		get => _mapName;
		private set
		{
			_mapName = value;
			transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = value;
		}
	}


	public void Select()
	{
		_menu.SelectItem(_mapName);
	}


	public void Init(SaveLoadMenu saveLoadMenu, string fileName)
	{
		_menu = saveLoadMenu;
		mapName = fileName;
	}


}
