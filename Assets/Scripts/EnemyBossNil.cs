using UnityEngine;
using System.Collections;

public class EnemyBossNil : Enemy {
    public enum Phase {
        None,
        Idle,
        Move,
        CastGround,
        CastMissile,
        Dead
    }

    public Phase[] phasePattern;

    public Transform[] teleports;

    public Transform projPoint;

    public float idleDelay = 0.6f;

    public float moveTurnDelay = 0.5f;

    public float moveJumpStartDelay = 1.0f;
    public float moveJumpDelay = 0.5f;
    public float[] moveJumpSeconds;
    public string moveJumpProj;
    //public float moveJumpPause = 0.2f;
    public int moveJumpProjCount = 3;
    public float moveJumpProjAngleRng = 80.0f;
    public float moveEndDelay = 1.0f;

    public Transform[] missilePts;
    public string missileProj;
    public float missileStartSpeed;
    public float missileMoveStartDelay = 0.5f;
    public float missileMoveSpeed;
    public float missileMoveDelay = 0.5f;
    public float missilePlayerRadius = 1.5f;
    public Vector2 missilePlayerAheadMin;
    public Vector2 missilePlayerAhead;

    public GameObject groundCastActiveGO;
    public AnimatorData groundCastSpell;
    public float groundCastStartDelay = 0.5f;

    public SoundPlayer shootSfx;
    public SoundPlayer seekerSfx;
    public SoundPlayer teleInSfx;
    public SoundPlayer teleOutSfx;
    public SoundPlayer groundCastSfx;
    public SoundPlayer deathSfx;

    public const string moveRoutine = "DoMove";
    public const string idleRoutine = "DoIdle";
    public const string missileRoutine = "DoMissile";
    public const string groundCastRoutine = "DoGroundCast";
    public const string deadRoutine = "DoDead";

    public const string clipCast = "cast";
    public const string clipDead = "cry";

    public const string takeWarpOut = "warpout";
    public const string takeWarpIn = "warpin";

    public const string takeGroundCast = "go";

    private AnimatorData mAnimDat;
    private Player mPlayer;
    private Phase mCurPhase = Phase.None;

    private bool mMoveTowardsPlayer;
    private float mLastMoveTurnTime;

    private ProjectileTweenTo[] mMissiles;

    private int mCurPhasePattern = 0;
    private int mCurTeleportInd = 0;

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

                SetPhysicsActive(true, false);

                NextPhasePattern();
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
        
        //Debug.Log("phase: " + phase);

        bodySpriteCtrl.StopOverrideClip();
        bodySpriteCtrl.lockFacing = false;
        bodySpriteCtrl.RefreshFacing();
        
        //prev
        switch(mCurPhase) {
            case Phase.Idle:
                StopCoroutine(idleRoutine);
                break;

            case Phase.CastGround:
                groundCastSpell.gameObject.SetActive(false);
                groundCastActiveGO.SetActive(false);
                StopCoroutine(groundCastRoutine);
                break;

            case Phase.CastMissile:
                for(int i = 0; i < mMissiles.Length; i++) {
                    if(mMissiles[i]) {
                        if(mMissiles[i].isAlive)
                            mMissiles[i].state = (int)Projectile.State.Dying;
                        else if(!mMissiles[i].isReleased)
                            mMissiles[i].Release();

                        mMissiles[i] = null;
                    }
                }

                SetPhysicsActive(true, false);

                StopCoroutine(missileRoutine);
                break;

            case Phase.Move:
                mMoveTowardsPlayer = false;
                StopCoroutine(moveRoutine);
                break;
                
            case Phase.Dead:
                StopCoroutine(deadRoutine);
                break;
        }
        
        switch(phase) {
            case Phase.Idle:
                bodySpriteCtrl.lockFacing = true;
                StartCoroutine(idleRoutine);
                break;

            case Phase.CastGround:
                bodySpriteCtrl.lockFacing = true;
                StartCoroutine(groundCastRoutine);
                break;

            case Phase.CastMissile:
                StartCoroutine(missileRoutine);
                break;

            case Phase.Move:
                StartCoroutine(moveRoutine);
                break;
                
            case Phase.Dead:
                StartCoroutine(deadRoutine);
                break;
        }
        
