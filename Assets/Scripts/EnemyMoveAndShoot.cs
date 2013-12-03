using UnityEngine;
using System.Collections;

public class EnemyMoveAndShoot : Enemy {
    public Transform projPt;
    public string projType = projCommonType;
    public float projAngleRand = 15.0f;
    public int projCount = 2;
    public float projFireDelay = 0.5f;
    public float projStartDelay = 1.0f;

    public bool ignoreFallDetect = true;

    private const string fireStartFunc = "FireStart";

    private EntitySensor mSensor;
    private bool mFiring;
    private int mCurNumFire;
    private float mLastFireTime;

    protected override void StateChanged() {
        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                CancelInvoke(fireStartFunc);
                bodySpriteCtrl.StopOverrideClip();
                mFiring = false;

                float side = Mathf.Sign(Player.instance.collider.bounds.center.x - collider.bounds.center.x);
                bodyCtrl.moveSide = side;
                
                if(mSensor) {
                    mSensor.Activate(true);
                }

                Invoke(fireStartFunc, projStartDelay);
                break;

            case EntityState.Stun:
                bodyCtrl.moveSide = 0.0f;
                break;
                
            case EntityState.Dead:
                if(mSensor) {
                    mSensor.Activate(false);
                }
                break;
                
            case EntityState.RespawnWait:
                if(mSensor) {
                    mSensor.Activate(false);
                }

                RevertTransform();
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        bodySpriteCtrl.clipFinishCallback += OnSpriteAnimEnd;

        mSensor = GetComponent<EntitySensor>();
        if(mSensor)
            mSensor.updateCallback += OnSensorUpdate;
    }

    void FixedUpdate() {
        switch((EntityState)state) {
            case EntityState.Hurt:
            case EntityState.Normal:
                if(mSensor)
                    mSensor.hFlip = bodySpriteCtrl.isLeft;

                if(mFiring) {
                    if(Time.fixedTime - mLastFireTime > projFireDelay) {
                        mLastFireTime = Time.fixedTime;

                        Vector3 pt = projPt.position; pt.z = 0.0f;

                        Vector3 dir = bodySpriteCtrl.isLeft ? Vector3.left : Vector3.right;
                        dir = Quaternion.AngleAxis(Random.Range(-projAngleRand, projAngleRand), Vector3.forward) * dir;

                        Projectile.Create(projGroup, projType, pt, dir, null);

                        mCurNumFire++;
                        if(mCurNumFire == projCount) {
                            mFiring = false;
                            bodySpriteCtrl.StopOverrideClip();
                            Invoke(fireStartFunc, projStartDelay);
                        }
                    }
                }
                else if(bodyCtrl.isGrounded) {
                    if(bodyCtrl.moveSide == 0.0f) {
                        float side = Mathf.Sign(Player.instance.collider.bounds.center.x - collider.bounds.center.x);
                        bodyCtrl.moveSide = side;
                    }
                }
                else
                    bodyCtrl.moveSide = 0.0f;
                break;
        }
    }

    void OnSensorUpdate(EntitySensor sensor) {
        switch((EntityState)state) {
            case EntityState.Normal:
            case EntityState.Hurt:
                if(sensor.isHit) {
                    if(Vector3.Angle(bodyCtrl.moveDir, sensor.hit.normal) >= 170.0f) {
                        bodyCtrl.moveSide *= -1.0f;
                    }
                }
                else if(!ignoreFallDetect) {
                    if(bodyCtrl.isGrounded) {
                        bodyCtrl.rigidbody.velocity = Vector3.zero;
                        bodyCtrl.moveSide *= -1.0f;
                    }
                }
                break;
        }
    }

    void OnSpriteAnimEnd(PlatformerSpriteController ctrl, tk2dSpriteAnimationClip clip) {
        if(clip.name == "fireStart") {
            bodySpriteCtrl.PlayOverrideClip("firing");
            mLastFireTime = 0.0f;
            mCurNumFire = 0;
            mFiring = true;
            bodyCtrl.moveSide = 0.0f;
        }
    }

    void FireStart() {
        float side = Mathf.Sign(Player.instance.collider.bounds.center.x - collider.bounds.center.x);
        bodyCtrl.moveSide = side;

        bodySpriteCtrl.PlayOverrideClip("fireStart");
        bodySpriteCtrl.isLeft = side < 0.0f;
    }
}
