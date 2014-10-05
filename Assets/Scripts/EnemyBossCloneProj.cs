using UnityEngine;
using System.Collections;

public class EnemyBossCloneProj : Projectile {
    public float followPlayerDelay;
    public DamageTrigger damageTrigger;
    public TransAnimWaveOfsRand shaker;

    private PlatformerController mCtrl;
    private PlatformerSpriteController mCtrlSpr;
    private Player mPlayer;
    private float mLastFollowPlayerTime;
    private float mDefaultDieDelay;
    private float mLastJumpTime;

	protected override void StateChanged() {
        switch((State)prevState) {
            case State.Active:
                mCtrl.Jump(false);
                mCtrl.moveSide = 0.0f;
                break;
        }

        base.StateChanged();

        switch((State)state) {
            case State.Active:
                //mCtrl.gravityController.enabled = true;
                //mCtrl.enabled = true;
                mCtrl.moveSideLock = true;
                mCtrl.moveEnabled = true;
                mCtrl.ResetCollision();
                dieDelay = mDefaultDieDelay;
                mLastFollowPlayerTime = 0;
                mLastJumpTime = Time.fixedTime;
                break;

            case State.Dying:
                shaker.enabled = true;
                mCtrl.moveSide = 0.0f;
                mCtrl.ResetCollision();
                Vector3 lv = mCtrl.localVelocity;
                lv.x = 0.0f;
                mCtrl.localVelocity = lv;
                break;

            case State.Invalid:
                //mCtrl.gravityController.enabled = false;
                //mCtrl.enabled = false;
                shaker.enabled = false;
                break;
        }
    }

    protected override void SpawnStart() {
        base.SpawnStart();

        shaker.enabled = false;

        rigidbody.detectCollisions = true;
        collider.enabled = true;
    }

    protected override void Awake() {
        mDefaultDieDelay = dieDelay;

        base.Awake();

        mCtrl = GetComponent<PlatformerController>();
        //mCtrl.gravityController.enabled = false;
        //mCtrl.enabled = false;

        mCtrlSpr = GetComponent<PlatformerSpriteController>();

        damageTrigger.damageCallback += OnDamageDealt;

        shaker.enabled = false;
    }

    protected override void Start() {
        base.Start();

        mPlayer = Player.instance;

        rigidbody.detectCollisions = true;
        collider.enabled = true;
    }

    protected override void FixedUpdate() {
        switch((State)state) {
            case State.Active:
                if(Time.fixedTime - mLastFollowPlayerTime > followPlayerDelay) {
                    if(mCtrl.isGrounded) {
                        Vector3 playerPos = mPlayer.transform.position;
                        Vector3 pos = transform.position;
                        mCtrl.moveSide = Mathf.Sign(playerPos.x - pos.x);
                        mCtrlSpr.isLeft = mCtrl.moveSide < 0.0f;

                        mLastFollowPlayerTime = Time.fixedTime;
                    }
                }

                if(mCtrl.isWallStick) {
                    if(Time.fixedTime - mCtrl.wallStickLastTime > 0.15f) {
                        Vector3 playerMin = mPlayer.collider.bounds.min;
                        Vector3 bMax = collider.bounds.max;

                        if(playerMin.y > bMax.y) {
                            mCtrl.Jump(false);
                            mCtrl.Jump(true);
                        }

                        Vector3 playerPos = mPlayer.transform.position;
                        Vector3 pos = transform.position;
                        mCtrl.moveSide = Mathf.Sign(playerPos.x - pos.x);
                    }
                }
                else {
                    if(mCtrl.isGrounded && !mCtrl.isJump && Time.fixedTime - mLastJumpTime > 1.0f) {
                        //if higher, jump
                        Vector3 playerMin = mPlayer.collider.bounds.min;
                        Vector3 bMax = collider.bounds.max;
                        if(playerMin.y > bMax.y) {
                            mCtrl.Jump(false);
                            mCtrl.Jump(true);
                            mLastJumpTime = Time.fixedTime;
                        }
                    }

                    mCtrlSpr.isLeft = mCtrl.moveSide < 0.0f;
                }
                break;
        }
    }

    void OnDamageDealt(DamageTrigger trigger, GameObject victim) {
        if(isAlive) {
            dieDelay = 0.0f;
            
            if(stats) {
                stats.curHP = 0.0f;
            }
            else {
                state = (int)State.Dying;
            }
        }
    }
}
