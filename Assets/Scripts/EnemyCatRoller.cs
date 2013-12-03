using UnityEngine;
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

    private GameObject[] mPlayers;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                CancelInvoke(activeFunc);
                //CancelInvoke(fireStartFunc);
                mFiring = false;

                Blink(0);
                if(projInactiveInvul) stats.isInvul = false;
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
                    if(mPlayers == null)
                        mPlayers = GameObject.FindGameObjectsWithTag("Player");

                    InvokeRepeating(activeFunc, 0, projActiveCheckDelay);

                    if(projInactiveInvul) stats.isInvul = true;
                    if(!string.IsNullOrEmpty(projInactiveClip))
                        bodySpriteCtrl.PlayOverrideClip(projInactiveClip);
                }
                    //Invoke(fireStartFunc, projStartDelay);
                break;

            case EntityState.Stun:
                mRockCtrl.moveSide = 0.0f;
                break;

            case EntityState.Dead:
                if(mRock && mRock.isAlive) {
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

                    if(mRock.stats)
                        mRock.stats.curHP = 0;
                    else
                        mRock.state = (int)Projectile.State.Dying;

                    mRock = null;
                }

                if(mSensor) {
                    mSensor.Activate(false);
                }

                bodySpriteCtrl.controller = null;
                break;

            case EntityState.RespawnWait:
                if(mRock && !mRock.isReleased) {
                    mRock.Release();
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

    void FixedUpdate() {
        bool updatePos = false;

        switch((EntityState)state) {
            case EntityState.Hurt:
            case EntityState.Normal:
                if(mSensor && mRockCtrl)
                    mSensor.hFlip = mRockCtrl.moveSide < 0.0f;

                if(mRock.state == (int)Projectile.State.Dying) {
                    mRock = null;
                    state = (int)EntityState.Dead;
                }
                else {
                    if(mRockCtrl.isGrounded) {
                        if(mRockCtrl.moveSide == 0.0f)
                            mRockCtrl.moveSide = defaultMoveSide;
                    }
                }

                if(mFiring) {
                    if(Time.fixedTime - mLastFireTime > projFireDelay) {
                        mLastFireTime = Time.fixedTime;
                        
                        Vector3 pt = projPt ? projPt.position : collider.bounds.center; pt.z = 0.0f;
                        
                        Vector3 dir = bodySpriteCtrl.isLeft ? Vector3.left : Vector3.right;
                        dir = Quaternion.AngleAxis(Random.Range(-projAngleRand, projAngleRand), Vector3.forward) * dir;
                        
                        Projectile.Create(projGroup, projType, pt, dir, null);
                        
                        mCurNumFire++;
                        if(mCurNumFire == projCount) {
                            mFiring = false;
                            //Invoke(fireStartFunc, projStartDelay);
                            InvokeRepeating(activeFunc, projStartDelay, projActiveCheckDelay);
                        }
                    }
                }

                updatePos = true;
                break;

            case EntityState.Stun:
                updatePos = true;
                break;
        }

        if(updatePos && mRock) {
            Vector3 rockPos = mRock.collider.bounds.center; rockPos.z = 0.0f;
            rockPos.y -= rockYOfs;
            rigidbody.MovePosition(rockPos);
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
                        mRockCtrl.rigidbody.velocity = Vector3.zero;
                        mRockCtrl.moveSide *= -1.0f;
                        //Debug.Log("move side: " + mRockCtrl.moveSide);
                    }
                }
                break;
        }
    }

    void FireActiveCheck() {
        Transform mTarget = null;
        Vector3 pos = transform.position;
        float nearestSqr = Mathf.Infinity;
        for(int i = 0, max = mPlayers.Length; i < max; i++) {
            if(mPlayers[i].activeSelf) {
                Vector3 dpos = mPlayers[i].transform.position - pos;
                float distSqr = dpos.sqrMagnitude;
                if(distSqr < nearestSqr) {
                    nearestSqr = distSqr;
                    mTarget = mPlayers[i].transform;
                }
            }
        }
        
        if(mTarget != null && nearestSqr < projActiveRange*projActiveRange) {
            Blink(0);
            if(projInactiveInvul) stats.isInvul = false;
            bodySpriteCtrl.StopOverrideClip();

            CancelInvoke(activeFunc);
            FireStart();
        }
        else {
            if(projInactiveInvul) stats.isInvul = true;
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
