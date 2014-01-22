using UnityEngine;
using System.Collections;

public class EnemyBossCloneGirl : Enemy {
    public enum Phase {
        None,
        Move,
        Crazy,
        Dead
    }

    public const string defeatClip = "defeated";
    public const string crazyClip = "crazy";
    public const string crazyPrepClip = "attackPrep";
    public const string castClip = "cast";

    public TransAnimWaveOfsRand shaker;
    public int maxWallJump = 3;

    public float fearCheckRadius = 5.0f;
    public float fearCheckDist = 20.0f;
    public LayerMask fearCheckMask;

    public string angryChaserType = "angryClone";
    public int angryChaserCount = 3;
    public float angryChaserLaunchDelay = 2.0f;
    public Vector3 chaserOfs;
    public int chaserAccumCount = 9;

    public Transform[] crazySpawnPts;
    public string crazyProjType = "angryCloneStill";
    public float crazyPrepDelay = 2.0f;

    public float moveWaitDelay = 0.3f;

    public ParticleSystem castParticle;
    public float castWaitDelay; //while standing still, wait for this
    public string castProjSeekType;
    public int castProjCount = 2;

    private const string ChaserLaunchFunc = "DoChaserLaunch";
    private const string CrazyRoutine = "DoCrazy";

    private Phase mCurPhase = Phase.None;

    private EntitySensor mSensor;

    private Player mPlayer;
    private int mCurWallJumpCount;
    private int mCurChaserCount;
    private int mChaserAccumCount;
    private int mCurCastProjCount;

    private Projectile[] mCrazyProjs;
    private bool mCrazyProjsActive;

    private float mLastMoveTime;

    private float mLastMoveSide;

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
                break;
                
