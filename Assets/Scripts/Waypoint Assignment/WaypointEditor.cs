using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(WaypointManager))]
public class WaypointEditor : Editor
{
	SerializedObject so;
	SerializedProperty propWaypoints;
	SerializedProperty propShowTransform;

	ReorderableList waypointList;

	private void OnEnable()
	{
		so = serializedObject;
		propWaypoints = so.FindProperty("waypoints");
		propShowTransform = so.FindProperty("showTransform");

		waypointList = new ReorderableList(so, propWaypoints, true, true, true, true)
		{
			// ReorderableList Delegates
			drawElementCallback = DrawListItems,
			drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Waypoints: "); },
			onAddCallback = AddListItem
		};

		SceneView.duringSceneGui += DuringSceneGUI;

		Tools.hidden = true;
	}

	private void OnDisable()
	{
		SceneView.duringSceneGui -= DuringSceneGUI;

		Tools.hidden = false;
	}

	public override void OnInspectorGUI()
	{
		so.Update();

		waypointList.DoLayoutList();
		propShowTransform.boolValue = EditorGUILayout.Toggle(propShowTransform.displayName, propShowTransform.boolValue);

		if (so.ApplyModifiedProperties())
		{ SceneView.RepaintAll(); }
	}

	private void DuringSceneGUI(SceneView sceneView)
	{
		so.Update();
		float lowestDist = 1000f;
		int currentIndex = 0;
		for (int i = 0; i < propWaypoints.arraySize; i++)
		{
			DrawMoveHandleOnWaypoint(i);

			int nextIndex = (int)Mathf.Repeat(i + 1, propWaypoints.arraySize);
			Vector3 currentPropPos = propWaypoints.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector3Value;
			Vector3 nextPropPos = propWaypoints.GetArrayElementAtIndex(nextIndex).FindPropertyRelative("position").vector3Value;

			float dist = HandleUtility.DistanceToLine(currentPropPos, nextPropPos);
			if (lowestDist > dist)
			{
				lowestDist = dist;
				currentIndex = i;
			}
		}
		if (so.ApplyModifiedProperties())
		{ Repaint(); }

		bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
		bool IsNearLine = lowestDist < 30f;
		if (Event.current.type == EventType.MouseDown && holdingAlt && IsNearLine)
		{
			int nextIndex = (int)Mathf.Repeat(currentIndex + 1, propWaypoints.arraySize);
			Vector3 propPos0 = propWaypoints.GetArrayElementAtIndex(currentIndex).FindPropertyRelative("position").vector3Value;
			Vector3 propPos1 = propWaypoints.GetArrayElementAtIndex(nextIndex).FindPropertyRelative("position").vector3Value;

			Vector3 dir0To1 = (propPos1 - propPos0).normalized;
			Vector3 dir1To0 = (propPos0 - propPos1).normalized;

			float distToExtendVector0 = HandleUtility.DistanceToLine(propPos0, propPos0 + dir1To0);
			float distToExtendVector1 = HandleUtility.DistanceToLine(propPos1, propPos1 + dir0To1);

			float sum = distToExtendVector0 + distToExtendVector1;
			float closerDistToExtendVector = distToExtendVector0 < distToExtendVector1 ? distToExtendVector0 : distToExtendVector1;

			// Calulate percentage
			float lengthPercentage = closerDistToExtendVector / sum;
			Vector3 worldDirBetweenPoints = propPos1 - propPos0;
			float lengthBetweenPoints = worldDirBetweenPoints.magnitude;
			float percentage = lengthBetweenPoints * lengthPercentage;

			int closerIndex = distToExtendVector0 < distToExtendVector1 ? currentIndex : nextIndex;
			//Debug.Log("Mouse Point is closer to " + closerIndex);

			Vector3 CloserPos = propWaypoints.GetArrayElementAtIndex(closerIndex).FindPropertyRelative("position").vector3Value;
			Vector3 targetPosition = CloserPos +
					(closerIndex == currentIndex ? dir0To1 : dir1To0) * percentage;

			AddItem(currentIndex, targetPosition);

			Repaint();
			Event.current.Use(); // consume the event, don't let it fall through
		}

		//DrawButtons(currentIndex);
		DrawLinesBetweenPoints();
	}

