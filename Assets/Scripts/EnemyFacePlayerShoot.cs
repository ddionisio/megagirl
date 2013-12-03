using UnityEngine;
using System.Collections;

public class EnemyFacePlayerShoot : Enemy {
    public float activateRange = 8.0f;
    public float activateCheckDelay = 0.2f;

    public bool leftIsFlip = false;

    public AnimatorData launcherAnim;
    public string launcherFireTake = "fire";
    public float launcherFireDelay;

    public string projType;
    public Transform projPt;

    private GameObject[] mPlayers;
    private tk2dBaseSprite[] mSprites;
    private Transform mTarget;

    public void Shoot() {
        Vector3 pt = projPt.position; pt.z = 0.0f;
        Projectile proj = Projectile.Create(projGroup, projType, pt, projPt.up, mTarget);
        if(proj) {
            tk2dBaseSprite spr = proj.GetComponentInChildren<tk2dBaseSprite>();
            spr.FlipY = mSprites[0].FlipX;
        }
    }

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                CancelInvoke("DoActiveCheck");
                launcherAnim.Stop();
                break;
        }

        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                if(mPlayers == null)
                    mPlayers = GameObject.FindGameObjectsWithTag("Player");

                if(!IsInvoking("DoActiveCheck"))
                    InvokeRepeating("DoActiveCheck", 0, activateCheckDelay);
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        mSprites = GetComponentsInChildren<tk2dBaseSprite>(true);

        launcherAnim.takeCompleteCallback += OnLauncherAnimComplete;
    }

    void DoActiveCheck() {
        mTarget = null;
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
        
        if(mTarget != null && nearestSqr < activateRange*activateRange) {
            CancelInvoke("DoActiveCheck");

            float faceSign = Mathf.Sign(mTarget.position.x - transform.position.x);
            for(int i = 0, max = mSprites.Length; i < max; i++) {
                mSprites[i].FlipX = faceSign < 0.0f ? leftIsFlip : !leftIsFlip;
            }

            launcherAnim.Play(launcherFireTake);
        }
    }

    void OnLauncherAnimComplete(AnimatorData animDat, AMTake take) {
        mTarget = null;
        InvokeRepeating("DoActiveCheck", launcherFireDelay, activateCheckDelay);
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();
        
        if(activateRange > 0) {
            Gizmos.color = Color.cyan*0.5f;
            Gizmos.DrawWireSphere(transform.position, activateRange);
        }
    }
}
