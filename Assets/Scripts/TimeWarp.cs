using UnityEngine;
using System.Collections;

public class TimeWarp : MonoBehaviour {
    public Transform target; //optional, usually you want TimeWarp in a collider, then maybe set this target to the parent (for movers, etc)

    private Projectile[] mProjs;
    private RigidBodyController[] mBodyCtrls;
    private GravityController[] mGravCtrls;
    private TransAnimSpinner[] mSpinners;
    private AnimatorData[] animDats;

    private float mScale = 1.0f;

    public float scale { get { return mScale; } }

    public void SetScale(float scale) {
        if(mScale != scale) {
            mScale = scale;

            if(scale < 1.0f && target.rigidbody && !target.rigidbody.isKinematic) {
                Vector3 v = target.rigidbody.velocity;
                float mag = v.magnitude;
                if(mag > 0.0f) {
                    target.rigidbody.velocity = (v / mag) * (mag * 0.3f);
                }
            }

            for(int i = 0; i < mProjs.Length; i++)
                mProjs[i].moveScale = scale;

            for(int i = 0; i < mBodyCtrls.Length; i++)
                mBodyCtrls[i].moveScale = scale;

            for(int i = 0; i < mGravCtrls.Length; i++)
                mGravCtrls[i].moveScale = scale;

            for(int i = 0; i < mSpinners.Length; i++)
                mSpinners[i].speedScale = scale;

            for(int i = 0; i < animDats.Length; i++)
                animDats[i].animScale = scale;
        }
    }

    public void Restore() {
        SetScale(1.0f);
    }

    void OnDisable() {
        SetScale(1.0f);
    }

    void Awake() {
        if(target == null)
            target = transform;

        mProjs = target.GetComponentsInChildren<Projectile>(true);
        mBodyCtrls = target.GetComponentsInChildren<RigidBodyController>(true);
        mGravCtrls = target.GetComponentsInChildren<GravityController>(true);
        mSpinners = target.GetComponentsInChildren<TransAnimSpinner>(true);
        animDats = target.GetComponentsInChildren<AnimatorData>(true);
    }
}
