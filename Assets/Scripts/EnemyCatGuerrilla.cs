using UnityEngine;
using System.Collections;

public class EnemyCatGuerrilla : Enemy {
    public tk2dSpriteAnimator anim;

    public Transform attackPoint;

    public string idleClip = "idle";
    public string attackPrepClip = "attackPrep";
    public string attackClip = "attack";

    public float idleDelay = 0.0f;
    public float idleRandDelay = 0.5f;

    public string projType;

    public float projVelocity = 10.0f;
    public float projFarVelocity = 12.0f;
    public float projNearVelocity = 6.0f;

    public float acquireRange; //radius to find target
    public LayerMask acquireMask;

    private Transform mCurTarget;

    private tk2dSpriteAnimationClip mIdleClip;
    private tk2dSpriteAnimationClip mAttackPrepClip;
    private tk2dSpriteAnimationClip mAttackClip;

    private float mLastIdleTime;
    private float mCurIdleDelay;
    private Projectile mCurProj;

    protected override void StateChanged() {
        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                mCurTarget = null;
                mLastIdleTime = 0.0f;
                anim.Play(mIdleClip);
                break;

            case EntityState.Dead:
            case EntityState.RespawnWait:
            case EntityState.Invalid:
                if(mCurProj != null) {
                    mCurProj.releaseCallback -= OnProjRelease;
                    mCurProj = null;
                }
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        mIdleClip = anim.GetClipByName(idleClip);
        mAttackPrepClip = anim.GetClipByName(attackPrepClip);
        mAttackClip = anim.GetClipByName(attackClip);

        anim.AnimationCompleted += OnAnimationEnd;
    }

    void Update() {
        switch((EntityState)state) {
            case EntityState.Normal:
                if(mCurProj == null && anim.CurrentClip == mIdleClip) {
                    if(mCurTarget) {
                        //check if still in distance
                        Vector3 dir = mCurTarget.position - transform.position;
                        float distSqr = dir.sqrMagnitude;

                        if(distSqr >= acquireRange * acquireRange) {
                            mCurTarget = null;
                        }
                        else {
                            //set facing
                            anim.Sprite.FlipX = Mathf.Sign(dir.x) < 0.0f;

                            //wait a bit
                            if(Time.time - mLastIdleTime > mCurIdleDelay) {
                                //prep attack and this will eventually throw projectile
                                anim.Play(mAttackPrepClip);
                            }
                        }
                    }
                    else {
                        //find target
                        Collider[] cols = Physics.OverlapSphere(transform.position, acquireRange, acquireMask);

                        if(cols.Length > 1) {
                            float shortestDistSqr = Mathf.Infinity;

                            for(int i = 0, max = cols.Length; i < max; i++) {
                                Collider col = cols[i];

                                float distSqr = (col.transform.position - transform.position).sqrMagnitude;
                                if(distSqr < shortestDistSqr) {
                                    mCurTarget = col.transform;
                                    shortestDistSqr = distSqr;
                                }
                            }
                        }
                        else if(cols.Length == 1) {
                            mCurTarget = cols[0].transform;
                        }
                    }
                }
                break;
        }
    }

    void OnProjRelease(EntityBase ent) {
        if(mCurProj == ent) {
            mCurProj.releaseCallback -= OnProjRelease;
            mCurProj = null;

            //refresh idle delay
            mCurIdleDelay = idleDelay + Random.value * idleRandDelay;
            mLastIdleTime = Time.time;

            anim.Play(mIdleClip);
        }
    }

    void OnAnimationEnd(tk2dSpriteAnimator aAnim, tk2dSpriteAnimationClip aClip) {
        if(aAnim == anim) {
            if(aClip == mAttackPrepClip) {
                anim.Play(mAttackClip);

                //launch projectile
                Vector3 pt = attackPoint.position;
                pt.z = 0.0f;
                mCurProj = Projectile.Create(projGroup, projType, pt, attackPoint.up, mCurTarget);
                mCurProj.releaseCallback += OnProjRelease;

                ProjectileArc projArc = mCurProj as ProjectileArc;
                if(projArc) {
                    projArc.seekVelocity = projVelocity;
                    projArc.nearVelocity = projNearVelocity;
                    projArc.farVelocity = projFarVelocity;
                }
            }
            else if(aClip == mAttackClip) {
                anim.Play(mIdleClip);
            }
        }
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        if(acquireRange > 0.0f) {
            Color clr = Color.yellow;
            clr.a = 0.3f;
            Gizmos.color = clr;
            Gizmos.DrawWireSphere(transform.position, acquireRange);
        }
    }
}
