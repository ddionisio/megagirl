using UnityEngine;
using System.Collections;

public class EnemyBossCloneGirl : Enemy {
    public enum Phase {
        None,
        Move,
        Dead
    }

    public const string defeatClip = "defeated";

    public TransAnimWaveOfsRand shaker;
    public int maxWallJump = 3;

    public string angryChaserType = "angryClone";
    public int angryChaserCount = 3;
    public float angryChaserLaunchDelay = 2.0f;
    public Vector3 chaserOfs;

    private const string ChaserLaunchFunc = "DoChaserLaunch";

    private Phase mCurPhase = Phase.None;

    private EntitySensor mSensor;

    private Player mPlayer;
    private int mCurWallJumpCount;
    private int mCurChaserCount;

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
            case Phase.Move:
                mSensor.Activate(false);
                bodyCtrl.moveSide = 0.0f;

                CancelInvoke(ChaserLaunchFunc);
                break;
        }

        switch(phase) {
            case Phase.Move:
                mCurWallJumpCount = 0;
                mSensor.Activate(true);

                if(!IsInvoking(ChaserLaunchFunc))
                    InvokeRepeating(ChaserLaunchFunc, 0.0f, angryChaserLaunchDelay);
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
    }

    protected override void Start() {
        base.Start();
        
        mPlayer = Player.instance;
    }
    
    void FixedUpdate() {
        if(mCurPhase == Phase.None)
            return;

        switch(mCurPhase) {
            case Phase.Move:
                mSensor.hFlip = bodySpriteCtrl.isLeft;

                Vector3 playerPos = mPlayer.transform.position;
                Vector3 pos = transform.position;
                Vector3 dpos = playerPos - pos;

                if(bodyCtrl.isGrounded) {
                    mCurWallJumpCount = 0;

                    //check if player is within y range
                    Bounds playerBounds = mPlayer.collider.bounds;
                    float playerYC = playerBounds.center.y;
                    float playerYMin = playerBounds.min.y;
                    float playerYMax = playerBounds.max.y;

                    Bounds bounds = collider.bounds;
                    float yMin = bounds.min.y;
                    float yMax = bounds.max.y;

                    if((playerYMin >= yMin && playerYMin <= yMax) 
                       || (playerYMax >= yMin && playerYMax <= yMax)
                       || (playerYC >= yMin && playerYC <= yMax)) {
                        shaker.enabled = false;
                        bodyCtrl.moveSide = -Mathf.Sign(dpos.x);
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
            if(mCurChaserCount == 0) {
                if(!IsInvoking(ChaserLaunchFunc))
                    InvokeRepeating(ChaserLaunchFunc, angryChaserLaunchDelay, angryChaserLaunchDelay);
            }
        }
    }
}
