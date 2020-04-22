using System.Collections;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class ExplosiveBarrel : MonoBehaviour
{
	//[Range(1f, 8f)]
	//public float radius = 1f;
	//public float damage = 10f;
	//public Color color = Color.red;
	public BarrelType barrelType;

	MaterialPropertyBlock mpb;
	static readonly int shPropColor = Shader.PropertyToID("_Color");

	public MaterialPropertyBlock Mpb
	{
		get
		{
			if (mpb == null)
			{ mpb = new MaterialPropertyBlock(); }
			return mpb;
		}
	}

	[ContextMenu("Do Something!")]
	public void DoSomething()
	{
		Debug.Log("DoSomething");
	}

	private void OnDrawGizmos()
	{
		if (barrelType == null)
		{ return; }

		Handles.color = barrelType.color;
		Handles.DrawWireDisc(transform.position, transform.up, barrelType.radius);
		Handles.color = Color.white;
		//Gizmos.DrawWireSphere(transform.position, radius);
	}

	private void OnValidate()
	{ 
		ApplyColor();
	}

	private void Awake()
	{
		//Shader shader = Shader.Find("Default/Diffuse");
		//Material mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

		//// !!! will duplicate the material
		//GetComponent<MeshRenderer>().material.color = Color.red;

		//// !!! will modify the *asset*
		//GetComponent<MeshRenderer>().sharedMaterial.color = Color.red;
	}

	void OnEnable()
	{
		ApplyColor();
		ExplosiveBarrelManager.allTheBarrels.Add(this);
	}

	private void OnDisable() => ExplosiveBarrelManager.allTheBarrels.Remove(this);

	public void ApplyColor()
	{
		if (barrelType == null)
		{ return; }

		MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
		//meshRenderer.material.SetColor(shPropColor, color);
		Mpb.SetColor(shPropColor, barrelType.color);
		meshRenderer.SetPropertyBlock(mpb);
	}
}