            case EntityState.Invalid:
                ToPhase(Phase.None);
                break;
        }
    }

    void ToPhase(Phase phase) {
        if(mCurPhase == phase)
            return;

        mSensor.hFlip = bodySpriteCtrl.isLeft;
        bodySpriteCtrl.StopOverrideClip();
        Jump(0);
        shaker.enabled = false;
        
        //Debug.Log("phase: " + phase);
        
        //prev
        switch(mCurPhase) {
            case Phase.Crazy:
                StopCoroutine(CrazyRoutine);

                for(int i = 0; i < mCrazyProjs.Length; i++) {
                    if(mCrazyProjs[i]) {
                        if(mCrazyProjs[i].isAlive) {
                            if(mCrazyProjs[i].stats)
                                mCrazyProjs[i].stats.curHP = 0;
                            else
                                mCrazyProjs[i].state = (int)Projectile.State.Dying;
                        }

                        mCrazyProjs[i] = null;
                    }
                }

                stats.damageAmp = 0.0f;
                mCrazyProjsActive = false;
                break;

            case Phase.Move:
                mSensor.Activate(false);
                bodyCtrl.moveSide = 0.0f;

                CancelInvoke(ChaserLaunchFunc);
                break;
        }

        switch(phase) {
            case Phase.Move:
                mLastMoveTime = Time.fixedTime;

                mChaserAccumCount = 0;

                animator.Play("normal");
                mCurWallJumpCount = 0;
                mSensor.Activate(true);

                if(!IsInvoking(ChaserLaunchFunc))
                    InvokeRepeating(ChaserLaunchFunc, 0.0f, angryChaserLaunchDelay);
                break;

            case Phase.Crazy:
                StartCoroutine(CrazyRoutine);
                break;

            case Phase.Dead:
                break;
        }

        mCurPhase = phase;
    }

    public override void SpawnFinish() {
        base.SpawnFinish();
    }
    
    protected override void Awake() {
        base.Awake();

        shaker.enabled = false;
        
        bodyCtrl.landCallback += OnLanded;
        bodySpriteCtrl.flipCallback += OnBodySpriteChangeFace;
        bodySpriteCtrl.clipFinishCallback += OnBodySpriteAnimEnd;

        mSensor = GetComponent<EntitySensor>();
        mSensor.updateCallback += OnSensorUpdate;

        mCrazyProjs = new Projectile[crazySpawnPts.Length - 1];
    }

    protected override void Start() {
        base.Start();
        
        mPlayer = Player.instance;
    }

    protected override void OnStatsHPChange(Stats stat, float delta) {
        base.OnStatsHPChange(stat, delta);

        if(mCurPhase == Phase.Crazy) {
            //detonate
            if(mCrazyProjsActive)
                ToPhase(Phase.Move);
        }
    }
    
    void FixedUpdate() {
        if(mCurPhase == Phase.None)
            return;

        switch(mCurPhase) {
            case Phase.Move:
                mSensor.hFlip = bodySpriteCtrl.isLeft;

                //Vector3 playerPos = mPlayer.transform.position;
                //Vector3 pos = transform.position;
                //Vector3 dpos = playerPos - pos;

                if(bodySpriteCtrl.overrideClip != null)
                    return;

                if(bodyCtrl.isGrounded) {
                    mCurWallJumpCount = 0;

                    if(mCurCastProjCount == 0 && bodyCtrl.moveSide == 0.0f && Time.fixedTime - mLastMoveTime > castWaitDelay) {
                        CancelInvoke(ChaserLaunchFunc);
                        bodySpriteCtrl.PlayOverrideClip(castClip);
                        castParticle.Play();
                    }
                    else {
                        //check if player is within y range
                        RaycastHit hit;
                        if(Physics.SphereCast(collider.bounds.center, fearCheckRadius, Vector3.left, out hit, fearCheckDist, fearCheckMask)
                           || Physics.SphereCast(collider.bounds.center, fearCheckRadius, Vector3.right, out hit, fearCheckDist, fearCheckMask)) {
                            if(Time.fixedTime - mLastMoveTime > moveWaitDelay) {
                                mLastMoveTime = Time.fixedTime;

                                shaker.enabled = false;
                                mLastMoveSide = bodyCtrl.moveSide = -Mathf.Sign(hit.point.x - collider.bounds.center.x);
                            }
                        }
                        else {
                            if(bodyCtrl.moveSide != 0.0f)
                                mLastMoveTime = Time.fixedTime;

                            shaker.enabled = true;
                            bodyCtrl.moveSide = 0.0f;
                        }
                    }
                }
                else {
                    if(bodyCtrl.isWallStick) {
                        if(Time.fixedTime - bodyCtrl.wallStickLastTime > 0.1f) {
                            if(mCurWallJumpCount < maxWallJump) {
                                Jump(0);
                                Jump(2.0f);
                                mCurWallJumpCount++;
                            }

                            bodyCtrl.moveSide = -bodyCtrl.moveSide;
                        }
                    }
                    else if(bodyCtrl.moveSide == 0.0f)
                        bodyCtrl.moveSide = mLastMoveSide;

                    mLastMoveTime = Time.fixedTime;
                }
                break;

            case Phase.Dead:
                if(bodyCtrl.isGrounded && bodySpriteCtrl.overrideClip == null)
                    bodySpriteCtrl.PlayOverrideClip(defeatClip);
                break;
        }
    }

    void OnSensorUpdate(EntitySensor sensor) {
        switch(mCurPhase) {
            case Phase.Move:
                if(sensor.isHit) {
                    float nAngle = Vector3.Angle(Vector3.up, sensor.hit.normal);
                    if(Mathf.Abs(90.0f - nAngle) <= 5.0f) {
                        Jump(0);
                        Jump(2.0f);

                        if(bodyCtrl.moveSide == 0.0f)
                            bodyCtrl.moveSide = mLastMoveSide;
                    }
                }
                break;
        }
    }

    void OnLanded(PlatformerController ctrl) {
        //switch(mCurPhase) {
        //}
    }

    void OnBodySpriteChangeFace(PlatformerSpriteController ctrl) {
    }

    void OnBodySpriteAnimEnd(PlatformerSpriteController ctrl, tk2dSpriteAnimationClip clip) {
        if(clip.name == castClip) {
            Vector3 pos = collider.bounds.center; pos.z = 0.0f;
            Quaternion rot = Quaternion.AngleAxis(360.0f/((float)castProjCount), Vector3.forward);
            Vector3 dir = Quaternion.AngleAxis(360.0f*Random.value, Vector3.forward)*Vector3.up;

            for(int i = 0; i < castProjCount; i++) {
                //NOTE: assumes seeker proj
                Projectile proj = Projectile.Create(projGroup, castProjSeekType, pos, dir, Player.instance.transform);
                proj.releaseCallback += OnCastProjRelease;
                dir = rot*dir;
            }

            mCurCastProjCount = castProjCount;
        }
    }

    void DoChaserLaunch() {
        Projectile proj = Projectile.Create(projGroup, angryChaserType, transform.position + chaserOfs, Vector3.up, null);
        proj.releaseCallback += OnChaserProjRelease;

        mCurChaserCount++;
        if(mCurChaserCount == angryChaserCount) {
            CancelInvoke(ChaserLaunchFunc);
        }
    }

    void OnChaserProjRelease(EntityBase ent) {
        if(mCurChaserCount > 0) {
            mCurChaserCount--;
        }

        ent.releaseCallback -= OnChaserProjRelease;

        if(mCurPhase == Phase.Move) {
            mChaserAccumCount++;
            if(mChaserAccumCount == chaserAccumCount) {
                ToPhase(Phase.Crazy);
            }
            else if(mCurChaserCount == 0 && mCurCastProjCount == 0) {
                if(!IsInvoking(ChaserLaunchFunc))
                    InvokeRepeating(ChaserLaunchFunc, angryChaserLaunchDelay, angryChaserLaunchDelay);
            }
        }
    }

    void OnCastProjRelease(EntityBase ent) {
        if(mCurCastProjCount > 0)
            mCurCastProjCount--;

        ent.releaseCallback -= OnCastProjRelease;

        if(mCurCastProjCount == 0 && mCurPhase == Phase.Move) {
            mLastMoveTime = Time.fixedTime;

            if(!IsInvoking(ChaserLaunchFunc))
                InvokeRepeating(ChaserLaunchFunc, angryChaserLaunchDelay, angryChaserLaunchDelay);
        }
    }

    IEnumerator DoCrazy() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        //wait till we are on ground
        while(!bodyCtrl.isGrounded) {
            if(bodyCtrl.isWallStick) {
                bodyCtrl.moveSide = bodySpriteCtrl.isLeft ? -1 : 1;
            }
            else
                bodyCtrl.moveSide = 0.0f;

            yield return wait;
        }

        bodySpriteCtrl.PlayOverrideClip(crazyPrepClip);

        yield return new WaitForSeconds(crazyPrepDelay);

        //warp
        int warpInd = Random.Range(0, crazySpawnPts.Length);

        transform.position = crazySpawnPts[warpInd].position;

        animator.Play("crazy");
        bodySpriteCtrl.PlayOverrideClip(crazyClip);
        shaker.enabled = true;

        //spawn projs on each pt
        int projInd = 0;
        for(int i = 0; i < crazySpawnPts.Length; i++) {
            if(i != warpInd) {
                mCrazyProjs[projInd] = Projectile.Create(projGroup, crazyProjType, crazySpawnPts[i].position, Vector3.up, null);
                projInd++;
            }
        }

        mCrazyProjsActive = true;
        stats.damageAmp = 2.5f;

        bool projsAlive = true;
        while(projsAlive) {
            yield return wait;

            int numProjsDead = 0;
            for(int i = 0; i < mCrazyProjs.Length; i++) {
                if(!mCrazyProjs[i].spawning && !mCrazyProjs[i].isAlive)
                    numProjsDead++;
            }

            projsAlive = numProjsDead < mCrazyProjs.Length;
        }

        ToPhase(Phase.Move);
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        if(fearCheckRadius > 0.0f) {
            Gizmos.color = Color.cyan*0.5f;
            Gizmos.DrawWireSphere(collider.bounds.center, fearCheckRadius);
        }
    }
}
