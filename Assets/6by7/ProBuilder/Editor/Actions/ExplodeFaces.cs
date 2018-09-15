using UnityEngine;
using UnityEditor;
using System.Collections;
using ProBuilder2.Math;
using ProBuilder2.Common;

public class ExplodeFaces : MonoBehaviour
{
	[MenuItem("Tools/" + pb_Constant.PRODUCT_NAME + "/Actions/API Examples/Explode Faces")]
	public static void InEditor()
	{
		if(null != Selection.activeTransform.GetComponent<pb_Object>())
			ExplodeFaces.ExplodeObject(Selection.activeTransform.GetComponent<pb_Object>());
	}

	public float explosionForce = 5f;

	GameObject cube;
	GameObject[] pieces;

	public void Start()
	{
		cube = ProBuilder.CreatePrimitive(ProBuilder.Shape.Cube).gameObject;
	}

	public void OnGUI()
	{
		if(GUILayout.Button("Reset"))
		{
			Destroy(cube);
			for(int i = 0; i < pieces.Length; i++)
				Destroy(pieces[i]);
			cube = ProBuilder.CreatePrimitive(ProBuilder.Shape.Cube).gameObject;
		}

		if(GUILayout.Button("Explode!!"))
			pieces = ExplodeObject(cube.GetComponent<pb_Object>());
	}

	// breaks a pb_object into a zillion* faces
	public static GameObject[] ExplodeObject(pb_Object pb)
	{
		// disable 'ze donor
		pb.gameObject.SetActive(false);
		
		GameObject[] pieces = new GameObject[pb.faces.Length];
		
		// extract mesh and material information for every face, and assign it to a gameobject
		for(int i = 0; i < pieces.Length; i++)
		{
			Mesh m = new Mesh();
			m.vertices 	= pb.GetVertices(pb.faces[i]);
			m.triangles	= new int[6] {0,1,2, 1,3,2};
			m.normals  	= pb.GetNormals(pb.faces[i]);
			m.uv	  	= pb.GetUVs(pb.faces[i]);
			m.RecalculateBounds();

			GameObject go = new GameObject();
			go.transform.position = pb.transform.position + pb_Math.PlaneNormal(m.vertices).normalized * .3f;
			go.transform.localRotation = pb.transform.localRotation;
			
			go.AddComponent<MeshFilter>().sharedMesh = m;
			go.AddComponent<MeshRenderer>().sharedMaterial = pb.GetMaterial(pb.faces[i]);

			pieces[i] = go;
		}

		return pieces;
	}
}
