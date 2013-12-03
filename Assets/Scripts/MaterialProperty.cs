using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class MaterialProperty : MonoBehaviour {
    private float mMod;

    public float mod {
        get { 
            return mMod;
        }
        set {
            mMod = value;
            if(Application.isPlaying)
                renderer.material.SetFloat("_Mod", value); 
        }
    }

    void Awake() {
        mMod = renderer.sharedMaterial.GetFloat("_Mod");
    }
}
