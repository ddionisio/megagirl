using UnityEngine;
using System.Collections;

public class EnemyJumpNShoot : Enemy {
    public tk2dSpriteAnimator anim;

    public string projType = projCommonType;

    public float activateRange = 5.0f;
    public float activateCheckDelay = 0.2f;
    public float jumpRepeatDelay = 1.0f;


    public string idleClip = "idle";
    public string jumpClip = "jump";
    public string landClip = "land"; //make sure it is in Once loop
    public string jumpReadyClip = "jumpReady"; //make sure it is in once loop

    private Transform mTarget;
    private bool mJumping;
    private GameObject[] mPlayers;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                CancelInvoke("DoActiveCheck");

                bodyCtrl.moveSide = 0;
                Jump(0);
                mJumping = false;

                Blink(0);
                anim.Play(idleClip);
                stats.isInvul = true;

                mTarget = null;
                break;
        }

        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                if(mPlayers == null)
                    mPlayers = GameObject.FindGameObjectsWithTag("Player");

                bodyCtrl.inputEnabled = true;
                anim.Play(idleClip);
                stats.isInvul = true;
                InvokeRepeating("DoActiveCheck", 0, activateCheckDelay);
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        anim.AnimationCompleted += OnAnimEnd;
        bodyCtrl.landCallback += OnLanded;
    }

    void Update() {
        switch((EntityState)state) {
            case EntityState.Normal:
                if(mJumping) {
                    Vector3 pos = collider.bounds.center; pos.z = 0;
                    Vector3 targetPos = mTarget.collider.bounds.center;
                    if(targetPos.y < pos.y && (Time.fixedTime - bodyCtrl.jumpLastTime > 0.07f)) {
                        Jump(0);
                    }

                    if(bodyCtrl.moveSide != 0.0f && Mathf.Sign(targetPos.x - pos.x) != Mathf.Sign(bodyCtrl.moveSide))
                        bodyCtrl.moveSide = 0.0f;
                }
                break;
        }
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
            anim.Play(jumpReadyClip);
            stats.isInvul = false;
        }
    }

    void OnAnimEnd(tk2dSpriteAnimator aAnim, tk2dSpriteAnimationClip aClip) {
        if(aClip.name == jumpReadyClip) {
            mJumping = true;
            Jump(5.0f);
            anim.Play(jumpClip);

            Vector3 pos = collider.bounds.center; pos.z = 0;
            Vector3 targetPos = mTarget.collider.bounds.center;
            bodyCtrl.moveSide = Mathf.Sign(targetPos.x - pos.x)*0.5f;
        }
        else if(aClip.name == landClip) {
            mTarget = null;
            Blink(0);
            stats.isInvul = true;
            anim.Play(idleClip);
            InvokeRepeating("DoActiveCheck", jumpRepeatDelay, activateCheckDelay);
        }
    }

    void OnLanded(PlatformerController ctrl) {
        if(mJumping) {
            Vector3 pos = collider.bounds.center; pos.z = 0;
            Vector3 targetPos = mTarget.collider.bounds.center;
            Projectile.Create(projGroup, projType, pos, new Vector3(Mathf.Sign(targetPos.x - pos.x), 0, 0), null);


            mJumping = false;
            bodyCtrl.rigidbody.velocity = Vector3.zero;
            bodyCtrl.moveSide = 0.0f;
            Jump(0);
            anim.Play(landClip);
        }
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        if(activateRange > 0) {
            Gizmos.color = Color.cyan*0.5f;
            Gizmos.DrawWireSphere(transform.position, activateRange);
        }
    }
}
