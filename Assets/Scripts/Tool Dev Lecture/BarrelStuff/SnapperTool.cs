using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class SnapperTool : EditorWindow
{
	/*
      
    1. Add a setting for setting grid size, 
        where 1 snaps to 1m units, 2 snaps to 2m units, 0.5 snaps to 0.5m units, etc

    2. Make the grid visible in the scene view

    3. Add support for a polar grid in addition to the regular grid! 
        where you can set both snap size as well as the number of angular divisions

    4. Make the settings persist between unity sessions         

    */

	public enum GridType
	{
		Cartesian,
		Polar
	}

	[MenuItem("Tools/Snapper")]
	public static void OpenTheThing() => GetWindow<SnapperTool>("Snapper");

	const float TAU = 6.28318530718f;

	public float gridSize = 1f;
	public GridType gridType = GridType.Cartesian;
	public int angularDivisions = 24;

	// array
	public Vector3[] points;

	SerializedObject so;
	SerializedProperty propGridSize;
	SerializedProperty propGridType;
	SerializedProperty propAngularDivisions;

	// array
	SerializedProperty propPoints;

	void OnEnable()
	{
		so = new SerializedObject(this);
		propGridSize = so.FindProperty("gridSize");
		propGridType = so.FindProperty("gridType");
		propAngularDivisions = so.FindProperty("angularDivisions");

		// load saved configuration
		gridSize = EditorPrefs.GetFloat("SNAPPER_TOOL_gridSize", 1f);
		gridType = (GridType)EditorPrefs.GetInt("SNAPPER_TOOL_gridType", 0);
		angularDivisions = EditorPrefs.GetInt("SNAPPER_TOOL_angularDivisions", 24);

		// array
		propPoints = so.FindProperty("points");

		Selection.selectionChanged += Repaint;
		SceneView.duringSceneGui += DuringSceneGUI;
	}

	void OnDisable()
	{
		Selection.selectionChanged -= Repaint;
		SceneView.duringSceneGui -= DuringSceneGUI;

		// save configuration
		EditorPrefs.SetFloat("SNAPPER_TOOL_gridSize", gridSize);
		EditorPrefs.SetInt("SNAPPER_TOOL_gridType", (int)gridType);
		EditorPrefs.SetInt("SNAPPER_TOOL_angularDivisions", angularDivisions);
	}

	void DuringSceneGUI(SceneView sceneView)
	{
		// array
		so.Update();
		for (int i = 0; i < propPoints.arraySize; i++)
		{
			SerializedProperty prop = propPoints.GetArrayElementAtIndex(i);
			//prop.FindPropertyRelative("haoitr");
			prop.vector3Value = Handles.PositionHandle(prop.vector3Value, Quaternion.identity);
		}
		so.ApplyModifiedProperties();

		Handles.zTest = CompareFunction.LessEqual;
		const float GRID_DRAW_EXTENT = 16;
		switch (gridType)
		{
			case GridType.Cartesian:
				DrawGridCartesian(GRID_DRAW_EXTENT);
				break;
			case GridType.Polar:
				DrawGridPolar(GRID_DRAW_EXTENT);
				break;
			default:
				break;
		}

	}

	private void DrawGridPolar(float GRID_DRAW_EXTENT)
	{
		int ringCount = Mathf.RoundToInt(GRID_DRAW_EXTENT / gridSize);

		// radial grid (rings)
		for (int i = 1; i < ringCount; i++)
		{
			Handles.DrawWireDisc(Vector3.zero, Vector3.up, gridSize * i);
		}

		// angular grid (lines)
		for (int i = 0; i < angularDivisions; i++)
		{
			float t = i / ((float)angularDivisions);
			float angleRad = t * TAU;
			float x = Mathf.Cos(angleRad);
			float y = Mathf.Sin(angleRad);
			Vector3 dir = new Vector3(x, 0f, y);
			Handles.DrawAAPolyLine(Vector3.zero, dir * (ringCount - 1) * gridSize);
		}
	}

	private void DrawGridCartesian(float GRID_DRAW_EXTENT)
	{
		if (Event.current.type == EventType.Repaint)
		{
			int lineCount = Mathf.RoundToInt((GRID_DRAW_EXTENT * 2) / gridSize);
			int halfLineCount = lineCount / 2;
			if (lineCount % 2 == 0)
			{
				lineCount++; // make sure it's an odd number
			}

			for (int i = 0; i < lineCount; i++)
			{
				float intOffset = i - halfLineCount;

				float xCoord = intOffset * gridSize;
				float zCoord0 = halfLineCount * gridSize;
				float zCoord1 = -halfLineCount * gridSize;
				Vector3 p0 = new Vector3(xCoord, 0f, zCoord0);
				Vector3 p1 = new Vector3(xCoord, 0f, zCoord1);
				Handles.DrawAAPolyLine(p0, p1);

				p0 = new Vector3(zCoord0, 0f, xCoord);
				p1 = new Vector3(zCoord1, 0f, xCoord);
				Handles.DrawAAPolyLine(p0, p1);
			}
		}
	}

	void OnGUI()
	{
		so.Update();
		EditorGUILayout.PropertyField(propGridType);
		EditorGUILayout.PropertyField(propGridSize);
		if (gridType == GridType.Polar)
		{
			EditorGUILayout.PropertyField(propAngularDivisions);
			propAngularDivisions.intValue = Mathf.Max(4, propAngularDivisions.intValue);
		}
		EditorGUILayout.PropertyField(propPoints);
		so.ApplyModifiedProperties();

		using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
		{
			if (GUILayout.Button("Snap Selection"))
				SnapSelection();
		}
	}

	void SnapSelection()
	{
		foreach (GameObject go in Selection.gameObjects)
		{
			Undo.RecordObject(go.transform, "snap objects");
			go.transform.position = GetSnappedPosition(go.transform.position);
		}
	}

	Vector3 GetSnappedPosition(Vector3 posOriginal)
	{
		if (gridType == GridType.Cartesian)
		{
			return posOriginal.Round(gridSize);
		}
		if (gridType == GridType.Polar)
		{
			Vector2 vec = new Vector2(posOriginal.x, posOriginal.z);
			float dist = vec.magnitude;
			float distSnapped = dist.Round(gridSize);

			float angRad = Mathf.Atan2(vec.y, vec.x); // 0 to TAU
			float angTurns = angRad / TAU; // 0 to 1
			float angTurnsSnapped = angTurns.Round(1f / angularDivisions);
			float angRadSnapped = angTurnsSnapped * TAU;

			float x = Mathf.Cos(angRadSnapped);
			float y = Mathf.Sin(angRadSnapped);
			Vector2 dirSnapped = new Vector2(x, y);

			Vector2 snappedVec = dirSnapped * distSnapped;
			return new Vector3(snappedVec.x, posOriginal.y, snappedVec.y);			
		}

		return default;
	}
}