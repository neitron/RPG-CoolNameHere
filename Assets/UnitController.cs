using System.Collections;
using System.Runtime.InteropServices;
using UniRx;
using UnityEngine;
using UnityEngine.AI;



[RequireComponent(typeof(NavMeshAgent))]
public class UnitController : MonoBehaviour
{

    private Camera _camera;
	private NavMeshAgent _agent;
	private Vector3 _destination;
	private NavMeshPath _path;
	private int step;
	void Awake()
    {
	    _camera = Camera.main;
	    _agent = GetComponent<NavMeshAgent>();
		_agent.enabled = true;
		_path = new NavMeshPath();
	}



    void Update()
    {
	    if (Input.GetMouseButtonDown(0))
	    {
			var ray = _camera.ScreenPointToRay(Input.mousePosition);

			if (Physics.Raycast(ray, out var hit))
			{
				_agent.autoRepath = false;
				_agent.autoTraverseOffMeshLink = false;
				_agent.ResetPath();
				_agent.SetDestination(hit.point);
				//NavMesh.CalculatePath(transform.position, hit.point, NavMesh.AllAreas, _path);

				//step = 1;
				//_destination = _path.corners[step];
			}
		}

	    //var direction = (_destination - transform.position).normalized;
	    //float distance = Vector3.Distance(_destination, transform.position);

	    //if (distance > 0.1f)
	    //{
		   // var movement = direction * (Time.deltaTime * 5.0f);
		   // _agent.Move(movement);
	    //}
	    //else if(step < _path.corners.Length - 1)
	    //{
		   // step++;
		   // _destination = _path.corners[step];
	    //}
    }


    private IEnumerator ComputeAgentPath()
    {
		var ray = _camera.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out var hit))
		{
			NavMesh.SamplePosition(hit.point, out var navHitStart, 1, -1);
			NavMesh.SamplePosition(transform.position, out var navHitTarget, 1, -1);
			var path = new NavMeshPath();
			NavMesh.CalculatePath(navHitStart.position, navHitTarget.position, NavMesh.AllAreas, path);
			_agent.ResetPath();
			_agent.SetPath(path);

			_agent.isStopped = true;

			yield return null;
			
			_agent.isStopped = false;
		}
	}


    private void OnDrawGizmosSelected()
	{
		var originalColor = Gizmos.color;
		Gizmos.color = Color.red;
		Vector3? prevPoint = null;
		if (_path != null && _path.corners.Length > 0)
			foreach (var point in _path.corners)
			{
				if (prevPoint != null)
				{
					Gizmos.DrawLine(prevPoint.Value, point);
				}
				Gizmos.DrawSphere(point, 0.2f);
				prevPoint = point;
			}

		Gizmos.color = originalColor;
	}


}