	private void DrawMoveHandleOnWaypoint(int currentIndex)
	{
		SerializedProperty prop = propWaypoints.GetArrayElementAtIndex(currentIndex);
		SerializedProperty propPosition = prop.FindPropertyRelative("position");
		SerializedProperty propRotation = prop.FindPropertyRelative("rotation");

		if (currentIndex == 0)
		{ Handles.color = Color.green; }
		else if (currentIndex == propWaypoints.arraySize - 1)
		{ Handles.color = Color.red; }

		propPosition.vector3Value = Handles.FreeMoveHandle(propPosition.vector3Value, propRotation.quaternionValue, 1f, Vector3.one, Handles.SphereHandleCap);
		Handles.color = Color.white;

		if (propShowTransform.boolValue)
		{
			propPosition.vector3Value = Handles.PositionHandle(propPosition.vector3Value, propRotation.quaternionValue.normalized);
			propRotation.quaternionValue = Handles.RotationHandle(propRotation.quaternionValue, propPosition.vector3Value);
		}

		Handles.Label(propPosition.vector3Value + new Vector3(0f, 2f, 0f), currentIndex.ToString());
	}

	private void DrawLinesBetweenPoints()
	{
		Vector3[] debugVectors = new Vector3[propWaypoints.arraySize];
		for (int i = 0; i < propWaypoints.arraySize; i++)
		{
			SerializedProperty prop = propWaypoints.GetArrayElementAtIndex(i);
			SerializedProperty propPosition = prop.FindPropertyRelative("position");
			debugVectors[i] = propPosition.vector3Value;
		}

		Handles.DrawAAPolyLine(debugVectors);
		// connects end and first
		Handles.color = Color.magenta;
		Handles.DrawAAPolyLine(debugVectors[debugVectors.Length - 1], debugVectors[0]);
		Handles.color = Color.white;
	}

	private void DrawButtons(int currentIndex)
	{
		int nextIndex = (int)Mathf.Repeat(currentIndex + 1, propWaypoints.arraySize);

		Vector3 currentPropPos = propWaypoints.GetArrayElementAtIndex(currentIndex).FindPropertyRelative("position").vector3Value;
		Vector3 nextPropPos = propWaypoints.GetArrayElementAtIndex(nextIndex).FindPropertyRelative("position").vector3Value;
		Vector3 halfPos = (currentPropPos + nextPropPos) / 2f;

		Handles.color = Color.cyan;

		// halfPos button
		Vector3 buttonPos = halfPos;
		if (Handles.Button(buttonPos, Quaternion.identity, .55f, 1.1f, Handles.SphereHandleCap))
		{ AddItem(currentIndex, buttonPos); }

		// quater button
		buttonPos = (currentPropPos + halfPos) / 2f;
		if (Handles.Button(buttonPos, Quaternion.identity, .55f, 1.1f, Handles.SphereHandleCap))
		{ AddItem(currentIndex, buttonPos); }

		// another quater button
		buttonPos = (nextPropPos + halfPos) / 2f;
		if (Handles.Button(buttonPos, Quaternion.identity, .55f, 1.1f, Handles.SphereHandleCap))
		{ AddItem(currentIndex, buttonPos); }

		Handles.color = Color.white;
	}

	void AddListItem(ReorderableList l)
	{
		AddItem(l.index);
	}

	void AddItem(int currentIndex)
	{
		so.Update();
		int nextIndex = (int)Mathf.Repeat(currentIndex + 1, propWaypoints.arraySize);

		SerializedProperty currentProp = propWaypoints.GetArrayElementAtIndex(currentIndex);
		SerializedProperty nextProp = propWaypoints.GetArrayElementAtIndex(nextIndex);

		Vector3 selectedPropPos = currentProp.FindPropertyRelative("position").vector3Value;
		Vector3 nextPropPos = nextProp.FindPropertyRelative("position").vector3Value;

		Vector3 newPos = (selectedPropPos + nextPropPos) / 2;

		propWaypoints.InsertArrayElementAtIndex(currentIndex);

		SerializedProperty newProp = propWaypoints.GetArrayElementAtIndex(currentIndex + 1);
		newProp.FindPropertyRelative("position").vector3Value = newPos;
		so.ApplyModifiedProperties();
	}

	void AddItem(int currentIndex, Vector3 position)
	{
		so.Update();
		propWaypoints.InsertArrayElementAtIndex(currentIndex);

		SerializedProperty newProp = propWaypoints.GetArrayElementAtIndex(currentIndex + 1);
		newProp.FindPropertyRelative("position").vector3Value = position;
		so.ApplyModifiedProperties();
	}

	void DrawListItems(Rect rect, int index, bool isActive, bool isFocused)
	{
		SerializedProperty element = waypointList.serializedProperty.GetArrayElementAtIndex(index);

		EditorGUI.LabelField(new Rect(rect.x, rect.y, 50, EditorGUIUtility.singleLineHeight), index.ToString() + ". ");

		EditorGUI.PropertyField(
			new Rect(new Rect(rect.x + 15, rect.y, rect.width - 15, EditorGUIUtility.singleLineHeight)),
			element.FindPropertyRelative("position"),
			GUIContent.none
		);
	}
}