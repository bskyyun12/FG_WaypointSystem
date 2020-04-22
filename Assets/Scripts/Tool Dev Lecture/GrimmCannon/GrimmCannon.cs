using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public struct SpawnData
{
	public Vector2 pointInDisc;
	public float randAngleDeg;

	public void SetRandomValues()
	{
		pointInDisc = Random.insideUnitCircle;
		randAngleDeg = Random.value * 360;
	}
}

public class GrimmCannon : EditorWindow
{
	[MenuItem("Tools/Grimm Cannon")]
	public static void OpenGrimm() => GetWindow<GrimmCannon>();

	public float radius = 2f;
	public int spawnCount = 8;

	SerializedObject so;
	SerializedProperty propRadius;
	SerializedProperty propSpawnCount;
	SerializedProperty propSpawnPrefab;
	SerializedProperty propPreviewMaterial;

	SpawnData[] spawnDataPoints;
	GameObject[] prefabs;
	GameObject spawnPrefab;

	private void OnEnable()
	{
		so = new SerializedObject(this);
		propRadius = so.FindProperty("radius");
		propSpawnCount = so.FindProperty("spawnCount");
		GenerateRandomPoints();

		SceneView.duringSceneGui += DuringSceneGUI;

		// load prefabs
		string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" });
		IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
		prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
	}

	private void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;

	void GenerateRandomPoints()
	{
		spawnDataPoints = new SpawnData[spawnCount];
		for (int i = 0; i < spawnCount; i++)
		{
			spawnDataPoints[i].SetRandomValues();
		}
	}

