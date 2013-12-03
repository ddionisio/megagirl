using UnityEngine;
using System.Collections;

public class EnemyBossCatGirl : Enemy {
    public enum Phase {
        None,
        Move,
        Jump,
        JumpToWall,
        ThrowGrenades,
        AirStrike,
        Attack,
        Dead
    }

    public const string attackWhipClip = "attack";
    public const string attackStrikeClipPrep = "wallstickPrepStrike";
    public const string attackStrikeClip = "strike";
    public const string attackProjClip = "wallstickThrow";
    public const string defeatClip = "defeated";

    public LayerMask attackMask;

    public Damage attackDamage;
    public Damage contactDamage;

    public float jumpToWallDelay = 4.0f;
    public Transform jumpToWallWP;

    public Transform airStrikeAttach;
    public float airStrikeOfs = 1f; //offset distance based on player direction
    public float airStrikeForce = 100;
    public float airStrikeRadius = 0.6f;

    public float attackWhipPlayerMinHeight = 0.5f;
    public float attackWhipPlayerAirMinHeight = 1.5f;
    public float attackWhipDist = 3f;
    public float attackWhipRadius = 0.2f;
    public Transform[] attackWhipPts; //should be 2

    public string projType;
    public Transform projAttach;
    public int projMax = 2;

    private Phase mCurPhase = Phase.None;

    private Player mPlayer;

    private bool mAttackActive;
    private float mStrikeLastTime;
    private Vector3 mStrikeDir;

    private float mLastJumpToWallTime;
    private bool mJumpToWallWait;
    private int mProjCounter;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                bodyCtrl.inputEnabled = false;
                
