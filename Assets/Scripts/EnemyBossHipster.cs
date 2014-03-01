using UnityEngine;
using System.Collections;

public class EnemyBossHipster : Enemy {
    public enum Phase {
        None,
        Idle,
        Move,
        Jump,
        SprayNPray,
        Cast,
        Dead
    }

    public AnimatorData timeWarp;
    public Transform[] timeWarpSpawnPts;
    public float timeWarpExpireDelay = 6.0f;
    public float timeWarpCooldown = 10.0f;

    public ParticleSystem dust;

    public string gunProjType = "enemyBullet2";

    public tk2dSpriteAnimator gunArmAnim;
    public Transform gunFireAttachPt;
    public ParticleSystem gunFireSpark;

    public Transform[] moveWPs;
    public float moveGunFireDelay = 0.3f;
    public float moveGunAngleMax = 30.0f;
    public float moveGunAngleSpread = 15.0f;
    public float moveGunAngleSpreadDiv = 6.0f;

    public float attackStartDelay = 0.5f;
    public float attackGunFireDelay = 0.3f;
    public float attackGunAngleMax = 60.0f;
    public float attackGunAngleSpread = 20.0f;
    public float attackGunAngleSpreadDiv = 10.0f;
    public int attackCount = 5;

    public float idleDelay = 1.0f;

    public float jumpMinYVel = 0.1f;
    public float jumpDropYVel = 6.0f;
    public float jumpMinHeight = 3.0f;
    public float jumpDropDelay = 0.2f;
    public float jumpMoveScale = 1.5f;

    public SoundPlayer gunSfx;
    public SoundPlayer dashSfx;
    public SoundPlayer spellSfx;

    public const string armIdleClip = "gun";
    public const string armFireClip = "gunFire";

    public const string attackClip = "attack";
    public const string defeatClip = "defeated";
    public const string castClip = "cast";
    public const string jumpAttackClip = "kick";

    public const string timeWarpStartTake = "start";
    public const string timeWarpEndTake = "end";

    public const string nextPhaseFunc = "DoNextPhase";

    public const string moveRoutine = "DoMove";
    public const string sprayPrayRoutine = "DoSprayPray";
    public const string jumpRoutine = "DoJump";

    public const string timeWarpEndFunc = "DoTimeWarpEnd";

    private Phase mCurPhase = Phase.None;
    
    private Player mPlayer;
    private bool mFiring;
    private float mLastFireTime;
    private float mLastTimeWarpEndTime;
    private Phase mForceNextPhase = Phase.None;

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

        bodyCtrl.moveSide = 0;
        Jump(0);
        bodySpriteCtrl.StopOverrideClip();
                
        //Debug.Log("phase: " + phase);
        
        //prev
        switch(mCurPhase) {
            case Phase.Idle:
                CancelInvoke(nextPhaseFunc);
                break;

            case Phase.Move:
                gunFireSpark.Stop();
                gunFireSpark.Clear();

                dust.Stop();
                StopCoroutine(moveRoutine);
                gunArmAnim.gameObject.SetActive(false);
                break;

            case Phase.SprayNPray:
                gunFireSpark.Stop();
                gunFireSpark.Clear();

                StopCoroutine(sprayPrayRoutine);

                gunArmAnim.gameObject.SetActive(false);
                break;

            case Phase.Jump:
                bodyCtrl.moveScale = 1.0f;
                bodyCtrl.gravityController.enabled = true;
                StopCoroutine(jumpRoutine);
                break;
        }
        
        //new
        switch(phase) {
            case Phase.Idle:
                //wait till land if not grounded
                if(bodyCtrl.isGrounded)
                    Invoke(nextPhaseFunc, idleDelay);
                break;

            case Phase.Move:
                StartCoroutine(moveRoutine);
                break;

            case Phase.SprayNPray:
                StartCoroutine(sprayPrayRoutine);
                break;

            case Phase.Dead:
                break;

            case Phase.Cast:
                //wait till we land if not grounded
                if(bodyCtrl.isGrounded)
                    bodySpriteCtrl.PlayOverrideClip(castClip);
                break;

            case Phase.Jump:
                StartCoroutine(jumpRoutine);
                break;
        }
        
