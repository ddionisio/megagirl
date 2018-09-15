using UnityEngine;
using System.Collections;

[AddComponentMenu("M8/Transform/AnimSpinner")]
public class TransAnimSpinner : MonoBehaviour {
    public Vector3 rotatePerSecond;
    public bool local = true;
    public bool forceFixedUpdate;

    private Vector3 mEulerAnglesOrig;
    private float mSpeedScale = 1.0f;

    private Rigidbody mBody;

    public float speedScale {
        get { return mSpeedScale; }
        set {
            mSpeedScale = value;
        }
    }

    void OnEnable() {
        mEulerAnglesOrig = transform.eulerAngles;
    }

    void OnDisable() {
        mSpeedScale = 1.0f;
    }

    // Use this for initialization
    void Awake() {
        mBody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update() {
        if(!forceFixedUpdate && mBody == null) {
            if(local) {
                transform.localEulerAngles = transform.localEulerAngles + (rotatePerSecond * mSpeedScale * Time.deltaTime);
            }
            else {
                mEulerAnglesOrig += rotatePerSecond * mSpeedScale * Time.deltaTime;
                transform.eulerAngles = mEulerAnglesOrig;
            }
        }
    }

    void FixedUpdate() {
        if(mBody != null) {
            if(local) {
                Vector3 eulers = transform.eulerAngles;
                mBody.MoveRotation(Quaternion.Euler(eulers + (rotatePerSecond * mSpeedScale * Time.fixedDeltaTime)));
            }
            else {
                mEulerAnglesOrig += rotatePerSecond * mSpeedScale * Time.fixedDeltaTime;
                mBody.MoveRotation(Quaternion.Euler(mEulerAnglesOrig));
            }
        }
        else if(forceFixedUpdate) {
            if(local) {
                transform.localEulerAngles = transform.localEulerAngles + (rotatePerSecond * mSpeedScale * Time.fixedDeltaTime);
            }
            else {
                mEulerAnglesOrig += rotatePerSecond * mSpeedScale * Time.fixedDeltaTime;
                transform.eulerAngles = mEulerAnglesOrig;
            }
        }
    }
}
