﻿using UnityEngine;
using System.Collections;

public class EnemyCatRoller : Enemy {
    public string rockProjType = "rockBombRoller";

    public float rockYOfs;

    public float defaultMoveSide = -1.0f;

    public bool ignoreFallDetect = false;

    public string projType;// = projCommonType; //leave empty for no shooting
    public Transform projPt;
    public float projAngleRand = 15.0f;
    public int projCount = 2;
    public float projFireDelay = 0.5f;
    public float projStartDelay = 1.0f;
    public float projActiveRange;
    public float projActiveCheckDelay = 0.2f;
    public string projInactiveClip = "idle";
    public bool projInactiveInvul = true;
    public bool projFacePlayer = true;
    public SoundPlayer projSfx;

    public bool rollerDieDelayOverride = false; //override roller death delay when we die, sets it 0 while we are alive
    public float rollerDieDelay = 1.0f;
    public float rollerDieBlinkDelay = 1.0f;

    //private const string fireStartFunc = "FireStart";
    private const string activeFunc = "FireActiveCheck";

    private bool mFiring;
    private int mCurNumFire;
    private float mLastFireTime;

    private EntitySensor mSensor;
    private Projectile mRock;
    private PlatformerController mRockCtrl;
    private EntityBlinkDelay mRockBlinkDelay;

    private Player mPlayer;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                CancelInvoke(activeFunc);
                //CancelInvoke(fireStartFunc);
                mFiring = false;

