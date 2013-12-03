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

    private static CameraField mLastField;
    private CameraController mCamCtrl;

    void OnTriggerEnter(Collider col) {
        if(mLastField != this) {
            mCamCtrl.mode = mode;

            Bounds setBounds = bounds;
            setBounds.center += transform.position;

            mCamCtrl.bounds = setBounds;

            Transform attachTo = null;

            if(!string.IsNullOrEmpty(attachTag)) {
                GameObject go = GameObject.FindGameObjectWithTag(attachTag);
                if(go) {
                    attachTo = go.transform;
                }
            }

            mCamCtrl.attach = attachTo;

            mCamCtrl.SetTransition(doTransition);

            mLastField = this;
        }
    }

    void OnDestroy() {
        if(mLastField == this)
            mLastField = null;
    }

    void Awake() {
        mCamCtrl = CameraController.instance;
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
