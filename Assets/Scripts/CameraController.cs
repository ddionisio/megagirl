﻿using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {
    public enum Mode {
        Lock, //stop camera motion by controller
        Free, //camera follows attach, restricted by bounds
        HorizontalLock, //camera X is forced at center of bounds, Y still follows attach
        VerticalLock, //camera Y is forced at center of bounds, X still follows attach
    }

    public Mode mode = Mode.Lock;
    public float delay = 0.1f; //reposition delay
    public bool transitionSnap; //if true, snap position within the bounds of a field right away with no transition
    public float transitionDelay = 0.5f;
    public float transitionExpire = 1.0f;
    
    private static CameraController mInstance;

    private tk2dCamera mCam;
    private Transform mAttach;
    private Vector3 mCurVel;
    private Bounds mBounds;
    private bool mDoTrans;
    private float mLastTransTime;
    private float mCurDelay;
    private CameraField mCurField;

    public static CameraController instance { get { return mInstance; } }

    public Transform attach {
        get { return mAttach; }
        set {
            if(mAttach != value) {
                mAttach = value;
                mCurVel = Vector3.zero;
            }
        }
    }

    public CameraField field {
        get { return mCurField; }
        set {
            if(mCurField != value) {
                mCurField = value;
                if(mCurField) {
                    mode = mCurField.mode;

                    Bounds setBounds = mCurField.bounds;
                    setBounds.center += mCurField.transform.position;

                    bounds = setBounds;

                    if(transitionSnap)
                        SnapPosition();
                    else
                        SetTransition(mCurField.doTransition);
                }
            }
        }
    }

    public Bounds bounds {
        get { return mBounds; }
        set {
            mBounds = value;
            mCurVel = Vector3.zero;
        }
    }

    public tk2dCamera tk2dCam { get { return mCam; } }

    public void SetTransition(bool transition) {
        mDoTrans = transition;
        mLastTransTime = Time.fixedTime;
        mCurVel = Vector3.zero;
        mCurDelay = transitionDelay;
    }

    public void SnapPosition() {
        Vector3 dest = mAttach ? mAttach.GetComponent<Collider>() ? mAttach.GetComponent<Collider>().bounds.center : mAttach.position : transform.position;

        //apply bounds
        ApplyBounds(ref dest);

        if(GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().MovePosition(dest);
        else
            transform.position = dest;

        mDoTrans = false;
        mCurDelay = delay;
    }

    void Awake() {
        if(mInstance == null) {
            mInstance = this;

            //init stuff
            mCam = GetComponentInChildren<tk2dCamera>();

            mCurDelay = delay;
        }
        else {
            DestroyImmediate(gameObject);
        }
    }

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void FixedUpdate() {
        if(mode == Mode.Lock)
            return;

        if(mDoTrans) {
            float curT = Time.fixedTime - mLastTransTime;
            if(curT >= transitionExpire) {
                mDoTrans = false;
                mCurDelay = delay;
            }
            else {
                float t = Mathf.Clamp(curT/transitionExpire, 0.0f, 1.0f);
                mCurDelay = Mathf.Lerp(transitionDelay, delay, t);
            }
        }

        Vector3 curPos = transform.position;
        Vector3 dest = mAttach ? mAttach.GetComponent<Collider>() ? mAttach.GetComponent<Collider>().bounds.center : mAttach.position : curPos;

        //apply bounds
        ApplyBounds(ref dest);

        if(curPos != dest) {
            if(GetComponent<Rigidbody>()) {
                GetComponent<Rigidbody>().MovePosition(Vector3.SmoothDamp(curPos, dest, ref mCurVel, mCurDelay, Mathf.Infinity, Time.fixedDeltaTime));
            }
            else {
                transform.position = Vector3.SmoothDamp(curPos, dest, ref mCurVel, mCurDelay, Mathf.Infinity, Time.fixedDeltaTime);
            }
        }
    }

    void ApplyBounds(ref Vector3 pos) {
        if(bounds.size.x > 0.0f && bounds.size.y > 0.0f) {
            Rect screen = mCam.ScreenExtents;

            if(pos.x - screen.width * 0.5f < bounds.min.x)
                pos.x = bounds.min.x + screen.width * 0.5f;
            else if(pos.x + screen.width * 0.5f > bounds.max.x)
                pos.x = bounds.max.x - screen.width * 0.5f;

            if(pos.y - screen.height * 0.5f < bounds.min.y)
                pos.y = bounds.min.y + screen.height * 0.5f;
            else if(pos.y + screen.height * 0.5f > bounds.max.y)
                pos.y = bounds.max.y - screen.height * 0.5f;
        }

        switch(mode) {
            case Mode.HorizontalLock:
                pos.x = bounds.center.x;
                break;

            case Mode.VerticalLock:
                pos.y = bounds.center.y;
                break;
        }
    }
}
