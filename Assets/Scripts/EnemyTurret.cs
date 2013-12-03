using UnityEngine;
using System.Collections;

public class EnemyTurret : Enemy {
    [System.Serializable]
    public class BarrelDat {
        public string idle = "idle";
        public string fire = "fire";
        public int numFire = 3;

        [System.NonSerialized]
        public tk2dSpriteAnimationClip idleClip;

        [System.NonSerialized]
        public tk2dSpriteAnimationClip fireClip;

        public void Init(tk2dSpriteAnimator anim) {
            idleClip = anim.GetClipByName(idle);
            fireClip = anim.GetClipByName(fire);
        }

        public void FireBullets(Transform[] spawnPts) {
            for(int i = 0; i < numFire; i++) {
                Projectile.Create(projGroup, projCommonType, spawnPts[i].position, spawnPts[i].up, null);
            }
        }
    }

    public tk2dSpriteAnimator baseAnim;
    public tk2dSpriteAnimator turretAnim;

    public string idle = "idle";
    public string fire = "fire";
    public string firing = "firing";

    public BarrelDat[] barrels;

    public Transform[] spawnPoints;

    public int startTurret = 0;
    public float shootDelay = 0.5f;

    private const string mInvokeFunc = "DoFire";

    private int mCurTurret;

    private tk2dSpriteAnimationClip mIdleClip;
    private tk2dSpriteAnimationClip mFireClip;
    private tk2dSpriteAnimationClip mFiringClip;

    protected override void StateChanged() {
        base.StateChanged();

        switch((EntityState)prevState) {
            case EntityState.Normal:
                mCurTurret = startTurret;
                SetFireActive(false);
                break;
        }

        switch((EntityState)state) {
            case EntityState.Normal:
                SetFireActive(true);
                break;

            case EntityState.RespawnWait:
                mCurTurret = startTurret;
                SetFireActive(false);
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        foreach(BarrelDat barrel in barrels)
            barrel.Init(turretAnim);

        turretAnim.AnimationCompleted = OnTurretAnimEnd;

        mIdleClip = baseAnim.GetClipByName(idle);
        mFireClip = baseAnim.GetClipByName(fire);
        mFiringClip = baseAnim.GetClipByName(firing);

        baseAnim.AnimationCompleted = OnBaseAnimEnd;

        mCurTurret = startTurret;
    }

    void SetFireActive(bool aActive) {
        if(aActive) {
            if(!IsInvoking(mInvokeFunc)) {
                Invoke(mInvokeFunc, shootDelay);
            }
        }
        else {
            CancelInvoke(mInvokeFunc);
        }

        if(baseAnim && mIdleClip != null)
            baseAnim.Play(mIdleClip);

        if(turretAnim != null && barrels != null && barrels.Length > 0 && barrels[mCurTurret].idleClip != null)
            turretAnim.Play(barrels[mCurTurret].idleClip);
    }

    void DoFire() {
        baseAnim.Play(mFireClip);
    }

    void OnBaseAnimEnd(tk2dSpriteAnimator anim, tk2dSpriteAnimationClip clip) {
        if(anim == baseAnim && clip == mFireClip) {
            if(state == (int)EntityState.Normal) {
                baseAnim.Play(mFiringClip);
                turretAnim.Play(barrels[mCurTurret].fireClip);

                barrels[mCurTurret].FireBullets(spawnPoints);
            }
            else
                SetFireActive(false);
        }
    }

    void OnTurretAnimEnd(tk2dSpriteAnimator anim, tk2dSpriteAnimationClip clip) {
        if(anim == turretAnim && clip == barrels[mCurTurret].fireClip) {
            mCurTurret++;
            if(mCurTurret == barrels.Length)
                mCurTurret = 0;

            SetFireActive(state == (int)EntityState.Normal);
        }
    }
}
