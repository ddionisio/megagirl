using UnityEngine;
using System.Collections;

public class EnemyJumpNShootSimple : Enemy {
    public string projType = projCommonType;

    public float shootRangeX = 10.0f;
    public float shootCooldown = 1.0f;
    public AnimatorData shootAnim;
    public Transform shootPt;

    public float jumpRepeatDelay = 1.0f;


    private GameObject[] mPlayers;
    private bool mJump;
    private float mLastJumpTime;
    private float mLastShootTime;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                Jump(0);
                break;
        }
        
        base.StateChanged();
        
        switch((EntityState)state) {
            case EntityState.Normal:
                bodyCtrl.inputEnabled = true;
                mJump = false;
                mLastJumpTime = 0;
                mLastShootTime = 0;
                break;
        }
    }


    protected override void Awake() {
        base.Awake();

        mPlayers = GameObject.FindGameObjectsWithTag("Player");

        bodyCtrl.landCallback += OnLanded;
    }

    Transform NearestPlayer(out float nearDistX) {
        nearDistX = Mathf.Infinity;
        Transform nearT = null;

        float x = transform.position.x;
        for(int i = 0, max = mPlayers.Length; i < max; i++) {
            if(mPlayers[i] && mPlayers[i].activeSelf) {
                Transform t = mPlayers[i].transform;
                float distX = Mathf.Abs(t.position.x - x);
                if(distX < nearDistX) {
                    nearT = t;
                    nearDistX = distX;
                }
            }
        }

        return nearT;
    }

    void FixedUpdate() {
        switch((EntityState)state) {
            case EntityState.Normal:
                float distX;
                Transform nearest = NearestPlayer(out distX);

                if(bodyCtrl.isGrounded) {
                    if(!mJump && !bodyCtrl.isJump) {
                        if(Time.fixedTime - mLastJumpTime > jumpRepeatDelay) {
                            Jump(0);
                            Jump(2.0f);
                            mJump = true;
                        }
                    }
                }
                else {
                    if(nearest && nearest.position.y < transform.position.y && Time.fixedTime - bodyCtrl.jumpLastTime > 0.15f) {
                        Jump(0);
                    }
                }

                if(nearest) {
                    if(distX <= shootRangeX) {
                        Vector3 pos = shootPt.position; pos.z = 0.0f;

                        //check bounds
                        Bounds plyrB = nearest.collider.bounds;
                        float minY = plyrB.min.y;
                        float maxY = plyrB.max.y;
                        if(pos.y >= minY && pos.y <= maxY) {
                            if(Time.fixedTime - mLastShootTime > shootCooldown) {
                                Vector3 dir = new Vector3(bodySpriteCtrl.isLeft ? -1.0f : 1.0f, 0.0f, 0.0f);

                                Projectile.Create(projGroup, projType, pos, dir, null);
                                shootAnim.Play("shoot");

                                mLastShootTime = Time.fixedTime;
                            }
                        }
                    }

                    bodySpriteCtrl.isLeft = Mathf.Sign(nearest.position.x - transform.position.x) < 0.0f;
                }
                break;
        }
    }

    void OnLanded(PlatformerController ctrl) {
        mJump = false;
        mLastJumpTime = Time.fixedTime;
    }
}
