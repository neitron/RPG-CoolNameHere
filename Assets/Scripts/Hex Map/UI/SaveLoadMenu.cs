using System;
using System.IO;
using TMPro;
using UnityEngine;



public class SaveLoadMenu : MonoBehaviour
{


	private const int MAP_FILE_VERSION = 2;

	[SerializeField] private TextMeshProUGUI _actionButtonLabel;
	[SerializeField] private TextMeshProUGUI _menuLabel;
	[SerializeField] private TMP_InputField _nameInput;
	[SerializeField] private RectTransform _listContent;
	[SerializeField] private SaveLoadItem _itemPrefab;
	[SerializeField] private HexGrid _hexGrid;

	private bool _isSaveMode;
	private Canvas _canvas;



	private void Awake()
	{
		_canvas = GetComponent<Canvas>();
	}


	public void Open(bool isSaveMode)
	{
		_isSaveMode = isSaveMode;
		_menuLabel.text = _isSaveMode ? "Save Map" : "Load Map";
		_actionButtonLabel.text = _isSaveMode ? "Save" : "Load";

		FillList();
		_canvas.enabled = true;
		HexMapCamera.isLocked = true;
	}


	public void Close()
	{
		_canvas.enabled = false;
		HexMapCamera.isLocked = false;
	}


	public string GetSelectedPath()
	{
		var mapName = _nameInput.text;
		return string.IsNullOrWhiteSpace(mapName) ? null : Path.Combine(Application.persistentDataPath, mapName + ".map");
	}


	public void Action()
	{
		var path = GetSelectedPath();
		if (path == null)
			return;

		if (_isSaveMode)
			Save(path);
		else
			Load(path);

		Close();
	}


	public void Save(string path)
	{
		using (var writer = new BinaryWriter(File.Open(path, FileMode.Create)))
		{
			writer.Write(MAP_FILE_VERSION);
			_hexGrid.Save(writer);
		}
		Debug.Log($"The file has been saved {path}");
	}


	public void Load(string path)
	{
		if (!File.Exists(path))
		{
			Debug.LogError($"File does not exist{path}");
			return;
		}

		using (var reader = new BinaryReader(File.OpenRead(path)))
		{
			var header = reader.ReadInt32();
			if (header <= MAP_FILE_VERSION)
			{
				_hexGrid.Load(reader, header);
				HexMapCamera.ValidatePosition();
			}
			else
			{
				Debug.LogWarning($"Unknown map format " + header);
			}
		}
	}


	public void SelectItem(string fileName)
	{
		_nameInput.text = fileName;
	}


	private void FillList()
	{
		for (var i = 0; i < _listContent.childCount; i++)
		{
			Destroy(_listContent.GetChild(i).gameObject);
		}

		var paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
		Array.Sort(paths);

		foreach (var path in paths)
		{
			var item = Instantiate(_itemPrefab, _listContent, false);
			item.Init(this, Path.GetFileNameWithoutExtension(path));
		}
	}


	public void Delete()
	{
		var path = GetSelectedPath();

		if (path == null)
			return;
		
		if (File.Exists(path))
			File.Delete(path);

		_nameInput.text = "";
		FillList();
	}
}
