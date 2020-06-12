using Sirenix.OdinInspector;
using UnityEngine;



[CreateAssetMenu(fileName = "NewHexUnitData", menuName = "HexData/UnitData", order = 1)]
public class HexUnitData : ScriptableObject
{


	public new string name = "Placeholder";
	public HexUnit prefab;

	public int visionRange = 3;
	[Tooltip("Value used to extract move cost of every cell.")]
	public int speed = 24;

	public int damage;
	public int armor;
	public int maxHp;

	[FoldoutGroup("Visual Motion")]
	public float travelSpeed = 4f;
	[FoldoutGroup("Visual Motion")]
	public float rotationSpeed = 180f;

}
