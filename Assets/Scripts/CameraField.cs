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
    private GameObject mAttachGO;

    void OnTriggerEnter(Collider col) {
        if(mAttachGO == null)
            mAttachGO = GameObject.FindGameObjectWithTag(attachTag);

        if(mAttachGO) {
            mCamCtrl.mode = mode;
            
            Bounds setBounds = bounds;
            setBounds.center += transform.position;
            
            mCamCtrl.bounds = setBounds;
            
            mCamCtrl.attach = mAttachGO.transform;
            
            mCamCtrl.SetTransition(doTransition);
        }
    }

    void OnDestroy() {
        mAttachGO = null;
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
