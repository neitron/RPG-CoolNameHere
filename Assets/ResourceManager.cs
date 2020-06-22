using UnityEngine;


public class ResourceManager : MonoBehaviour
{
    private void Awake()
    {
	    HexUnitFactory.LoadAssets();
    }

}
