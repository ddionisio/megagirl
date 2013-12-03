using UnityEngine;
using System.Collections;

public class EnemyCatchy : Enemy {
    public GameObject[] buddies;
    public GameObject projectile;
    public float projectileSpeed;
    public float projectileRestDelay;
    public Transform[] projWP;

    public float chaseTimeScale = 1.5f;
    public float chaseCheckLength = 10.0f;
    public LayerMask chaseCheckMask;

    private Stats[] mBuddyStats;
    private tk2dSpriteAnimator[] mBuddyAnims;
    private TimeWarp[] mBuddyTimeWarps;

    private int mCurProjInd = 0;
    private int mCurProjDestInd = 0;
    private float mLastProjTime = 0;

    private float mProjMoveDelay; //compute from projectileSpeed and distance
    private Vector3[] mProjWPLocals;

    private float mProjMoveCurTime;
    private int mNumDead = 0;
    private string mDeathSpawnType;

    protected override void StateChanged() {
        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Stun:
                projectile.SetActive(false);

                Vector3 p = projWP[0].position; p.z = 0;
                projectile.transform.position = p;
                mCurProjInd = 0;
                mCurProjDestInd = 1;
                break;

            case EntityState.Normal:
                mLastProjTime = Time.fixedTime;
                projectile.SetActive(mNumDead == 0);

                if(animator && !animator.isPlaying)
                    animator.Play("move");
                break;

            case EntityState.RespawnWait:
                break;
        }
    }

    protected override void Restart() {
        base.Restart();

        for(int i = 0; i < buddies.Length; i++) {
            buddies[i].SetActive(true);
            mBuddyAnims[i].Play("normal");
            mBuddyStats[i].Reset();
        }

        mNumDead = 0;

        Vector3 p = projWP[0].position; p.z = 0;
        projectile.transform.position = p;
        mCurProjInd = 0;
        mCurProjDestInd = 1;

        mProjMoveCurTime = 0.0f;
    }

    public override void SpawnFinish() {
        base.SpawnFinish();

        for(int i = 0; i < mBuddyAnims.Length; i++) {
            mBuddyAnims[i].Play("normal");
        }

        mProjMoveCurTime = 0.0f;
    }

    protected override void Awake() {
        base.Awake();

        mBuddyStats = new Stats[buddies.Length];
        mBuddyAnims = new tk2dSpriteAnimator[buddies.Length];
        mBuddyTimeWarps = new TimeWarp[buddies.Length];

        for(int i = 0; i < buddies.Length; i++) {
            mBuddyStats[i] = buddies[i].GetComponent<Stats>();
            mBuddyStats[i].changeHPCallback += OnBuddyHPChange;
            mBuddyTimeWarps[i] = buddies[i].GetComponent<TimeWarp>();
            mBuddyAnims[i] = buddies[i].GetComponentInChildren<tk2dSpriteAnimator>();
        }

        mDeathSpawnType = deathSpawnType;
        deathSpawnType = "";

        mProjWPLocals = new Vector3[projWP.Length];
        for(int i = 0; i < mProjWPLocals.Length; i++) {
            mProjWPLocals[i] = transform.worldToLocalMatrix.MultiplyPoint(projWP[i].position);
            mProjWPLocals[i].z = 0.0f;
        }

        Vector3 p1 = projWP[0].position; p1.z = 0;
        Vector3 p2 = projWP[1].position; p2.z = 0;

        float projDist = (p2 - p1).magnitude;
        mProjMoveDelay = projDist/projectileSpeed;

        projectile.transform.position = p1;
        mCurProjInd = 0;
        mCurProjDestInd = 1;
    }

    void OnBuddyHPChange(Stats aStat, float delta) {
        int deadInd = -1;
        int stunInd = -1;

        for(int i = 0; i < mBuddyStats.Length; i++) {

            if(mBuddyStats[i] == aStat) {
                if(aStat.lastDamageSource && aStat.lastDamageSource.stun)
                    stunInd = i;

                if(aStat.curHP <= 0) {
                    deadInd = i;
                    mNumDead++;
                }
            }
        }

        if(deadInd != -1) {
            buddies[deadInd].SetActive(false);
            Vector3 pt = buddies[deadInd].collider.bounds.center;
            pt.z = 0.0f;
            PoolController.Spawn(deathSpawnGroup, mDeathSpawnType, mDeathSpawnType, null, pt, Quaternion.identity);
        }

        projectile.SetActive(mNumDead == 0);

        if(mNumDead == mBuddyStats.Length) {
            state = (int)EntityState.Dead;
        }
        else {
            if(deadInd != -1) {
                for(int i = 0; i < mBuddyAnims.Length; i++) {
                    if(i != deadInd) {
                        mBuddyAnims[i].Play("sad");
                    }
                }
            }
            else if(stunInd != -1) {
                state = (int)EntityState.Stun;
            }
        }
    }

    void FixedUpdate() {
        switch((EntityState)state) {
            case EntityState.Normal:
                float timeScale = 1.0f;
                for(int i = 0; i < mBuddyTimeWarps.Length; i++) {
                    if(mBuddyTimeWarps[i].scale < timeScale)
                        timeScale = mBuddyTimeWarps[i].scale;
                }

                if(animator) {
                    float animTimeScale = 1.0f;

                    RaycastHit hit;

                    for(int i = 0; i < buddies.Length; i++) {
                        Vector3 r = buddies[i].transform.right;
                        Vector3 pos = buddies[i].collider.bounds.center; pos.z = 0.0f;
                        if(Physics.Raycast(pos, r, out hit, chaseCheckLength, chaseCheckMask)) {
                            if(hit.collider.CompareTag("Player")) {
                                animTimeScale = chaseTimeScale;
                                break;
                            }
                        }
                        else if(Physics.Raycast(pos, -r, out hit, chaseCheckLength, chaseCheckMask)) {
                            if(hit.collider.CompareTag("Player")) {
                                animTimeScale = chaseTimeScale;
                                break;
                            }
                        }
                    }

                    animator.animScale = animTimeScale * timeScale;
                }

                /////////////////////////////
                //projectile move
                if(mNumDead == 0 && (Time.fixedTime - mLastProjTime) * timeScale > projectileRestDelay) {
                    mProjMoveCurTime += Time.fixedDeltaTime * timeScale;

                    if(mProjMoveCurTime >= mProjMoveDelay) {
                        Vector3 p = projWP[mCurProjDestInd].position; p.z = 0.0f;
                        projectile.transform.position = p;

                        mCurProjInd = mCurProjDestInd;
                        mCurProjDestInd++;
                        if(mCurProjDestInd == projWP.Length)
                            mCurProjDestInd = 0;

                        mLastProjTime = Time.fixedTime;
                        mProjMoveCurTime = 0.0f;
                    }
                    else {
                        float t = Holoville.HOTween.Core.Easing.Sine.EaseInOut(mProjMoveCurTime, 0.0f, 1.0f, mProjMoveDelay, 0.0f, 0.0f);

                        projectile.transform.position = transform.localToWorldMatrix.MultiplyPoint(Vector3.Lerp(mProjWPLocals[mCurProjInd], mProjWPLocals[mCurProjDestInd], t));
                    }
                }
                break;
        }
    }
}
