using UnityEngine;
using System.Collections;

/// <summary>
/// Make sure it has a collider trigger to activate the field
/// </summary>
public class CameraField : MonoBehaviour {
    public Bounds bounds;
    public CameraController.Mode mode;
    public bool doTransition = true;

    public string attachTag = "Player";

    public Color boundColor = Color.blue; //for gizmo

    private CameraController mCamCtrl;
    private Transform mAttach;

    //void OnTriggerEnter(Collider col) {
    //}

    void OnTriggerStay(Collider col) {
        mCamCtrl.attach = mAttach;
        mCamCtrl.field = this;
    }

    void OnDestroy() {
        mAttach = null;
    }

    void Awake() {
        mCamCtrl = CameraController.instance;
    }

    void Start() {
        if(mAttach == null && !string.IsNullOrEmpty(attachTag)) {
            GameObject go = GameObject.FindGameObjectWithTag(attachTag);
            if(go) mAttach = go.transform;
        }
    }

    void OnDrawGizmos() {
        if(bounds.size.x > 0 && bounds.size.y > 0 && bounds.size.z > 0) {
            Color clr = boundColor;

            Gizmos.color = clr;
            Gizmos.DrawWireCube(transform.position + bounds.center, bounds.size);

            clr.a = 0.2f;
            Gizmos.color = clr;
            Gizmos.DrawCube(transform.position + bounds.center, new Vector3(0.3f, 0.3f, bounds.size.z));
        }
    }
}