        mCurPhase = phase;
    }
    
    public override void SpawnFinish() {
        base.SpawnFinish();
    }

    protected override void SpawnStart() {
        base.SpawnStart();

        SetPhysicsActive(false, false);
    }
    
    protected override void Awake() {
        base.Awake();
        
        mPlayer = Player.instance;
        mAnimDat = GetComponent<AnimatorData>();

        M8.ArrayUtil.Shuffle(teleports);

        mMissiles = new ProjectileTweenTo[missilePts.Length];

        groundCastSpell.gameObject.SetActive(false);
        groundCastActiveGO.SetActive(false);

        bodyCtrl.landCallback += OnLand;
    }

    void FacePlayer() {
        Vector3 playerPos, pos;
        float s;
        playerPos = mPlayer.transform.position;
        pos = transform.position;
        s = Mathf.Sign(playerPos.x - pos.x);
        bodySpriteCtrl.isLeft = s < 0.0f;
    }

    void Update() {
        switch(mCurPhase) {
            case Phase.Idle:
            case Phase.CastGround:
                if(bodyCtrl.isGrounded) {
                    //face player
                    FacePlayer();
                }
                break;

            case Phase.Move:
                if(mMoveTowardsPlayer) {
                    if(bodyCtrl.isGrounded) {
                        float s;
                        Vector3 playerPos, pos;

                        //face player, move
                        playerPos = mPlayer.transform.position;
                        pos = transform.position;
                        s = Mathf.Sign(playerPos.x - pos.x);
                        if(s != bodyCtrl.moveSide && Time.time - mLastMoveTurnTime > moveTurnDelay) {
                            bodySpriteCtrl.isLeft = s < 0.0f;
                            bodyCtrl.moveSide = s;
                            mLastMoveTurnTime = Time.time;
                        }
                    }
                }
                break;
        }
    }

    void OnLand(PlatformerController ctrl) {
        if(state != (int)EntityState.Invalid) {
            Vector2 p = transform.position;
            PoolController.Spawn("fxp", "landdust", "landdust", null, p);
        }
    }

    void NextPhasePattern() {
        ToPhase(phasePattern[mCurPhasePattern]);
        mCurPhasePattern++;
        if(mCurPhasePattern == phasePattern.Length)
            mCurPhasePattern = 0;
    }

    IEnumerator DoDead() {
        deathSfx.Play();

        mPlayer.currentWeaponIndex = 0;

        HUD.instance.barBoss.gameObject.SetActive(false);

        bodyCtrl.moveSide = 0.0f;
        SetDamageTriggerActive(false);

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        do {
            yield return wait;
        } while(!bodyCtrl.isGrounded);

        SetPhysicsActive(false, false);

        bodySpriteCtrl.isLeft = true;
        bodySpriteCtrl.PlayOverrideClip(clipDead);

        state = (int)EntityState.Final;
    }

    IEnumerator DoIdle() {
        bodyCtrl.moveSide = 0.0f;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        
        do {
            yield return wait;
        } while(!bodyCtrl.isGrounded);

        yield return new WaitForSeconds(idleDelay);

        //next phase
        NextPhasePattern();
    }

    IEnumerator DoGroundCast() {
        bodyCtrl.moveSide = 0.0f;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        
        do {
            yield return wait;
        } while(!bodyCtrl.isGrounded);

        groundCastActiveGO.SetActive(true);

        bodySpriteCtrl.PlayOverrideClip(clipCast);

        yield return new WaitForSeconds(groundCastStartDelay);

        groundCastSfx.Play();

        //set to player's x loc.
        Vector3 groundCastPos = groundCastSpell.transform.position;
        groundCastPos.x = mPlayer.transform.position.x;
        groundCastSpell.transform.position = groundCastPos;

        groundCastSpell.gameObject.SetActive(true);
        groundCastSpell.Play(takeGroundCast);
        while(groundCastSpell.isPlaying)
            yield return wait;

        ToPhase(Phase.Idle);
    }

    IEnumerator DoMissile() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        do {
            yield return wait;
        } while(!bodyCtrl.isGrounded);

        FacePlayer();

        //jump
        Jump(0.3f);


        //wait till we are about to drop
        while(bodyCtrl.isGrounded || bodyCtrl.isJump || bodyCtrl.localVelocity.y > -0.1f)
            yield return wait;

        SetPhysicsActive(false, false);

        //warp-out
        teleOutSfx.Play();

        mAnimDat.Play(takeWarpOut);
        while(mAnimDat.isPlaying)
            yield return wait;

        //launch
        seekerSfx.Play();

        Vector3 missilePt = collider.bounds.center; missilePt.z = 0;

        for(int i = 0; i < mMissiles.Length; i++) {
            mMissiles[i] = Projectile.Create(projGroup, missileProj, missilePt, Vector3.zero, null) as ProjectileTweenTo;

            //move missile to start pos.
            mMissiles[i].Move(missilePts[i].position, missileStartSpeed);
        }

        while(true) {
            int numDone = 0;
            for(int i = 0; i < mMissiles.Length; i++)
                if(!mMissiles[i].isMoveActive)
                    numDone++;
            if(numDone == mMissiles.Length)
                break;

            yield return wait;
        }
        //

        yield return new WaitForSeconds(missileMoveStartDelay);

        M8.ArrayUtil.Shuffle(mMissiles);

        WaitForSeconds missileMoveWait = new WaitForSeconds(missileMoveDelay);

        //move towards player
        for(int i = 0; i < mMissiles.Length; i++) {
            Vector2 ofs = Random.insideUnitCircle;
            Vector3 toPos = mPlayer.collider.bounds.center; 
            Vector3 playerVel = mPlayer.controller.localVelocity;
            Vector3 playerVelDir = playerVel.normalized;

            if(Mathf.Abs(playerVel.x) >= missilePlayerAheadMin.x)
                toPos.x += playerVelDir.x*missilePlayerAhead.x;
            if(Mathf.Abs(playerVel.y) >= missilePlayerAheadMin.y)
                toPos.y += playerVelDir.y*missilePlayerAhead.y;

            toPos.x += ofs.x*missilePlayerRadius;
            toPos.y += ofs.y*missilePlayerRadius;
            toPos.z = 0.0f;

            mMissiles[i].Move(toPos, missileMoveSpeed);
            seekerSfx.Play();
            yield return missileMoveWait;
        }
        
        while(true) {
            int numDone = 0;
            for(int i = 0; i < mMissiles.Length; i++)
                if(!mMissiles[i].isMoveActive)
                    numDone++;
            if(numDone == mMissiles.Length)
                break;
            
            yield return wait;
        }
        //

        //determine warp pos.
        transform.position = teleports[mCurTeleportInd].position;
        mCurTeleportInd++; if(mCurTeleportInd == teleports.Length) mCurTeleportInd = 0;

        //move missiles to new pos.
        for(int i = 0; i < mMissiles.Length; i++) {
            mMissiles[i].Move(missilePts[i].position, missileMoveSpeed);
            seekerSfx.Play();
            yield return missileMoveWait;
        }

        while(true) {
            int numDone = 0;
            for(int i = 0; i < mMissiles.Length; i++)
                if(!mMissiles[i].isMoveActive)
                    numDone++;
            if(numDone == mMissiles.Length)
                break;
            
            yield return wait;
        }
        //

        //warp-in
        teleInSfx.Play();

        mAnimDat.Play(takeWarpIn);
        while(mAnimDat.isPlaying)
            yield return wait;

        ToPhase(Phase.Idle);
    }

    IEnumerator DoMove() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        do {
            yield return wait;
        } while(!bodyCtrl.isGrounded);

        bodySpriteCtrl.RefreshFacing();
        mMoveTowardsPlayer = true;
        mLastMoveTurnTime = 0;

        yield return new WaitForSeconds(moveJumpStartDelay);

        WaitForSeconds jumpDelay = new WaitForSeconds(moveJumpDelay);
        //WaitForSeconds landDelay = new WaitForSeconds(moveJumpPause);

        //jump and stuff
        for(int i = 0; i < moveJumpSeconds.Length; i++) {
            //jump
            Jump(moveJumpSeconds[i]);

            //wait till we are about to drop
            while(bodyCtrl.isGrounded || bodyCtrl.isJump || bodyCtrl.localVelocity.y > -0.1f)
                yield return wait;

            //
            Vector3 projPos = projPoint.position; projPos.z = 0.0f;
            Vector3 dpos = mPlayer.collider.bounds.center - projPos; dpos.z = 0.0f;

            //face player
            bodyCtrl.moveSide = Mathf.Sign(dpos.x);
            mLastMoveTurnTime = Time.time;

            Vector3 dir = dpos.normalized;

            dir = Quaternion.Euler(0, 0, moveJumpProjAngleRng*0.5f)*dir;
            Quaternion rot = Quaternion.Euler(0, 0, -moveJumpProjAngleRng/(float)moveJumpProjCount);

            for(int p = 0; p < moveJumpProjCount; p++) {
                Projectile.Create(projGroup, moveJumpProj, projPos, dir, null);
                dir = rot*dir;
            }

            shootSfx.Play();
            //

            //wait till we are landed
            while(!bodyCtrl.isGrounded)
                yield return wait;

            //wait a bit
            yield return jumpDelay;
        }

        yield return new WaitForSeconds(moveEndDelay);

        ToPhase(Phase.Idle);
    }
}