	private void OnGUI()
	{
		so.Update();
		EditorGUILayout.PropertyField(propRadius);
		propRadius.floatValue = propRadius.floatValue.AtLeast(1f);
		EditorGUILayout.PropertyField(propSpawnCount);
		propSpawnCount.intValue = propSpawnCount.intValue.AtLeast(1);

		if (so.ApplyModifiedProperties())
		{
			GenerateRandomPoints();
			SceneView.RepaintAll();
		}

		// if you clicked left mouse button, in the editor window
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			GUI.FocusControl(null);
			Repaint(); // repaint on the editor window UI
		}
	}

	void DuringSceneGUI(SceneView sceneView)
	{
		Handles.BeginGUI();

		Rect rect = new Rect(8, 8, 64, 64);
		foreach (var prefab in prefabs)
		{
			Texture icon = AssetPreview.GetAssetPreview(prefab);
			if (GUI.Toggle(rect, spawnPrefab == prefab, new GUIContent(icon)))
			{
				spawnPrefab = prefab;
			}
			rect.y += rect.height + 2;
		}

		Handles.EndGUI();


		Handles.zTest = CompareFunction.LessEqual;
		Transform camTransform = sceneView.camera.transform;

		// make sure it repaints on mouse move
		if (Event.current.type == EventType.MouseMove)
		{
			sceneView.Repaint();
		}

		// change radius
		bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
		if (Event.current.type == EventType.ScrollWheel && !holdingAlt)
		{
			float scrolldir = Mathf.Sign(Event.current.delta.y);
			so.Update();
			propRadius.floatValue *= 1 + scrolldir * .1f;
			so.ApplyModifiedProperties();
			Repaint();
			Event.current.Use(); // consume the event, don't let it fall through
		}

		if (TryRaycastFromCamera(camTransform.up, out Matrix4x4 tangentToWorld))
		{
			// draw circle marker
			DrawCircleRegion(tangentToWorld);

			// draw all spawn positions and meshes
			List<Pose> spawnPoses = GetSpawnPoses(tangentToWorld);
			DrawSpawnPreviews(spawnPoses);

			// spawn on press
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
				TrySpawnObjects(spawnPoses);
		}
	}

	void TrySpawnObjects(List<Pose> poses)
	{
		if (spawnPrefab == null)
			return;
		foreach (Pose pose in poses)
		{
			// spawn prefab
			GameObject spawnedThing = (GameObject)PrefabUtility.InstantiatePrefab(spawnPrefab);
			Undo.RegisterCreatedObjectUndo(spawnedThing, "Spawn Objects");
			spawnedThing.transform.position = pose.position;
			spawnedThing.transform.rotation = pose.rotation;
		}

		GenerateRandomPoints(); // update points
	}

	List<Pose> GetSpawnPoses(Matrix4x4 tangentToWorld)
	{
		List<Pose> hitPoses = new List<Pose>();
		foreach (SpawnData rndDataPoint in spawnDataPoints)
		{
			// create ray for this point
			Ray ptRay = GetCircleRay(tangentToWorld, rndDataPoint.pointInDisc);
			// raycast to find point on surface
			if (Physics.Raycast(ptRay, out RaycastHit ptHit))
			{
				// calculate rotation and assign to pose together with position
				Quaternion randRot = Quaternion.Euler(0f, 0f, rndDataPoint.randAngleDeg);
				Quaternion rot = Quaternion.LookRotation(ptHit.normal) * (randRot * Quaternion.Euler(90f, 0f, 0f));
				Pose pose = new Pose(ptHit.point, rot);
				hitPoses.Add(pose);
			}
		}

		return hitPoses;
	}

	void DrawSpawnPreviews(List<Pose> spawnPoses)
	{
		foreach (Pose pose in spawnPoses)
		{
			if (spawnPrefab != null)
			{
				// draw preview of all meshes in the prefab
				Matrix4x4 poseToWorld = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
				DrawPrefab(spawnPrefab, poseToWorld);
			}
			else
			{
				// prefab missing, draw sphere and normal on surface instead
				Handles.SphereHandleCap(-1, pose.position, Quaternion.identity, 0.1f, EventType.Repaint);
				Handles.DrawAAPolyLine(pose.position, pose.position + pose.up);
			}
		}
	}

	static void DrawPrefab(GameObject prefab, Matrix4x4 poseToWorld)
	{
		MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>();
		foreach (MeshFilter filter in filters)
		{
			Matrix4x4 childToPose = filter.transform.localToWorldMatrix;
			Matrix4x4 childToWorldMtx = poseToWorld * childToPose;
			Mesh mesh = filter.sharedMesh;
			Material mat = filter.GetComponent<MeshRenderer>().sharedMaterial;
			mat.SetPass(0);
			Graphics.DrawMeshNow(mesh, childToWorldMtx);
		}
	}

	private void DrawCircleRegion(Matrix4x4 localToWorld)
	{
		DrawAxes(localToWorld);
		// draw circle adapted to the terrain
		const int circleDetail = 128;
		Vector3[] ringPoints = new Vector3[circleDetail];
		for (int i = 0; i < circleDetail; i++)
		{
			float t = i / ((float)circleDetail - 1); // go back to 0/1 position
			const float TAU = 6.28318530718f;
			float angRad = t * TAU;
			Vector2 dir = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
			Ray r = GetCircleRay(localToWorld, dir);
			if (Physics.Raycast(r, out RaycastHit cHit))
			{
				ringPoints[i] = cHit.point + cHit.normal * 0.02f;
			}
			else
			{
				ringPoints[i] = r.origin;
			}
		}

		Handles.DrawAAPolyLine(ringPoints);
	}

	Ray GetCircleRay(Matrix4x4 tangentToWorld, Vector2 pointInCircle)
	{
		Vector3 normal = tangentToWorld.MultiplyVector(Vector3.forward);
		Vector3 rayOrigin = tangentToWorld.MultiplyPoint3x4(pointInCircle * radius);
		rayOrigin += normal * 2; // offset margin thing
		Vector3 rayDirection = -normal;
		return new Ray(rayOrigin, rayDirection);
	}

	void DrawAxes(Matrix4x4 localToWorld)
	{
		Vector3 pt = localToWorld.MultiplyPoint3x4(Vector3.zero);
		Handles.color = Color.red;
		Handles.DrawAAPolyLine(6, pt, pt + localToWorld.MultiplyVector(Vector3.right));
		Handles.color = Color.green;
		Handles.DrawAAPolyLine(6, pt, pt + localToWorld.MultiplyVector(Vector3.up));
		Handles.color = Color.blue;
		Handles.DrawAAPolyLine(6, pt, pt + localToWorld.MultiplyVector(Vector3.forward));
	}

	private bool TryRaycastFromCamera(Vector3 cameraUp, out Matrix4x4 tangentToWorldMtx)
	{
		Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hit))
		{
			// setting up tangent space
			Vector3 refZ = hit.normal;
			Vector3 refX = Vector3.Cross(refZ, cameraUp).normalized; // tangent
			Vector3 refY = Vector3.Cross(refZ, refX);   // bitangent
			tangentToWorldMtx = Matrix4x4.TRS(hit.point, Quaternion.LookRotation(hit.normal, refY), Vector3.one);
			return true;
		}

		tangentToWorldMtx = default;
		return false;
	}
}
