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

    private const string ChaserLaunchFunc = "DoChaserLaunch";
    private const string CrazyRoutine = "DoCrazy";

    private Phase mCurPhase = Phase.None;

    private EntitySensor mSensor;

    private Player mPlayer;
    private int mCurWallJumpCount;
    private int mCurChaserCount;
    private int mChaserAccumCount;

    private Projectile[] mCrazyProjs;
    private bool mCrazyProjsActive;

    private float mLastMoveTime;

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

                if(bodyCtrl.isGrounded) {
                    mCurWallJumpCount = 0;

                    //check if player is within y range
                    RaycastHit hit;
                    if(Physics.SphereCast(collider.bounds.center, fearCheckRadius, Vector3.left, out hit, fearCheckDist, fearCheckMask)
                       || Physics.SphereCast(collider.bounds.center, fearCheckRadius, Vector3.right, out hit, fearCheckDist, fearCheckMask)) {
                        if(Time.fixedTime - mLastMoveTime > moveWaitDelay) {
                            mLastMoveTime = Time.fixedTime;

                            shaker.enabled = false;
                            bodyCtrl.moveSide = -Mathf.Sign(hit.point.x - collider.bounds.center.x);
                        }
                    }
                    else {
                        shaker.enabled = true;
                        bodyCtrl.moveSide = 0.0f;
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
            else if(mCurChaserCount == 0) {
                if(!IsInvoking(ChaserLaunchFunc))
                    InvokeRepeating(ChaserLaunchFunc, angryChaserLaunchDelay, angryChaserLaunchDelay);
            }
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
