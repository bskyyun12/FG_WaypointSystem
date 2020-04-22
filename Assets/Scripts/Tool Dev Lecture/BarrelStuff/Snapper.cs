using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Snapper
{
	const string UNDO_STR_SNAP = "snap objects";

	[MenuItem("Gwangyeong/Snap Selected Objects %&S", isValidateFunction: true)]
	public static bool SnapTheThingsValidate()
	{
		return Selection.gameObjects.Length > 0;
	}

	[MenuItem("Gwangyeong/Snap Selected Objects %&S")]
	public static void SnapTheThings()
	{
		Debug.Log("Snap");
		foreach (var go in Selection.gameObjects)
		{
			Undo.RecordObject(go.transform, UNDO_STR_SNAP);
			go.transform.position = go.transform.position.Round();
		} 
	}
}
