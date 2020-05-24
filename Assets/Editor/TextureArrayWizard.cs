﻿using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;



public class TextureArrayWizard : ScriptableWizard
{


	public Texture2D[] textures;



	[MenuItem("Assets/Create/Texture Array"), UsedImplicitly]
	static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<TextureArrayWizard>("Create Texture Array", "Create");
	}


	private void OnWizardCreate()
	{
		if (textures.Length == 0)
		{
			return;
		}

		var path = 
			EditorUtility.SaveFilePanelInProject(
				"Save Texture Array", 
				"Texture Array", 
				"asset", 
				"Save Texture Array");

		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		var t = textures[0];
		var textureArray = new Texture2DArray(t.width, t.height, textures.Length, t.format, t.mipmapCount > 1)
		{
			anisoLevel = t.anisoLevel, 
			filterMode = t.filterMode, 
			wrapMode = t.wrapMode
		};

		for (var i = 0; i < textures.Length; i++)
		{
			for (var m = 0; m < t.mipmapCount; m++)
			{
				Graphics.CopyTexture(textures[i], 0, m, textureArray, i, m);
			}
		}

		AssetDatabase.CreateAsset(textureArray, path);
	}

}
