using UnityEditor;
using UnityEngine;



[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer
{


	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		var coords = new HexCoordinates(
			property.FindPropertyRelative("_x").intValue,
			property.FindPropertyRelative("_z").intValue);
		
		GUI.enabled = false;
		EditorGUI.Vector3IntField(position, label, coords);
		GUI.enabled = true;
	}


}