using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class Waypoint
{
	public Vector3 position = Vector3.zero;
	public Quaternion rotation = Quaternion.identity;
}

public class WaypointManager : MonoBehaviour
{
	public List<Waypoint> waypoints = new List<Waypoint>();
	public bool showTransform;

	float accuracy = 1f;
	Vector3 goal;
	int index = 0;

	public void Move(Transform transform, float speed, float turnSpeed)
	{
		if (Vector3.Distance(transform.position, goal) < accuracy)
		{ index++; }

		if (index == waypoints.Count)
		{ index = 0; }

		goal = waypoints[index].position;
		Vector3 direction = goal - transform.position;
		Quaternion targetRotation = Quaternion.LookRotation(direction);

		transform.rotation = Quaternion.Slerp(transform.rotation,
											  targetRotation,
											  Time.deltaTime * turnSpeed);

		transform.Translate(0f, 0f, speed * Time.deltaTime);
	}

	private void OnDrawGizmos()
	{
#if UNITY_EDITOR			
		Vector3[] debugVectors = new Vector3[waypoints.Count];
		for (int i = 0; i < waypoints.Count; i++)
		{
			debugVectors[i] = waypoints[i].position;
			Handles.Label(waypoints[i].position + new Vector3(0f, 2f, 0f), i.ToString());
		}
		Handles.DrawAAPolyLine(debugVectors);
		Handles.DrawAAPolyLine(debugVectors[debugVectors.Length - 1], debugVectors[0]);
#endif
	}
}
