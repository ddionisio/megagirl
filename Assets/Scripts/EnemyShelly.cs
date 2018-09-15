﻿using UnityEngine;
using System.Collections;

public class EnemyShelly : Enemy {
    public tk2dSpriteAnimator cannonAnim;
    public Transform cannonSpawnPt;

    public AnimatorData shellAnimDat;

    public float orientDelay = 0.1f;
    public float orientAcquireDelay = 0.2f;

    public float openDelay;

    public string projType;

    public int projCount;
    public float projDelay;

    public SoundPlayer activeSfx;
    public SoundPlayer shootSfx;

    private const string openFunc = "Open";
    private const string orientFunc = "OrientCannon";

    private bool mIsOpen;
    private int mCurShotCount;
    private float mLastShotTime;

    private float mDirAngle;
    private float mDirAngleDest;
    private Vector3 mDirCur;
    private Vector3 mDirRotAxis;
    private float mDirAngleVel;
    private bool mIsClosed;

    void SetClosedState(bool closed) {
        if(mIsClosed != closed) {
            mIsClosed = closed;

            if(mIsClosed) {
                stats.AddDamageReduce(1.0f);
            }
            else {
                stats.AddDamageReduce(-1.0f);
            }
        }
    }

    void InitNormal() {
        mIsOpen = false;
        
        CancelInvoke(orientFunc);
        CancelInvoke(openFunc);
        
        cannonAnim.Play("normal");
        
        shellAnimDat.Play("normal");

        SetClosedState(true);

        Invoke(openFunc, openDelay);
        
        mDirCur = Vector3.up;
        mDirAngle = mDirAngleDest = 0.0f;
        mDirAngleVel = 0.0f;
    }
        
    protected override void StateChanged() {
        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                InitNormal();
                break;

            case EntityState.Dead:
            case EntityState.Invalid:
                SetClosedState(false);
                break;
        }
    }

    protected override void ActivatorWakeUp() {
        base.ActivatorWakeUp();

        switch((EntityState)state) {
            case EntityState.Normal:
                InitNormal();
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        shellAnimDat.takeCompleteCallback += OnShellAnimDone;
    }

    protected override void Start() {
        base.Start();
    }

    void Open() {
        if(activeSfx.gameObject.activeSelf)
            activeSfx.Play();

        shellAnimDat.Play("open");
    }

    void Update() {
        if((EntityState)state == EntityState.Normal && mIsOpen) {
            Vector3 dir = mDirCur;

            if(mDirAngleDest > 0.0f && mDirAngle != mDirAngleDest) {
                mDirAngle = Mathf.SmoothDampAngle(mDirAngle, mDirAngleDest, ref mDirAngleVel, orientDelay, Mathf.Infinity, Time.deltaTime);

                dir = Quaternion.AngleAxis(mDirAngle, mDirRotAxis) * dir;

                if(mDirAngle == mDirAngleDest) {
                    mDirAngle = mDirAngleDest = 0.0f;
                    mDirCur = dir;
                }
            }

            cannonAnim.transform.up = dir;

            if(mCurShotCount == projCount) {
                Blink(0);
                mIsOpen = false;
                SetClosedState(true);
                shellAnimDat.Play("close");
            }
            else {
                if(Time.time - mLastShotTime > projDelay) {
                    mLastShotTime = Time.time;

                    cannonAnim.Play("fire");
                    Vector3 pos = cannonSpawnPt.position; pos.z = 0;
                    Projectile.Create(projGroup, projType, pos, dir, null);
                    mCurShotCount++;

                    shootSfx.Play();
                }
            }
        }
    }

    void OrientCannon() {
        switch((EntityState)state) {
            case EntityState.Normal:
                Vector3 pos = cannonAnim.transform.position;
                GameObject[] gos = GameObject.FindGameObjectsWithTag("Player");
                if(gos.Length > 0) {
                    float nearestDistSqr = Mathf.Infinity;
                    Vector3 nearestDPos = Vector3.up;
                    for(int i = 0, max = gos.Length; i < max; i++) {
                        if(gos[i].activeSelf) {
                            Vector3 colPos = gos[i].GetComponent<Collider>() ? gos[i].GetComponent<Collider>().bounds.center : gos[i].transform.position;
                            Vector3 dpos = colPos - pos;
                            float distSqr = dpos.sqrMagnitude;
                            if(distSqr > 0.0f && distSqr < nearestDistSqr) {
                                nearestDPos = dpos;
                                nearestDPos.z = 0;
                            }
                        }
                    }

                    mDirAngleDest = Vector3.Angle(mDirCur, nearestDPos);
                    mDirRotAxis = Vector3.Cross(mDirCur, nearestDPos);
                    mDirAngleVel = 0.0f;
                }
                break;
        }
    }

    void OnShellAnimDone(AnimatorData anim, AMTake take) {
        if(take.name == "close") {
            shellAnimDat.Play("normal");
            Invoke(openFunc, openDelay);
        }
        else if(take.name == "open") {
            mIsOpen = true;
            SetClosedState(false);

            mLastShotTime = Time.time;
            mCurShotCount = 0;

            InvokeRepeating(orientFunc, 0, orientAcquireDelay);
        }
    }
}
