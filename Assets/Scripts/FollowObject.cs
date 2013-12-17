using UnityEngine;
using System.Collections;

public class FollowObject : MonoBehaviour {
    public Transform target;
    public float delay;
    public float velOfs; //offset based on target's velocity's dir.
    public float minVel;
    public Vector3 ofs;

    public bool snapOnEnable;

    private bool mStarted;
    private Vector3 mCurVel;
    private Vector3 mLastVelOfs;

    void OnEnable() {
        mCurVel = Vector3.zero;
        mLastVelOfs = Vector3.zero;

        if(mStarted && snapOnEnable) {
            transform.position = target.position;
        }
    }

    void Awake() {
    }

	// Use this for initialization
	void Start() {
        mStarted = true;
        OnEnable();
	}
	
	// Update is called once per frame
	void Update() {
        if(target) {
            Vector3 dest = target.position;

            Rigidbody body = target.rigidbody;
            if(body && velOfs != 0.0f) {
                float mag = body.velocity.magnitude;
                if(mag > minVel) {
                    Vector3 dir = body.velocity/mag;
                    mLastVelOfs = dir*velOfs;
                }

                dest += mLastVelOfs;
            }

            dest += target.rotation*ofs;

            Vector3 pos = transform.position;
            transform.position = Vector3.SmoothDamp(pos, dest, ref mCurVel, delay, Mathf.Infinity, Time.deltaTime);
        }
	}

    void ChangeTarget(GameObject go) {
        if(go) {
            target = go.transform;
            mCurVel = Vector3.zero;
            mLastVelOfs = Vector3.zero;
        }
        else {
            target = null;
        }
    }
}
