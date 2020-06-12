using UnityEngine;


public class CameraManager : MonoBehaviour
{
	[SerializeField] private float speed = 0.5f;

	private int _border = 5;

	void Update()
	{
		if (Input.GetKey(KeyCode.UpArrow) || Input.mousePosition.y > Screen.height - _border)
			transform.position += new Vector3(1, 0, 1) * speed;

		if (Input.GetKey(KeyCode.DownArrow) || Input.mousePosition.y < _border)
			transform.position += new Vector3(-1, 0, -1) * speed;

		if (Input.GetKey(KeyCode.RightArrow) || Input.mousePosition.x > Screen.width - _border)
			transform.position += new Vector3(1, 0, -1) * speed;

		if (Input.GetKey(KeyCode.LeftArrow) || Input.mousePosition.x < _border)
			transform.position += new Vector3(-1, 0, 1) * speed;
	}
}
