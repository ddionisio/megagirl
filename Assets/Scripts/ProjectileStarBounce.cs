using UnityEngine;
using System.Collections;

public class ProjectileStarBounce : Projectile {

    public Vector3 attachOfs;

    private Rigidbody mAttachBody;
    private FixedJoint mAttachBodyJoint;

    public Rigidbody attachBody {
        get { return mAttachBody; }
        set {
            if(mAttachBody != value) {
                if(mAttachBodyJoint) {
                    Destroy(mAttachBodyJoint);
                    mAttachBodyJoint = null;
                }

                if(value) {
                    mAttachBody = value;
                    mAttachBody.transform.position = transform.localToWorldMatrix.MultiplyPoint(attachOfs);
                    mAttachBodyJoint = mAttachBody.gameObject.AddComponent<FixedJoint>();
                    mAttachBodyJoint.connectedBody = GetComponent<Rigidbody>();
                }
                else
                    mAttachBody = null;
            }
        }
    }

    public Vector3 attachWorldPos {
        get { return transform.localToWorldMatrix.MultiplyPoint(attachOfs); }
    }

    void OnDrawGizmosSelected() {
        Color clr = Color.yellow; clr.a = 0.5f;
        Gizmos.color = clr;

        Gizmos.DrawWireSphere(transform.localToWorldMatrix.MultiplyPoint(attachOfs), 0.25f);
    }

    public override void Release() {
        attachBody = null;
        base.Release();
    }
}
