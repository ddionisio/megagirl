using UnityEngine;
using System.Collections;

public class EnemyJumpNShootSimple : Enemy {
    public string projType = projCommonType;

    public float shootRange = 10.0f;
    public float shootCooldown = 1.0f;
    public AnimatorData shootAnim;
    public Transform shootPt;
    public int shootCount = 3;
    public float shootAngle = 90.0f;

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
                    if(distSqr <= shootRange*shootRange) {
                        Vector3 pos = shootPt.position; pos.z = 0.0f;

                        //check bounds
                        //Bounds plyrB = nearest.collider.bounds;
                        //float minY = plyrB.min.y;
                        //float maxY = plyrB.max.y;
                        //if(pos.y >= minY && pos.y <= maxY) {
                            if(Time.fixedTime - mLastShootTime > shootCooldown) {
                                Quaternion rot = Quaternion.AngleAxis(shootAngle/((float)(shootCount-1)), bodySpriteCtrl.isLeft ? Vector3.back : Vector3.forward);
                                Vector3 dir = new Vector3(bodySpriteCtrl.isLeft ? -1.0f : 1.0f, 0.0f, 0.0f);
                                dir = Quaternion.AngleAxis(shootAngle*0.5f, bodySpriteCtrl.isLeft ? Vector3.forward : Vector3.back)*dir;

                                for(int i = 0; i < shootCount; i++) {
                                    Projectile.Create(projGroup, projType, pos, dir, null);
                                    dir = rot*dir;
                                }

                                shootAnim.Play("shoot");

                                mLastShootTime = Time.fixedTime;
                            }
                        //}
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

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        if(shootRange > 0 && collider != null) {
            Gizmos.color = Color.blue*0.5f;
            Gizmos.DrawWireSphere(collider.bounds.center, shootRange);
        }
    }
}
