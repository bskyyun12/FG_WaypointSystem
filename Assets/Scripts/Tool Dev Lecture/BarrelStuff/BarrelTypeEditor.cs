using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(BarrelType))]
public class BarrelTypeEditor : Editor
{
	public float thing0; // serialized, visible, public
	float thing1; // not serialized, hidden, private
	[SerializeField] float thing2; // serialized, visible, private
	[HideInInspector] public float thing3; // serialized, hidden, public

	public enum Things
	{
		Bleep, Bloop, Blap
	}

	Things things;
	private float someValue;

	SerializedObject so;
	SerializedProperty propRadius;
	SerializedProperty propDamage;
	SerializedProperty propColor;

	// everytime we select an object, it will call OnEnable
	private void OnEnable()
	{
		so = serializedObject;
		propRadius = so.FindProperty("radius");
		propDamage = so.FindProperty("damage");
		propColor = so.FindProperty("color");
	}

	public override void OnInspectorGUI()
	{
		// This can be replaced
		// base.OnInspectorGUI();
		EditorGUILayout.PropertyField(propRadius);
		EditorGUILayout.PropertyField(propDamage);
		EditorGUILayout.PropertyField(propColor);
		//so.ApplyModifiedProperties();
		if (so.ApplyModifiedProperties())
		{
			ExplosiveBarrelManager.UpdateAllBarrelColors();
		}


		GUILayout.Space(20);

		// Way 1: BeginHorizontal - EndHorizontal
		GUILayout.Label("Way 1: BeginHorizontal - EndHorizontal", EditorStyles.boldLabel);
		GUILayout.BeginHorizontal();
		GUILayout.Label("Things: ", GUILayout.Width(60));
		if (GUILayout.Button("Do a thing"))
		{ Debug.Log("did the thing"); }
		things = (Things)EditorGUILayout.EnumPopup(things);
		GUILayout.EndHorizontal();

		GUILayout.Space(20);

		// Way 2(safer): HorizontalScope
		GUILayout.Label("Way 2(safer): HorizontalScope", EditorStyles.boldLabel);
		using (new GUILayout.HorizontalScope())
		{
			GUILayout.Label("Things: ", GUILayout.Width(60));
			if (GUILayout.Button("Do a thing"))
			{ Debug.Log("did the thing"); }
			things = (Things)EditorGUILayout.EnumPopup(things);
		}

		GUILayout.Space(20);

		// Example layout
		GUILayout.Label("Example layout", EditorStyles.boldLabel);
		using (new GUILayout.VerticalScope(EditorStyles.helpBox))
		{
			using (new GUILayout.HorizontalScope())
			{
				GUILayout.Label("SomeValue: ", GUILayout.Width(60));
				someValue = GUILayout.HorizontalSlider(someValue, -1f, 1f);
			}
			GUILayout.Label("Things");
			GUILayout.Label("Things", GUI.skin.button);


			EditorGUILayout.ObjectField("Assign here: ", null, typeof(Transform), true);
		}



		// explicit positioning using rect
		// GUI
		// EditorGUI

		// implicit positioning, auto-layout
		// GUILayout
		// EditorGUILayout
	}
}