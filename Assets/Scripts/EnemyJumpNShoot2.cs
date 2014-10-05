using UnityEngine;
using System.Collections;

/// <summary>
/// Shoot in mid-air based on shootVelYMin
/// </summary>
public class EnemyJumpNShoot2 : Enemy {
    public string projType = projCommonType;

    public AnimatorData shootAnim;
    public Transform shootPt;
    public int shootCount = 3;
    public float shootAngle = 90.0f;
    public int shootMaxProj = 3;

    public float jumpRepeatDelay = 1.0f;
    public bool jumpTowardsPlayer = true;

    public float shootVelYMax = -0.1f; //the y velocity criteria when shooting, must be in air

    public SoundPlayer shootSfx;

    private GameObject[] mPlayers;

    private bool mJump;
    private float mLastJumpTime;
    private bool mProjIsShot;
    private int mCurProjCount;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                Jump(0);
                break;
        }
        
        base.StateChanged();
        
        switch((EntityState)state) {
            case EntityState.Normal:
                bodyCtrl.moveEnabled = true;
                bodyCtrl.moveSide = 0.0f;
                mJump = false;
                mLastJumpTime = 0;
                mProjIsShot = false;
                break;
        }
    }


    protected override void Awake() {
        base.Awake();

        mPlayers = GameObject.FindGameObjectsWithTag("Player");

        bodyCtrl.landCallback += OnLanded;
    }

    Transform NearestPlayer(out float nearDistSqr) {
        nearDistSqr = Mathf.Infinity;
        Transform nearT = null;

        Vector3 p = collider.bounds.center;
        for(int i = 0, max = mPlayers.Length; i < max; i++) {
            if(mPlayers[i] && mPlayers[i].activeSelf) {
                Transform t = mPlayers[i].transform;
                float distSqr = (t.position - p).sqrMagnitude;
                if(distSqr < nearDistSqr) {
                    nearT = t;
                    nearDistSqr = distSqr;
                }
            }
        }

        return nearT;
    }

    void FixedUpdate() {
        switch((EntityState)state) {
            case EntityState.Normal:
                float distSqr;
                Transform nearest = NearestPlayer(out distSqr);

                if(bodyCtrl.isGrounded) {
                    bodySpriteCtrl.isLeft = Mathf.Sign(nearest.position.x - transform.position.x) < 0.0f;

                    if(!mJump && !bodyCtrl.isJump) {
                        if(Time.fixedTime - mLastJumpTime > jumpRepeatDelay) {
                            Jump(0);
                            Jump(2.0f);
                            mJump = true;

                            if(jumpTowardsPlayer) {
                                bodyCtrl.moveSide = bodySpriteCtrl.isLeft ? -1.0f : 1.0f;
                            }
                        }
                    }
                }
                else {
                    if(nearest && nearest.position.y < transform.position.y && Time.fixedTime - bodyCtrl.jumpLastTime > 0.15f) {
                        Jump(0);
                    }
                }

                if(nearest) {
                    if(!mProjIsShot && mCurProjCount < shootMaxProj) {
                        if(!bodyCtrl.isGrounded && bodyCtrl.localVelocity.y <= shootVelYMax) {
                            Vector3 pos = shootPt.position; pos.z = 0.0f;

                            Vector3 dir = new Vector3(bodySpriteCtrl.isLeft ? -1.0f : 1.0f, 0.0f, 0.0f);

                            if(shootCount > 1) {
                                Quaternion rot = Quaternion.AngleAxis(shootAngle/((float)(shootCount-1)), bodySpriteCtrl.isLeft ? Vector3.back : Vector3.forward);
                                dir = Quaternion.AngleAxis(shootAngle*0.5f, bodySpriteCtrl.isLeft ? Vector3.forward : Vector3.back)*dir;
                                
                                for(int i = 0; i < shootCount; i++) {
                                    mCurProjCount++;
                                    Projectile proj = Projectile.Create(projGroup, projType, pos, dir, null);
                                    proj.releaseCallback += OnProjRelease;

                                    dir = rot*dir;
                                }
                            }
                            else {
                                mCurProjCount++;
                                Projectile proj = Projectile.Create(projGroup, projType, pos, dir, null);
                                proj.releaseCallback += OnProjRelease;
                            }

                            if(shootAnim)
                                shootAnim.Play("shoot");

                            if(shootSfx)
                                shootSfx.Play();

                            mProjIsShot = true;
                        }
                    }

                    //bodySpriteCtrl.isLeft = Mathf.Sign(nearest.position.x - transform.position.x) < 0.0f;
                }
                break;
        }
    }

    void OnLanded(PlatformerController ctrl) {
        bodyCtrl.moveSide = 0.0f;
        mJump = false;
        mLastJumpTime = Time.fixedTime;
        mProjIsShot = false;
    }

    void OnProjRelease(EntityBase ent) {
        ent.releaseCallback -= OnProjRelease;
        mCurProjCount--;
        if(mCurProjCount < 0)
            mCurProjCount = 0;
    }
}
