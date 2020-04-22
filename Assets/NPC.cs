using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
	[SerializeField] WaypointManager waypointManager = default;

	[SerializeField] float speed = 5f;
	[SerializeField] float turnSpeed = 5f;

	void LateUpdate()
	{
		waypointManager.Move(transform, speed, turnSpeed);
	}
}
