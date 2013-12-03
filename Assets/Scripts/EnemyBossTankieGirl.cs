using UnityEngine;
using System.Collections;

public class EnemyBossTankieGirl : Enemy {
    public enum Phase {
        None,
        Move,
        MoveNoTank,
        FireSeeker,
        FireCannon,
        Dead
    }

    public const string defeatClip = "defeated";

    public const string cryClip = "cry";

    public const string moveToPlayerFunc = "MoveToPlayer";
    public const string nextPhaseFunc = "SetToNextPhase";

    public const string moveFireRoutine = "DoMoveFire";
    public const string seekerFireRoutine = "DoSeekerFire";

    public EnemyBossTank tank;

    public float moveFacePlayerDelay = 1.0f;
    public float moveFireDelay;
    public Transform[] moveFirePts;
    public string moveFireProjType = projCommonType;
    public float moveNextPhaseDelay = 5.0f;

    public Transform seekerPt;
    public Transform seekerRotatePt;
    public AnimatorData seekerAnimDat;
    public AnimatorData seekerLauncherAnimDat;
    public string seekerProjType;
    public int seekerCount = 2;
    public float seekerFireDelay = 0.5f;

    public Transform[] movePts;

    private Phase mCurPhase = Phase.None;
    private Phase mNextPhase = Phase.FireSeeker;
    private Player mPlayer;

    private int mCurMovePtInd;

