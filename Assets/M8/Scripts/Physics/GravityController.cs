using UnityEngine;
using System.Collections;

[AddComponentMenu("M8/Physics/GravityController")]
public class GravityController : MonoBehaviour {
    public Vector3 startUp = Vector3.up; //initial up vector, this is to orient the object's up to match this, if zero, init with transform's up

    public bool orientUp = true; //allow orientation of the up vector

    public float gravity = -9.8f; //the gravity accel, -9.8 m/s^2 as default

    public float orientationSpeed = 90.0f;

    protected bool mIsOrienting;
    protected Quaternion mRotateTo;
    protected WaitForFixedUpdate mWaitUpdate = new WaitForFixedUpdate();

    private bool mGravityLocked = false;
    private Vector3 mUp;
    private float mStartGravity;
    private bool mStarted;
    private float mMoveScale = 1.0f; //NOTE: reset during disable

    private const int mMaxGravityFields = 4;
    private GravityFieldBase[] mGravityFields = new GravityFieldBase[mMaxGravityFields];
    private int mGravityFieldCurCount;

    public bool gravityLocked { get { return mGravityLocked; } set { mGravityLocked = value; } }
    public float startGravity { get { return mStartGravity; } }

    public float moveScale { get { return mMoveScale; } set { mMoveScale = value; } }

    public Vector3 up {
        get { return mUp; }
        set {
            if(mUp != value) {
                mUp = value;

                ApplyUp();
            }
        }
    }

    void OnEnable() {
        if(mStarted) {
            Init();
        }
    }

    void OnDisable() {
        if(mIsOrienting) {
            mIsOrienting = false;
            transform.up = mUp;
        }

        mGravityFieldCurCount = 0;

        gravity = mStartGravity;
        mMoveScale = 1.0f;
    }

    void OnTriggerEnter(Collider col) {
        if(mGravityFieldCurCount < mMaxGravityFields) {
            GravityFieldBase gravField = col.GetComponent<GravityFieldBase>();
            if(gravField) {
                int ind = System.Array.IndexOf(mGravityFields, gravField, 0, mGravityFieldCurCount);
                if(ind == -1) {
                    mGravityFields[mGravityFieldCurCount] = gravField;
                    mGravityFieldCurCount++;
                }
            }
        }
    }
    
    void OnTriggerExit(Collider col) {
        GravityFieldBase gravField = col.GetComponent<GravityFieldBase>();
        if(gravField) {
            int ind = System.Array.IndexOf(mGravityFields, gravField, 0, mGravityFieldCurCount);
            if(ind != -1) {
                gravField.ItemRemoved(this);

                if(mGravityFieldCurCount > 1) {
                    mGravityFields[ind] = mGravityFields[mGravityFieldCurCount - 1];
                }
                else { //restore
                    up = startUp;
                    gravity = mStartGravity;
                }

                mGravityFieldCurCount--;
            }
        }
    }
    
    protected virtual void Awake() {
        GetComponent<Rigidbody>().useGravity = false;

        if(startUp == Vector3.zero)
            startUp = transform.up;

        mStartGravity = gravity;
    }

    // Use this for initialization
    protected virtual void Start() {
        mStarted = true;
        Init();
    }

    // Update is called once per frame
    protected virtual void FixedUpdate() {
        if(mGravityFieldCurCount > 0) {
            bool fallLimit = false;
            float fallSpeedLimit = Mathf.Infinity;

            Vector3 newUp = Vector3.zero;

            float newGravity = 0.0f;

            for(int i = 0; i < mGravityFieldCurCount; i++) {
                GravityFieldBase gf = mGravityFields[i];
                if(gf && gf.gameObject.activeSelf && gf.enabled) {
                    newUp += gf.GetUpVector(this);
                    if(gf.gravityOverride)
                        newGravity += gf.gravity;

                    if(gf.fallLimit) {
                        fallLimit = true;
                        if(gf.fallSpeedLimit < fallSpeedLimit)
                            fallSpeedLimit = gf.fallSpeedLimit;
                    }
                }
                else { //not active, remove it
                    if(mGravityFieldCurCount > 1) {
                        mGravityFields[i] = mGravityFields[mGravityFieldCurCount - 1];
                    }

                    mGravityFieldCurCount--;
                    i--;
                }
            }

            if(mGravityFieldCurCount > 0) { //in case all were inactive
                newUp /= ((float)mGravityFieldCurCount); newUp.Normalize();

                up = newUp;

                gravity = newGravity;

                if(fallLimit) {
                    //assume y-axis, positive up
                    if(GetComponent<Rigidbody>() && !GetComponent<Rigidbody>().isKinematic) {
                        Vector3 localVel = transform.worldToLocalMatrix.MultiplyVector(GetComponent<Rigidbody>().velocity);
                        if(localVel.y < -fallSpeedLimit) {
                            localVel.y = -fallSpeedLimit;
                            GetComponent<Rigidbody>().velocity = transform.localToWorldMatrix.MultiplyVector(localVel);
                        }
                    }
                }
            }
            else { //restore
                up = startUp;
                gravity = mStartGravity;
            }
        }

        if(!mGravityLocked)
            GetComponent<Rigidbody>().AddForce(mUp * gravity * GetComponent<Rigidbody>().mass * mMoveScale, ForceMode.Force);
    }

    protected virtual void ApplyUp() {
        if(orientUp) {
            //TODO: figure out better math
            if(M8.MathUtil.RotateToUp(mUp, -transform.right, transform.forward, ref mRotateTo)) {
                if(!mIsOrienting)
                    StartCoroutine(OrientUp());
            }
        }
    }

    void Init() {
        if(GravityFieldBase.global != null) {
            mGravityFields[mGravityFieldCurCount] = GravityFieldBase.global;
            mGravityFieldCurCount++;
        }

        up = startUp;
    }

    protected IEnumerator OrientUp() {
        mIsOrienting = true;

        while(transform.up != mUp) {
            float step = orientationSpeed * Time.fixedDeltaTime;
            GetComponent<Rigidbody>().MoveRotation(Quaternion.RotateTowards(transform.rotation, mRotateTo, step));

            yield return mWaitUpdate;
        }

        mIsOrienting = false;
    }
}
