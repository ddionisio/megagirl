using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class GOMimicScale : MonoBehaviour {
    public string targetTag;
    public Transform target;

    void Awake() {
        GetTarget();
    }

    void GetTarget() {
        if(!target) {
            if(!string.IsNullOrEmpty(targetTag)) {
                GameObject go = GameObject.FindGameObjectWithTag(targetTag);
                target = go ? go.transform : null;
            }
        }
    }
    	
	// Update is called once per frame
	void LateUpdate () {
#if UNITY_EDITOR
        if(!Application.isPlaying) {
            GetTarget();
        }
#endif

        if(target)
            transform.localScale = target.localScale;
	}
}
