using UnityEngine;
using UnityEditor;
using System.Collections;
using ProBuilder2.MeshOperations;

namespace ProBuilder2.Actions
{
	public class StripProBuilderScripts : Editor 
	{
		[MenuItem("Tools/" + pb_Constant.PRODUCT_NAME + "/Actions/Strip All ProBuilder Scripts in Scene")]
		public static void StripAllScenes()
		{
			
			if(!EditorUtility.DisplayDialog("Strip ProBuilder Scripts", "This will remove all ProBuilder scripts in the scene.  You will no longer be able to edit these objects.  There is no undo, please exercise caution!\n\nAre you sure you want to do this?", "Okay", "Cancel"))
				return;

			pb_Object[] all = (pb_Object[])FindObjectsOfType(typeof(pb_Object));

			Strip(all);
		}

		[MenuItem("Tools/" + pb_Constant.PRODUCT_NAME + "/Actions/Strip ProBuilder Scripts in Selection")]
		public static void StripAllSelected()
		{
			if(!EditorUtility.DisplayDialog("Strip ProBuilder Scripts", "This will remove all ProBuilder scripts on the selected objects.  You will no longer be able to edit these objects.  There is no undo, please exercise caution!\n\nAre you sure you want to do this?", "Okay", "Cancel"))
				return;

			pb_Object[] all = pbUtil.GetComponents<pb_Object>(Selection.transforms);

			GameObject[] gos = new GameObject[all.Length];
			for(int i = 0; i < all.Length; i++) gos[i] = all[i].gameObject;

			Strip(all);
		}

		public static void Strip(pb_Object[] all)
		{
			for(int i = 0; i < all.Length; i++)
			{
				EditorUtility.DisplayProgressBar(
					"Stripping ProBuilder Scripts",
					"Working over " + all[i].id + ".",
					((float)i / all.Length));

				Mesh m = pbMeshUtils.DeepCopyMesh(all[i].msh);
				m.name = all[i].msh.name;

				GameObject go = all[i].gameObject;

				if(all[i].GetComponent<pb_Entity>())
					DestroyImmediate(all[i].GetComponent<pb_Entity>());
				DestroyImmediate(all[i]);

				go.GetComponent<MeshFilter>().sharedMesh = m;
				if(go.GetComponent<MeshCollider>())
					go.GetComponent<MeshCollider>().sharedMesh = m;
			}

			EditorUtility.ClearProgressBar();
			EditorUtility.DisplayDialog("Strip ProBuilder Scripts", "Successfully stripped out all ProBuilder components.", "Okay");

		}
	}
}