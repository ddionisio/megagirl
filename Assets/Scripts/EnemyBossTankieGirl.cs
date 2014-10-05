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
    public const string panicShootFunc = "PanicShoot";

    public const string moveFireRoutine = "DoMoveFire";
    public const string seekerFireRoutine = "DoSeekerFire";
    public const string cannonFireRoutine = "DoCannonFire";

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

    public string cannonProjType;
    public Transform[] cannonFirePts;
    public float cannonMoveScale = 0.35f;
    public ParticleSystem cannonParticleReady;

    public Transform[] movePts;

    public Phase[] phasePattern;

    public Transform panicShootPt;
    public float panicJumpDelay = 2.5f;
    public float panicFireDelay = 1.0f;

    public SoundPlayer shootSfx;
    public SoundPlayer rocketSfx;
    public SoundPlayer cannonSfx;

    private Phase mCurPhase = Phase.None;
    private int mCurPhasePatternInd = 0;
    private Player mPlayer;

    private int mCurMovePtInd;

    private RigidBodyMoveToTarget mMoveToTarget;

    private float mPanicLastJumpTime;

    public void ShootMissile() {
        Vector3 pt = seekerPt.position; pt.z = 0.0f;
        Vector3 dir = bodySpriteCtrl.anim.Sprite.FlipX ? -seekerPt.up : seekerPt.up;

        Projectile proj = Projectile.Create(projGroup, seekerProjType, pt, dir, mPlayer.transform);
        if(proj) {
            proj.stats.itemDropIndex = -1;
            tk2dBaseSprite spr = proj.GetComponentInChildren<tk2dBaseSprite>();
            spr.FlipY = bodySpriteCtrl.anim.Sprite.FlipX;

            rocketSfx.Play();
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
                bodyCtrl.moveEnabled = true;
                ToPhase(Phase.Move);
                break;
                
            case EntityState.Dead:
                ToPhase(Phase.Dead);
                if(tank.stats.curHP > 0) {
                    tank.state = (int)EntityState.Dead;
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

            case Phase.MoveNoTank:
                CancelInvoke(panicShootFunc);
                break;

            case Phase.FireSeeker:
                StopCoroutine(seekerFireRoutine);
                seekerAnimDat.Play("default");
                break;

            case Phase.FireCannon:
                StopCoroutine(cannonFireRoutine);
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

            case Phase.FireCannon:
                StartCoroutine(cannonFireRoutine);
                break;

            case Phase.MoveNoTank:
                bodySpriteCtrl.StopOverrideClip();

                mMoveToTarget.enabled = false;

                bodyCtrl.rigidbody.isKinematic = false;
                bodyCtrl.enabled = true;
                bodyCtrl.moveEnabled = true;
                gravityCtrl.enabled = true;
                bodySpriteCtrl.controller = bodyCtrl;

                bodySpriteCtrl.moveClip = cryClip;
                bodySpriteCtrl.idleClip = cryClip;
                bodySpriteCtrl.upClips[0] = cryClip;
                bodySpriteCtrl.downClips[0] = cryClip;
                bodySpriteCtrl.RefreshClips();

                mCurMovePtInd = 0;

                mPanicLastJumpTime = 0.0f;

                InvokeRepeating(panicShootFunc, panicFireDelay, panicFireDelay);
                break;

            case Phase.Dead:
                mMoveToTarget.enabled = false;

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

        mMoveToTarget = GetComponent<RigidBodyMoveToTarget>();

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

                if(bodyCtrl.isGrounded && Time.time - mPanicLastJumpTime >= panicJumpDelay) {
                    Jump(0);
                    Jump(1);

                    mPanicLastJumpTime = Time.time;
                }
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
                if((EntityState)state != EntityState.Dead && mCurPhase != Phase.Dead) {
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
        //Phase nextPhase = Phase.FireCannon;
        Phase nextPhase = phasePattern[mCurPhasePatternInd];
        mCurPhasePatternInd++;
        if(mCurPhasePatternInd == phasePattern.Length)
            mCurPhasePatternInd = 0;

        ToPhase(nextPhase);
    }

    void PanicShoot() {
        Vector3 dir = bodySpriteCtrl.isLeft ? Vector3.left : Vector3.right;
        Vector3 pos = panicShootPt.position; pos.z = 0.0f;
        Projectile.Create(projGroup, moveFireProjType, pos, dir, null);

        shootSfx.Play();
    }

    IEnumerator DoMoveFire() {
        WaitForFixedUpdate waitUpdate = new WaitForFixedUpdate();
        WaitForSeconds waitDelay = new WaitForSeconds(moveFireDelay);

        do {
            //wait till we face player
            //while(Mathf.Sign(mPlayer.collider.bounds.center.x - collider.bounds.center.x) != Mathf.Sign(tank.bodyCtrl.localVelocity.x))
                //yield return waitUpdate;

            //fire
            for(int i = 0; i < moveFirePts.Length; i++) {
                Vector3 dir = moveFirePts[i].up;
                Vector3 pos = moveFirePts[i].position; pos.z = 0.0f;
                Projectile.Create(projGroup, moveFireProjType, pos, dir, null);
            }

            shootSfx.Play();

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

        ToPhase(Phase.Move);
    }

    IEnumerator DoCannonFire() {
        SpriteHFlipRigidbodyVelX tankSpriteFlip = tank.GetComponent<SpriteHFlipRigidbodyVelX>();
        
        WaitForFixedUpdate waitUpdate = new WaitForFixedUpdate();
        WaitForSeconds waitFireDelay = new WaitForSeconds(seekerFireDelay);

        tank.bodyCtrl.moveSide = 0.0f;
        tank.bodyCtrl.rigidbody.velocity = Vector3.zero;

        //face player
        float sign = Mathf.Sign(mPlayer.collider.bounds.center.x - collider.bounds.center.x);
        tankSpriteFlip.SetFlip(sign < 0.0f);
        bodySpriteCtrl.isLeft = sign < 0.0f;

        //play particle ready and wait
        cannonParticleReady.Play();
        while(cannonParticleReady.isPlaying || !tank.bodyCtrl.isGrounded) {
            sign = Mathf.Sign(mPlayer.collider.bounds.center.x - collider.bounds.center.x);
            tankSpriteFlip.SetFlip(sign < 0.0f);
            bodySpriteCtrl.isLeft = sign < 0.0f;

            yield return waitUpdate;
        }

        //jump
        tank.bodyCtrl.moveSide = sign*cannonMoveScale;
        tank.Jump(1.0f);

        //check if we start dropping, then fire
        while(tank.bodyCtrl.isGrounded || tank.bodyCtrl.isJump || tank.bodyCtrl.localVelocity.y < 0.0f)
            yield return waitUpdate;

        //fire
        for(int i = 0; i < cannonFirePts.Length; i++) {
            Vector3 pt = cannonFirePts[i].position; pt.z = 0.0f;
            Projectile.Create(projGroup, cannonProjType, pt, cannonFirePts[i].up, null);
        }

        cannonSfx.Play();

        //wait till we land
        while(!tank.bodyCtrl.isGrounded)
            yield return waitUpdate;

        tank.Jump(0.0f);

        ToPhase(Phase.Move);
    }
}