                ToPhase(Phase.None);
                break;
        }

        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                bodyCtrl.inputEnabled = true;

                mLastJumpToWallTime = Time.fixedTime;
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

        //Debug.Log("phase: " + phase);

        //prev
        switch(mCurPhase) {
            case Phase.JumpToWall:
                mLastJumpToWallTime = Time.fixedTime;
                break;

            case Phase.AirStrike:
                bodyCtrl.enabled = true;
                bodyCtrl.gravityController.enabled = true;

                bodySpriteCtrl.StopOverrideClip();
                //bodyCtrl.gravityController.enabled = true;
                break;
        }

        //new
        switch(phase) {
            case Phase.Move:
                Jump(0);
                break;

            case Phase.Attack:
                if(bodyCtrl.isGrounded)
                    bodyCtrl.rigidbody.velocity = Vector3.zero;

                Jump(0);

                bodyCtrl.moveSide = 0.0f;

                mAttackActive = false; //wait for frame trigger to activate

                bodySpriteCtrl.PlayOverrideClip(attackWhipClip);
                break;

            case Phase.Jump:
                bodyCtrl.moveSide = bodySpriteCtrl.isLeft ? -1.0f : 1.0f;
                Jump(1.0f);
                break;

            case Phase.AirStrike:
                Jump(0);
                mAttackActive = false;
                bodyCtrl.moveSide = 0;
                bodySpriteCtrl.PlayOverrideClip(attackStrikeClipPrep);
                break;

            case Phase.JumpToWall:
                Jump(0);
                mJumpToWallWait = false;
                break;

            case Phase.ThrowGrenades:
                mProjCounter = 0;
                bodyCtrl.moveSide = 0;
                bodySpriteCtrl.PlayOverrideClip(attackProjClip);
                break;

            case Phase.Dead:
                bodySpriteCtrl.StopOverrideClip();
                break;
        }

        mCurPhase = phase;
    }

    public override void SpawnFinish() {
        base.SpawnFinish();
    }

    protected override void Awake() {
        base.Awake();

        bodyCtrl.landCallback += OnLanded;

        bodySpriteCtrl.clipFinishCallback += OnAnimFinish;
        bodySpriteCtrl.clipFrameEventCallback += OnAnimFrameEvent;
    }

    protected override void Start() {
        base.Start();

        mPlayer = Player.instance;
    }

    void FixedUpdate() {
        if(mCurPhase == Phase.None)
            return;

        Vector3 playerPos = mPlayer.collider.bounds.center;
        Vector3 pos = collider.bounds.center;
        float deltaPlayerX = playerPos.x - pos.x;
        float playerDistX = Mathf.Abs(deltaPlayerX);
        float toPlayerDirX = mPlayer.state == (int)EntityState.Dead ? 0.0f : Mathf.Sign(deltaPlayerX);
        float playerDistY = Mathf.Abs(playerPos.y - pos.y);

        switch(mCurPhase) {
            case Phase.Move:
                if(bodyCtrl.moveSide != toPlayerDirX) {
                    if(bodyCtrl.isGrounded)
                        bodyCtrl.rigidbody.velocity = Vector3.zero;

                    bodyCtrl.moveSide = toPlayerDirX;
                }

                if(playerDistY > attackWhipPlayerMinHeight) {
                    //jump
                    if(playerPos.y > pos.y)
                        ToPhase(Phase.Jump);
                }
                else if(playerDistX <= attackWhipDist) {
                    bodySpriteCtrl.isLeft = toPlayerDirX < 0.0f;
                    ToPhase(Phase.Attack);
                }
                
                if(Time.fixedTime - mLastJumpToWallTime > jumpToWallDelay)
                    ToPhase(Phase.JumpToWall);
                break;

            case Phase.Jump:
                if(bodyCtrl.isWallStick) {
                    ToPhase(Phase.AirStrike);
                }
                else if(playerDistY <= attackWhipPlayerAirMinHeight && playerDistX <= attackWhipDist) {
                    bodySpriteCtrl.isLeft = toPlayerDirX < 0.0f;
                    ToPhase(Phase.Attack);
                }
                else if(!bodyCtrl.isJump && bodyCtrl.isGrounded) {
                    ToPhase(Phase.Move);
                }
                break;

            case Phase.Attack:
                if(mAttackActive) {
                    Vector3 pt1 = attackWhipPts[0].position; pt1.z = 0;
                    Vector3 pt2 = attackWhipPts[1].position; pt2.z = 0;

                    Vector3 dir = pt2 - pt1;
                    float dist = dir.magnitude;
                    dir /= dist;

                    RaycastHit hit;
                    if(Physics.SphereCast(pt1, attackWhipRadius, dir, out hit, dist, attackMask)) {
                        attackDamage.CallDamageTo(hit.collider.gameObject, hit.point, hit.normal);
                    }
                }

                if(bodyCtrl.isWallStick) {
                    ToPhase(Phase.AirStrike);
                }
                break;

            case Phase.AirStrike:
                if(mAttackActive) {
                    rigidbody.AddForce(mStrikeDir * airStrikeForce, ForceMode.Force);

                    Vector3 vel = rigidbody.velocity;
                    float spdSqr = vel.sqrMagnitude;
                    if(spdSqr > 900.0f) {
                        rigidbody.velocity = (vel / Mathf.Sqrt(spdSqr)) * 30.0f;
                    }

                    bodySpriteCtrl.isLeft = vel.x < 0.0f;

                    Vector3 atkPt = airStrikeAttach.position; atkPt.z = 0.0f;
                    Collider[] cols = Physics.OverlapSphere(atkPt, airStrikeRadius, attackMask);
                    if(cols.Length > 0 && cols[0] == mPlayer.collider)
                        contactDamage.CallDamageTo(mPlayer.gameObject, playerPos, (playerPos - pos).normalized);
                }
                else {
                    if(bodyCtrl.isWallStick) {
                        bodySpriteCtrl.isLeft = M8.MathUtil.CheckSide(bodyCtrl.wallStickCollide.normal, bodyCtrl.dirHolder.up) == M8.MathUtil.Side.Right;
                    }
                }
                break;

            case Phase.JumpToWall:
                if(mJumpToWallWait) {
                    if(bodyCtrl.isWallStick) {
                        //strike or grenade
                        if(mPlayer.controller.isGrounded) {
                            if(Random.Range(0, 3) == 0)
                                ToPhase(Phase.AirStrike);
                            else
                                ToPhase(Phase.ThrowGrenades);
                        }
                        else
                            ToPhase(Phase.AirStrike);
                        /*if(Random.Range(0, 2) == 0)
                            ToPhase(Phase.AirStrike);
                        else
                            ToPhase(Phase.ThrowGrenades);*/
                        //ToPhase(Phase.ThrowGrenades);
                    }
                    else if(bodyCtrl.isGrounded && !bodyCtrl.isJump) {
                        Jump(0);
                        mJumpToWallWait = false;
                    }
                }
                else {
                    Vector3 wp = jumpToWallWP.position;
                    float wpDeltaX = wp.x - pos.x;
                    float wpDistX = Mathf.Abs(wpDeltaX);
                    float wpDirX = Mathf.Sign(wpDeltaX);

                    if(bodyCtrl.isGrounded) {
                        if(wpDistX <= 0.2f) {
                            bodyCtrl.moveSide = 1.0f;
                            Jump(2.0f);
                            mJumpToWallWait = true;
                        }
                        else {
                            bodyCtrl.moveSide = wpDirX;
                        }
                    }
                    else if(bodyCtrl.isWallStick) {
                        bodyCtrl.moveSide = 0.0f;
                        mJumpToWallWait = true;
                    }
                }
                break;

            case Phase.Dead:
                if(bodyCtrl.isGrounded && bodySpriteCtrl.overrideClip == null)
                    bodySpriteCtrl.PlayOverrideClip(defeatClip);
                break;
        }
    }

    void OnAnimFinish(PlatformerSpriteController ctrl, tk2dSpriteAnimationClip clip) {
        switch(mCurPhase) {
            case Phase.Attack:
                if(clip.name == attackWhipClip) {
                    //todo: random pattern
                    ToPhase(Phase.Move);
                }
                break;

            case Phase.AirStrike:
                if(clip.name == attackStrikeClipPrep) {
                    bodySpriteCtrl.PlayOverrideClip(attackStrikeClip);

                    bodyCtrl.enabled = false;
                    bodyCtrl.gravityController.enabled = false;

                    rigidbody.velocity = Vector3.zero;
                    rigidbody.drag = 0.0f;

                    //fly towards player's current pos
                    Vector3 playerPos = mPlayer.collider.bounds.center;
                    Vector3 pos = collider.bounds.center;

                    if(!mPlayer.controller.isGrounded) {
                        pos += mPlayer.rigidbody.velocity.normalized * airStrikeOfs;
                    }
                    else {
                        pos.x += mPlayer.controller.moveSide * airStrikeOfs;
                    }

                    mStrikeDir = (playerPos - pos).normalized;

                    mAttackActive = true;
                    mStrikeLastTime = Time.fixedTime;
                }
                break;

            case Phase.ThrowGrenades:
                if(clip.name == attackProjClip) {
                    Vector3 playerPos = mPlayer.collider.bounds.center;
                    Vector3 pos = collider.bounds.center;
                    Vector3 posLaunch = projAttach.position; posLaunch.z = 0.0f;
                    Projectile.Create(projGroup, projType, posLaunch, (playerPos - pos).normalized, mPlayer.transform);
                    mProjCounter++;
                    if(mProjCounter >= projMax)
                        ToPhase(Phase.Move);
                    else {
                        bodySpriteCtrl.PlayOverrideClip(attackProjClip);
                    }
                }
                break;
        }
    }

    void OnCollisionStay(Collision col) {
        if(mCurPhase == Phase.AirStrike) {
            if(mAttackActive && Time.fixedTime - mStrikeLastTime > 0.2f) {
                ToPhase(Phase.Move);
            }
        }
    }

    void OnAnimFrameEvent(PlatformerSpriteController ctrl, tk2dSpriteAnimationClip clip, int frame) {
        tk2dSpriteAnimationFrame frameDat = clip.frames[frame];
        switch(mCurPhase) {
            case Phase.Attack:
                if(clip.name == attackWhipClip) {
                    mAttackActive = frameDat.eventInt == 1;
                }
                break;
        }
    }

    void OnLanded(PlatformerController ctrl) {
        switch(mCurPhase) {
            case Phase.Jump:
            case Phase.AirStrike:
            case Phase.ThrowGrenades:
                Jump(0);
                ToPhase(Phase.Move);
                break;
        }
    }
}
