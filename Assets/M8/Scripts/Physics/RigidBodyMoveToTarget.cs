using UnityEngine;
using System.Collections;

/// <summary>
/// Make sure this is on an object with a rigidbody!
/// </summary>
[ExecuteInEditMode]
[AddComponentMenu("M8/Physics/RigidBodyMoveToTarget")]
public class RigidBodyMoveToTarget : MonoBehaviour {
    public Transform target;
    public Collider thisCollider;
    public Vector3 offset;

    public bool ignoreRotation = false;
    public Vector3 rotOfs;

    private Quaternion mRotQ;

    private Rigidbody mBody;
    private Vector3 mCenter;

#if UNITY_EDITOR
    // Update is called once per frame
    void Update() {
        if(!Application.isPlaying && target != null) {
            Collider col = thisCollider != null ? thisCollider : GetComponent<Collider>();
            if(col != null) {
                Vector3 ofs = transform.worldToLocalMatrix.MultiplyPoint(col.bounds.center);

                transform.position = target.localToWorldMatrix.MultiplyPoint(offset - ofs);
            }
            else {
                transform.position = target.position + target.rotation*offset;
            }

            if(!ignoreRotation)
                transform.rotation = Quaternion.Euler(rotOfs) * target.rotation;
        }
    }
#endif

    void FixedUpdate() {
        if(target) {
            Vector3 newPos;

            if(thisCollider != null) {
                newPos = target.localToWorldMatrix.MultiplyPoint3x4(offset - mCenter);
            }
            else
                newPos = target.position + target.rotation * offset;

            if(transform.position != newPos)
                mBody.MovePosition(newPos);

            if(!ignoreRotation)
                mBody.MoveRotation(mRotQ * target.rotation);
        }
    }

    void Awake() {
        if(thisCollider == null)
            thisCollider = GetComponent<Collider>();

        mBody = GetComponent<Rigidbody>();

        mRotQ = Quaternion.Euler(rotOfs);
        mCenter = transform.worldToLocalMatrix.MultiplyPoint3x4(thisCollider.bounds.center);
    }
}