    public void ShootMissile() {
        Vector3 pt = seekerPt.position; pt.z = 0.0f;
        Vector3 dir = bodySpriteCtrl.anim.Sprite.FlipX ? -seekerPt.up : seekerPt.up;

        Projectile proj = Projectile.Create(projGroup, seekerProjType, pt, dir, mPlayer.transform);
        if(proj) {
            proj.stats.itemDropIndex = -1;
            tk2dBaseSprite spr = proj.GetComponentInChildren<tk2dBaseSprite>();
            spr.FlipY = bodySpriteCtrl.anim.Sprite.FlipX;
        }
    }

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                ToPhase(Phase.None);
                break;
        }

        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                bodyCtrl.inputEnabled = true;
                ToPhase(Phase.Move);
                break;
                
            case EntityState.Dead:
                ToPhase(Phase.Dead);
                if(tank.stats.curHP > 0) {
                    tank.stats.curHP = 0;
                }
                break;
                
            case EntityState.Invalid:
                ToPhase(Phase.None);
                break;
        }
    }

    void ToPhase(Phase phase) {
        if(mCurPhase == phase)
            return;

        //prev
        switch(mCurPhase) {
            case Phase.Move:
                StopCoroutine(moveFireRoutine);
                CancelInvoke(moveToPlayerFunc);
                CancelInvoke(nextPhaseFunc);

                tank.bodyCtrl.moveSide = 0.0f;
                break;

            case Phase.FireSeeker:
                StopCoroutine(seekerFireRoutine);
                seekerAnimDat.Play("default");
                break;
        }

        switch(phase) {
            case Phase.Move:
                if(tank.bodyCtrl.isGrounded) {
                    if(!IsInvoking(moveToPlayerFunc))
                        InvokeRepeating(moveToPlayerFunc, 0.0f, moveFacePlayerDelay);
                }
                else {
                    tank.bodyCtrl.moveSide = 0.0f; //wait till we land
                }

                Invoke(nextPhaseFunc, moveNextPhaseDelay);
                StartCoroutine(moveFireRoutine);
                break;

            case Phase.FireSeeker:
                StartCoroutine(seekerFireRoutine);
                break;

            case Phase.MoveNoTank:
                bodySpriteCtrl.StopOverrideClip();

                RigidBodyMoveToTarget mover = GetComponent<RigidBodyMoveToTarget>();
                mover.enabled = false;

                bodyCtrl.rigidbody.isKinematic = false;
                bodyCtrl.enabled = true;
                gravityCtrl.enabled = true;
                bodySpriteCtrl.controller = bodyCtrl;

                bodySpriteCtrl.moveClip = cryClip;
                bodySpriteCtrl.idleClip = cryClip;
                bodySpriteCtrl.upClips[0] = cryClip;
                bodySpriteCtrl.downClips[0] = cryClip;
                bodySpriteCtrl.RefreshClips();

                mCurMovePtInd = 0;
                break;

            case Phase.Dead:
                bodyCtrl.rigidbody.isKinematic = false;
                bodyCtrl.enabled = true;
                gravityCtrl.enabled = true;
                bodySpriteCtrl.controller = bodyCtrl;

                bodyCtrl.moveSide = 0.0f;
                Vector3 vel = bodyCtrl.localVelocity;
                vel.x = 0;
                bodyCtrl.localVelocity = vel;

                bodySpriteCtrl.StopOverrideClip();
                break;
        }

        mCurPhase = phase;
    }

    protected override void Awake() {
        base.Awake();

        tank.setStateCallback += OnTankStateChanged;

        bodyCtrl.rigidbody.isKinematic = true;
        bodyCtrl.enabled = false;
        gravityCtrl.enabled = false;
    }

    protected override void Start() {
        base.Start();
        
        mPlayer = Player.instance;

        tank.bodyCtrl.landCallback += OnTankLanded;

        bodySpriteCtrl.controller = tank.bodyCtrl;
    }

    void Update() {
        switch(mCurPhase) {
            case Phase.MoveNoTank:
                Vector3 pt = bodyCtrl.collider.bounds.center;
                Vector3 curMPt = movePts[mCurMovePtInd].position;
                float dX = curMPt.x - pt.x;
                if(Mathf.Abs(dX) <= 0.1f) {
                    mCurMovePtInd++; if(mCurMovePtInd == movePts.Length) mCurMovePtInd = 0;
                }
                else
                    bodyCtrl.moveSide = Mathf.Sign(dX);
                break;

            case Phase.Dead:
                if(bodyCtrl.isGrounded && bodySpriteCtrl.overrideClip == null)
                    bodySpriteCtrl.PlayOverrideClip(defeatClip);
                break;
        }
    }

    void OnTankStateChanged(EntityBase ent) {
        switch((EntityState)ent.state) {
            case EntityState.Dead:
                if((EntityState)state != EntityState.Dead) {
                    ToPhase(Phase.MoveNoTank);
                }
                break;
        }
    }

    void OnTankLanded(PlatformerController ctrl) {
        switch(mCurPhase) {
            case Phase.Move:
                if(!IsInvoking(moveToPlayerFunc))
                    InvokeRepeating(moveToPlayerFunc, 0.0f, moveFacePlayerDelay);
                break;
        }
    }

    void MoveToPlayer() {
        Vector3 dpos = mPlayer.transform.position - transform.position;
        float s = Mathf.Sign(dpos.x);
        if(tank.bodyCtrl.moveSide != s) {
            tank.bodyCtrl.moveSide = s;
        }
    }

    void SetToNextPhase() {
        ToPhase(mNextPhase);
        //mNextPhase = Phase.FireCannon;
    }

    IEnumerator DoMoveFire() {
        WaitForFixedUpdate waitUpdate = new WaitForFixedUpdate();
        WaitForSeconds waitDelay = new WaitForSeconds(moveFireDelay);

        do {
            //wait till we face player
            while(Mathf.Sign(mPlayer.collider.bounds.center.x - collider.bounds.center.x) != Mathf.Sign(tank.bodyCtrl.localVelocity.x))
                yield return waitUpdate;

            //fire
            for(int i = 0; i < moveFirePts.Length; i++) {
                Vector3 dir = moveFirePts[i].up;
                Vector3 pos = moveFirePts[i].position; pos.z = 0.0f;
                Projectile.Create(projGroup, moveFireProjType, pos, dir, null);
            }

            yield return waitDelay;
        } while(mCurPhase == Phase.Move);
    }

    IEnumerator DoSeekerFire() {
        SpriteHFlipRigidbodyVelX tankSpriteFlip = tank.GetComponent<SpriteHFlipRigidbodyVelX>();

        WaitForFixedUpdate waitUpdate = new WaitForFixedUpdate();
        WaitForSeconds waitFireDelay = new WaitForSeconds(seekerFireDelay);

        //prep
        seekerAnimDat.Play("prep");
        do {
            yield return waitUpdate;
        } while(seekerAnimDat.isPlaying);

        //move to a far spot away from player
        Vector3 playerPos = mPlayer.collider.bounds.center;
        
        float farthestX = 0;
        float farthestDistSq = 0;
        for(int j = 0; j < movePts.Length; j++) {
            Vector3 p = movePts[j].position;
            float d = Mathf.Abs(playerPos.x - p.x);
            if(d > farthestDistSq) {
                farthestDistSq = d;
                farthestX = p.x;
            }
        }
        
        //move till we are close
        while(Mathf.Abs(farthestX - collider.bounds.center.x) > 0.1f) {
            if(tank.bodyCtrl.moveSide == 0.0f || Mathf.Abs(tank.bodyCtrl.rigidbody.velocity.x) > 2.0f)
                tank.bodyCtrl.moveSide = Mathf.Sign(farthestX - collider.bounds.center.x);
            yield return waitUpdate;
        }

        tank.bodyCtrl.moveSide = 0.0f;
        tank.bodyCtrl.rigidbody.velocity = Vector3.zero;

        //fire stuff
        for(int i = 0; i < seekerCount; i++) {
            //face player
            float sign = Mathf.Sign(mPlayer.collider.bounds.center.x - collider.bounds.center.x);
            tankSpriteFlip.SetFlip(sign < 0.0f);
            bodySpriteCtrl.isLeft = sign < 0.0f;

            seekerLauncherAnimDat.Play("fire");
            do {
                yield return waitUpdate;
            } while(seekerLauncherAnimDat.isPlaying);

            yield return waitFireDelay;
        }

        //holster
        seekerAnimDat.Play("holster");
        do {
            yield return waitUpdate;
        } while(seekerAnimDat.isPlaying);

        if(mCurPhase == Phase.FireSeeker)
            ToPhase(Phase.Move);
    }
}