                Blink(0);
                if(projInactiveInvul) stats.damageReduction = 1.0f;
                bodySpriteCtrl.StopOverrideClip();
                break;
        }

        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                CancelInvoke(activeFunc);
                //CancelInvoke(fireStartFunc);
                mFiring = false;

                if(!mRock) {
                    Vector3 rockPos = transform.position;
                    rockPos.y += rockYOfs;
                    mRock = Projectile.Create(projGroup, rockProjType, rockPos, Vector3.zero, null);

                    mRockCtrl = mRock.GetComponent<PlatformerController>();
                    mRockBlinkDelay = mRock.GetComponent<EntityBlinkDelay>();

                    bodySpriteCtrl.controller = mRockCtrl;

                    if(rollerDieDelayOverride) {
                        mRock.dieBlink = false;
                        mRock.dieDelay = 0.0f;
                    }
                }

                mRockCtrl.dirHolder = transform;
                mRockCtrl.moveSideLock = true;
                mRockCtrl.moveSide = defaultMoveSide;

                if(mRockBlinkDelay)
                    mRockBlinkDelay.enabled = false;

                if(mSensor) {
                    mSensor.Activate(true);
                }

                if(!string.IsNullOrEmpty(projType)) {
                    mPlayer = Player.instance;

                    InvokeRepeating(activeFunc, 0, projActiveCheckDelay);

                    if(projInactiveInvul) stats.damageReduction = 1.0f;
                    if(!string.IsNullOrEmpty(projInactiveClip))
                        bodySpriteCtrl.PlayOverrideClip(projInactiveClip);
                }
                    //Invoke(fireStartFunc, projStartDelay);
                break;

            case EntityState.Stun:
                mRockCtrl.moveSide = 0.0f;
                break;

            case EntityState.Dead:
                if(mRock) {
                    if(rollerDieDelayOverride) {
                        if(mRockBlinkDelay) {
                            mRockBlinkDelay.delay = rollerDieDelay - rollerDieBlinkDelay;
                            mRockBlinkDelay.enabled = true;
                        }
                        else {
                            mRock.dieBlink = true;
                        }

                        mRock.dieDelay = rollerDieDelay;
                    }

                    mRock.state = (int)Projectile.State.Dying;
                
                    mRock = null;
                    mRockCtrl = null;
                    mRockBlinkDelay = null;
                }

                if(mSensor) {
                    mSensor.Activate(false);
                }

                bodySpriteCtrl.controller = null;
                break;

            case EntityState.RespawnWait:
                if(mRock) {
                    if(mRock.isAlive)
                        mRock.Release();
                    //if((!mRock.isAlive || mRock.state != (int)Projectile.State.Dying) && !mRock.isReleased)
                       // mRock.Release();
                
                    mRock = null;
                    mRockCtrl = null;
                    mRockBlinkDelay = null;
                }

                if(mSensor) {
                    mSensor.Activate(false);
                }

                bodySpriteCtrl.controller = null;

                RevertTransform();
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        mSensor = GetComponent<EntitySensor>();
        if(mSensor)
            mSensor.updateCallback += OnSensorUpdate;
    }

    protected override void OnSuddenDeath() {
        if(mRock) {
            mRock.Release();
            mRock = null;
            mRockCtrl = null;
            mRockBlinkDelay = null;
        }

        base.OnSuddenDeath();
    }

    void FixedUpdate() {
        bool updatePos = false;

        switch((EntityState)state) {
            case EntityState.Hurt:
            case EntityState.Normal:
                if(mSensor && mRockCtrl)
                    mSensor.hFlip = mRockCtrl.moveSide < 0.0f;

                if(mRock == null || mRock.state == (int)Projectile.State.Dying) {
                    mRock = null;
                    stats.curHP = 0;
                }
                else {
                    if(mRockCtrl.isGrounded) {
                        if(mRockCtrl.moveSide == 0.0f)
                            mRockCtrl.moveSide = defaultMoveSide;
                    }

	                if(mFiring) {
                        if(projFacePlayer) {
                            bodySpriteCtrl.lockFacing = true;
                            bodySpriteCtrl.isLeft = Mathf.Sign(mPlayer.transform.position.x - transform.position.x) < 0.0f;
                        }

	                    if(Time.fixedTime - mLastFireTime > projFireDelay) {
	                        mLastFireTime = Time.fixedTime;
	                        
	                        Vector3 pt = projPt ? projPt.position : GetComponent<Collider>().bounds.center; pt.z = 0.0f;
	                        
	                        Vector3 dir = bodySpriteCtrl.isLeft ? Vector3.left : Vector3.right;
	                        dir = Quaternion.AngleAxis(Random.Range(-projAngleRand, projAngleRand), Vector3.forward) * dir;
	                        
	                        Projectile.Create(projGroup, projType, pt, dir, null);

                            if(projSfx)
                                projSfx.Play();
	                        
	                        mCurNumFire++;
	                        if(mCurNumFire == projCount) {
                                bodySpriteCtrl.lockFacing = false;
	                            mFiring = false;
	                            //Invoke(fireStartFunc, projStartDelay);
	                            InvokeRepeating(activeFunc, projStartDelay, projActiveCheckDelay);
	                        }
	                    }
	                }

	                updatePos = true;
				}
                break;

            case EntityState.Stun:
                updatePos = true;
                break;
        }

        if(updatePos && mRock) {
            Vector3 rockPos = mRock.GetComponent<Collider>().bounds.center; rockPos.z = 0.0f;
            rockPos.y -= rockYOfs;
            GetComponent<Rigidbody>().MovePosition(rockPos);
        }
    }

    void OnSensorUpdate(EntitySensor sensor) {
        switch((EntityState)state) {
            case EntityState.Normal:
            case EntityState.Hurt:
                if(sensor.isHit) {
                    if(Vector3.Angle(mRockCtrl.moveDir, sensor.hit.normal) >= 170.0f) {
                        //mRockCtrl.rigidbody.velocity = Vector3.zero;
                        mRockCtrl.moveSide *= -1.0f;
                        //Debug.Log("move side: " + mRockCtrl.moveSide);
                    }
                }
                else if(!ignoreFallDetect) {
                    if(mRockCtrl.isGrounded) {
                        mRockCtrl.GetComponent<Rigidbody>().velocity = Vector3.zero;
                        mRockCtrl.moveSide *= -1.0f;
                        //Debug.Log("move side: " + mRockCtrl.moveSide);
                    }
                }
                break;
        }
    }

    void FireActiveCheck() {
        Transform mTarget = mPlayer.transform;
        Vector3 pos = transform.position;
        float nearestSqr = (mTarget.position - pos).sqrMagnitude;

        if(nearestSqr < projActiveRange*projActiveRange) {
            Blink(0);
            if(projInactiveInvul) stats.damageReduction = 0.0f;
            bodySpriteCtrl.StopOverrideClip();

            CancelInvoke(activeFunc);
            FireStart();
        }
        else {
            if(projInactiveInvul) stats.damageReduction = 1.0f;
            if(!string.IsNullOrEmpty(projInactiveClip) && (bodySpriteCtrl.overrideClip == null || bodySpriteCtrl.overrideClip.name != projInactiveClip))
                bodySpriteCtrl.PlayOverrideClip(projInactiveClip);
        }
    }

    void FireStart() {
        mLastFireTime = 0.0f;
        mCurNumFire = 0;
        mFiring = true;
        //bodyCtrl.moveSide = 0.0f;
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        Vector3 pos = transform.position;
        pos.y += rockYOfs;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(pos, 0.1f);

        if(projActiveRange > 0 && !string.IsNullOrEmpty(projType)) {
            Gizmos.color = Color.cyan*0.5f;
            Gizmos.DrawWireSphere(transform.position, projActiveRange);
        }
    }
}
