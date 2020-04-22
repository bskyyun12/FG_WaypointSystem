using System.Collections.Generic;
using UnityEditor;

#if UNITY_EDITOR
using UnityEngine;
#endif

public class ExplosiveBarrelManager : MonoBehaviour
{
	public static List<ExplosiveBarrel> allTheBarrels = new List<ExplosiveBarrel>();

	public static void UpdateAllBarrelColors()
	{
		foreach (var barrel in allTheBarrels)
		{
			barrel.ApplyColor();
		}
	}

	private void OnDrawGizmos()
	{
		foreach (var barrel in allTheBarrels)
		{
			if (barrel.barrelType == null)
			{ return; }

			#if UNITY_EDITOR
			Vector3 managerPos = transform.position;
			Vector3 barrelPos = barrel.transform.position;
			float halfHeight = (managerPos.y - barrelPos.y) * 0.5f;
			Vector3 offset = Vector3.up * halfHeight;

			Handles.DrawBezier(
				managerPos,
				barrelPos, 
				managerPos - offset, 
				barrelPos + offset, 
				barrel.barrelType.color, 
				EditorGUIUtility.whiteTexture, 
				1f);
			//Handles.DrawAAPolyLine(transform.position, barrel.transform.position);
			#endif
		}
	}
}