        mCurPhase = phase;
    }
    
    public override void SpawnFinish() {
        base.SpawnFinish();
    }

    protected override void OnStatsHPChange(Stats stat, float delta) {
        base.OnStatsHPChange(stat, delta);

        switch(mCurPhase) {
            case Phase.Move:
                //DoNextPhase();
                //ToPhase(Phase.Idle);
                break;
        }
    }
    
    protected override void Awake() {
        base.Awake();
        
        bodyCtrl.landCallback += OnLanded;
        
        bodySpriteCtrl.clipFinishCallback += OnAnimFinish;

        gunArmAnim.AnimationCompleted += OnGunArmAnimFinish;
        gunArmAnim.gameObject.SetActive(false);

        timeWarp.takeCompleteCallback += OnTimeWarpAnimFinish;
        timeWarp.gameObject.SetActive(false);
    }
    
    protected override void Start() {
        base.Start();
        
        mPlayer = Player.instance;
    }
    
    void FixedUpdate() {
        if(mCurPhase == Phase.None)
            return;

        switch(mCurPhase) {
            case Phase.Idle:
                //face player
                Vector3 playerPos = mPlayer.transform.position;
                Vector3 pos = transform.position;
                bodySpriteCtrl.isLeft = Mathf.Sign(playerPos.x - pos.x) < 0.0f;
                break;

            case Phase.Move:
            case Phase.SprayNPray:
                if(gunArmAnim.gameObject.activeSelf) {
                    gunArmAnim.Sprite.FlipX = bodySpriteCtrl.anim.Sprite.FlipX;
                }
                break;

            case Phase.Dead:
                if(bodyCtrl.isGrounded && bodySpriteCtrl.overrideClip == null)
                    bodySpriteCtrl.PlayOverrideClip(defeatClip);
                break;
        }
    }

    IEnumerator DoJump() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        //wait till we are on ground
        do {
            yield return wait;
        } while(!bodyCtrl.isGrounded);

        float lastGroundY = transform.position.y;

        //jump
        Jump(5.0f);

        /*public float jumpMinYVel = 0.1f;
    public float jumpDropYVel = 6.0f;
    jumpAttackClip
         */
        yield return new WaitForSeconds(0.1f); //wait a small bit

        //check if our velocity y is at min or player is in range
        while(true) {
            if(transform.position.y - lastGroundY >= jumpMinHeight) {
                if(!bodyCtrl.isGrounded && !bodyCtrl.isJump && bodyCtrl.localVelocity.y < jumpMinYVel)
                    break;

                Vector3 playerPos = mPlayer.collider.bounds.center;
                Bounds bounds = collider.bounds;

                if(playerPos.y >= bounds.min.y && playerPos.y <= bounds.max.y)
                    break;
            }

            if(bodyCtrl.isGrounded) { //something went wrong
                ToPhase(Phase.Idle);
                yield break;
            }
            else {
                yield return wait;
            }
        }

        //kick until we are somewhere near player X or we hit something
        bodySpriteCtrl.PlayOverrideClip(jumpAttackClip);
        bodySpriteCtrl.isLeft = Mathf.Sign(mPlayer.transform.position.x - transform.position.x) < 0.0f;

        bodyCtrl.rigidbody.velocity = Vector3.zero;
        bodyCtrl.gravityController.enabled = false;

        bodyCtrl.moveScale = jumpMoveScale;

        bodyCtrl.moveSide = bodySpriteCtrl.isLeft ? -1 : 1;

        dashSfx.Play();

        yield return new WaitForSeconds(jumpDropDelay);

        while(bodyCtrl.collisionFlags == CollisionFlags.None) {
            Vector3 playerPos = mPlayer.transform.position;
            Vector3 pos = transform.position;

            if(Mathf.Abs(playerPos.x - pos.x) <= 0.1f || Mathf.Sign(playerPos.x - pos.x) != bodyCtrl.moveSide)
                break;

            yield return wait;
        }

        //drop
        bodyCtrl.localVelocity = new Vector3(0, -jumpDropYVel, 0);

        ToPhase(Phase.Idle);
    }

    IEnumerator DoSprayPray() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        WaitForSeconds waitAttack = new WaitForSeconds(attackGunFireDelay);
        
        //wait till we are on ground
        do {
            yield return wait;
        } while(!bodyCtrl.isGrounded);
        
        Vector3 pos = collider.bounds.center;

        //face player
        Vector3 playerPos = mPlayer.collider.bounds.center;

        bodySpriteCtrl.PlayOverrideClip(attackClip);

        gunArmAnim.gameObject.SetActive(true);
        gunArmAnim.Play(armIdleClip);
        gunArmAnim.transform.localRotation = Quaternion.identity;

        mFiring = false;

        float startTime = Time.fixedTime;
        while(Time.fixedTime - startTime < attackStartDelay) {
            playerPos = mPlayer.collider.bounds.center;
            bodySpriteCtrl.isLeft = Mathf.Sign(playerPos.x - pos.x) < 0.0f;
            yield return wait;
        }

        Vector3 dir = playerPos - pos; dir.z = 0.0f; dir.Normalize();

        dir = M8.MathUtil.DirCap(new Vector3(bodySpriteCtrl.isLeft ? -1 : 1, 0, 0), dir, attackGunAngleMax);

        for(int i = 0; i < attackCount; i++) {
            Vector3 attackDir = Quaternion.AngleAxis(
                (Mathf.Round(Random.Range(-attackGunAngleSpreadDiv, attackGunAngleSpreadDiv))/attackGunAngleSpreadDiv)*attackGunAngleSpread, 
                Vector3.forward) * dir;

            gunArmAnim.transform.right = bodySpriteCtrl.isLeft ? -attackDir : attackDir;
            
            //fire proj with given dir
            Vector3 projPos = gunFireAttachPt.position; projPos.z = 0.0f;
            Projectile.Create(projGroup, gunProjType, projPos, attackDir, null);
            
            mFiring = true;
            gunArmAnim.Play(armFireClip);
            
            gunFireSpark.Play();

            gunSfx.Play();

            while(mFiring)
                yield return wait;

            yield return waitAttack;
        }

        ToPhase(Phase.Idle);
    }

    IEnumerator DoMove() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        //wait till we are on ground
        do {
            yield return wait;
        } while(!bodyCtrl.isGrounded);

        Vector3 pos = collider.bounds.center;

        //Debug.Log("fuck : "+pos.x);

        //get fartherst wp
        float farX = 0.0f;
        float farDistX = 0.0f;
        float farSignX = 0.0f;
        for(int i = 0, max = moveWPs.Length; i < max; i++) {
            float x = moveWPs[i].position.x;
            float dx = x - pos.x;
            float distX = Mathf.Abs(dx);
            if(distX > farDistX) {
                farDistX = distX;
                farX = x;
                farSignX = Mathf.Sign(dx);
            }
        }

        //Debug.Log("fuck far: "+farX);

        bodyCtrl.moveSide = farSignX;

        gunArmAnim.gameObject.SetActive(true);
        gunArmAnim.Play(armIdleClip);
        gunArmAnim.transform.localRotation = Quaternion.identity;

        mFiring = false;
        mLastFireTime = Time.fixedTime;

        dust.Play();

        dashSfx.Play();

        while(farSignX == bodyCtrl.moveSide) {
            pos = collider.bounds.center;
            farSignX = Mathf.Sign(farX - pos.x);

            if(!mFiring) {
                if(Time.fixedTime - mLastFireTime > moveGunFireDelay) {
                    //check if player is towards where we are moving
                    Vector3 playerPos = mPlayer.collider.bounds.center;
                    Vector3 dpos = playerPos - pos;

                    if(Mathf.Sign(dpos.x) == bodyCtrl.moveSide) {
                        //set angle
                        Vector3 dir = dpos; dir.z = 0.0f; dir.Normalize();
                        dir = Quaternion.AngleAxis(
                            (Mathf.Round(Random.Range(-moveGunAngleSpreadDiv, moveGunAngleSpreadDiv))/moveGunAngleSpreadDiv)*moveGunAngleSpread, 
                            Vector3.forward) * dir;

                        dir = M8.MathUtil.DirCap(new Vector3(bodyCtrl.moveSide, 0.0f, 0.0f), dir, moveGunAngleMax);

                        gunArmAnim.transform.right = bodySpriteCtrl.isLeft ? -dir : dir;

                        //fire proj with given dir
                        Vector3 projPos = gunFireAttachPt.position; projPos.z = 0.0f;
                        Projectile.Create(projGroup, gunProjType, projPos, dir, null);

                        mFiring = true;
                        gunArmAnim.Play(armFireClip);

                        gunFireSpark.Play();

                        gunSfx.Play();
                    }
                }
            }

            yield return wait;
        }

        ToPhase(Phase.Idle);
    }

    void DoNextPhase() {
        //activate timewarp if it's gone
        if(!timeWarp.gameObject.activeSelf && Time.fixedTime - mLastTimeWarpEndTime > timeWarpCooldown) {
            ToPhase(Phase.Cast);
        }
        else {
            if(mForceNextPhase != Phase.None) {
                Phase p = mForceNextPhase;
                mForceNextPhase = Phase.None;
                ToPhase(p);
            }
            else {
                //random shit
                int r = Random.Range(0, 6);
                switch(r) {
                    case 0:
                    case 1:
                    case 2:
                        ToPhase(Phase.Move);
                        break;
                    case 3:
                    case 4:
                        ToPhase(Phase.SprayNPray);
                        break;
                    case 5:
                        ToPhase(Phase.Jump);
                        break;
                }
            }
        }
    }

    void DoTimeWarpEnd() {
        timeWarp.Play(timeWarpEndTake);
    }

    void OnAnimFinish(PlatformerSpriteController ctrl, tk2dSpriteAnimationClip clip) {
        switch(mCurPhase) {
            case Phase.Cast:
                if(clip.name == castClip) {
                    //cast time warp

                    //get nearest spawn pt to player
                    Vector3 playerPos = mPlayer.collider.bounds.center;
                    Vector3 nearestPt = Vector3.zero;
                    float nearestDistSqr = Mathf.Infinity;
                    for(int i = 0; i < timeWarpSpawnPts.Length; i++) {
                        Vector3 p = timeWarpSpawnPts[i].position;
                        float distSqr = (p - playerPos).sqrMagnitude;
                        if(distSqr < nearestDistSqr) {
                            nearestPt = p;
                            nearestDistSqr = distSqr;
                        }
                    }

                    timeWarp.transform.position = nearestPt;
                    timeWarp.gameObject.SetActive(true);
                    timeWarp.Play(timeWarpStartTake);

                    spellSfx.Play();

                    mForceNextPhase = Phase.Jump;
                    ToPhase(Phase.SprayNPray);
                }
                break;
        }
    }

    void OnLanded(PlatformerController ctrl) {
        switch(mCurPhase) {
            case Phase.Idle:
                if(!IsInvoking(nextPhaseFunc))
                    Invoke(nextPhaseFunc, idleDelay);
                break;

            case Phase.Cast:
                bodySpriteCtrl.PlayOverrideClip(castClip);
                break;
        }
    }

    void OnGunArmAnimFinish(tk2dSpriteAnimator anim, tk2dSpriteAnimationClip clip) {
        anim.Play(armIdleClip);
        mFiring = false;
        mLastFireTime = Time.fixedTime;
    }

    void OnTimeWarpAnimFinish(AnimatorData animDat, AMTake take) {
        if(animDat == timeWarp) {
            if(take.name == timeWarpStartTake) {
                if(!IsInvoking(timeWarpEndFunc))
                    Invoke(timeWarpEndFunc, timeWarpExpireDelay);
            }
            else if(take.name == timeWarpEndTake) {
                timeWarp.gameObject.SetActive(false);
                mLastTimeWarpEndTime = Time.fixedTime;
            }
        }
    }
}
