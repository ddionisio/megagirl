using UnityEngine;
using System.Collections;

public class EnemyBossLightning : Enemy {
    public enum Phase {
        None,
        Move,
        MidAirCharge,
        TargetStrike,
        Dead
    }

    public const string chargeClip = "charge";
    public const string chargeMidAirClip = "chargeUp";
    public const string defeatClip = "defeated";
    public const string moveToPlayerFunc = "MoveToPlayer";

    public const string lightningBallGroup = "projBossLightning";
    public const string lightningBallType = "enemyLightningBall";

    public const string moveLightningStrikeFunc = "DoMoveLightningStrike";
    public const string targetStrikeFunc = "DoTargetStrike";

    public const string superStrikeFunc = "DoSuperStrike";

    public Transform fork;

    public float strikeCount = 3;

    public float moveFacePlayerDelay = 0.5f;
    public GameObject movePrepareGO;
    public GameObject lightningStrike;
    public float moveStrikeStartDelay = 0.5f;
    public float moveStrikePrepDelay = 1.0f;
    public float moveLightningActiveDelay = 0.3f;

    public GameObject targetStrikeGO;
    public float targetStrikePulse = 1.0f;
    public float targetStrikeReadyPulse = 6.0f;
    public float targetStrikeStartDelay = 2.0f;
    public float targetStrikeRepeatDelay = 0.5f;
    public float targetStrikeReadyDelay = 1.0f;
    public float targetStrikeActiveDelay = 0.3f;

    public Transform superStrikeDest;
    public GameObject superStrikeChargeGO;
    public GameObject superStrikeHolderGO;
    public GameObject superStrikeTargetGO;
    public GameObject superStrikeGO;
    public float superStrikeReadyDelay = 1.0f;
    public float superStrikeActiveDelay = 4.0f;

    private Phase mCurPhase = Phase.None;

    private Player mPlayer;
    private bool mInitialMoveStrike;
    private bool mTargetStrikeIsSeek = false;
    private Phase mNextPhase = Phase.TargetStrike;

    private SpriteColorPulse mTargetStrikePulse;
    private TransAnimSpinner mSuperStrikeSpinner;

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
                mNextPhase = Phase.TargetStrike;
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
        
        //Debug.Log("phase: " + phase);
        Jump(0);
        bodySpriteCtrl.StopOverrideClip();
        
        //prev
        switch(mCurPhase) {
            case Phase.Move:
                CancelInvoke(moveToPlayerFunc);
                StopCoroutine(moveLightningStrikeFunc);

                movePrepareGO.SetActive(false);
                lightningStrike.SetActive(false);
                break;

            case Phase.MidAirCharge:
                StopCoroutine(superStrikeFunc);

                superStrikeChargeGO.SetActive(false);
                superStrikeHolderGO.SetActive(false);
                superStrikeTargetGO.SetActive(false);
                superStrikeGO.SetActive(false);

                bodyCtrl.rigidbody.isKinematic = false;
                bodyCtrl.enabled = true;
                gravityCtrl.enabled = true;
                break;

            case Phase.TargetStrike:
                StopCoroutine(targetStrikeFunc);

                movePrepareGO.SetActive(false);
                targetStrikeGO.SetActive(false);
                lightningStrike.SetActive(false);
                break;
        }

        switch(phase) {
            case Phase.Move:
                if(bodyCtrl.isGrounded) {
                    if(!IsInvoking(moveToPlayerFunc))
                        InvokeRepeating(moveToPlayerFunc, 0.0f, moveFacePlayerDelay);
                }
                else {
                    bodyCtrl.moveSide = 0.0f; //wait till we land
                }

                StartCoroutine(moveLightningStrikeFunc);
                break;

            case Phase.TargetStrike:
                StartCoroutine(targetStrikeFunc);
                break;

            case Phase.MidAirCharge:
                StartCoroutine(superStrikeFunc);
                break;

            case Phase.Dead:
                bodyCtrl.moveSide = 0.0f;
                Vector3 vel = bodyCtrl.localVelocity;
                vel.x = 0;
                bodyCtrl.localVelocity = vel;
                break;
        }

        mCurPhase = phase;
    }

    public override void SpawnFinish() {
        mInitialMoveStrike = true;

        base.SpawnFinish();
    }
    
    protected override void Awake() {
        base.Awake();

        bodyCtrl.landCallback += OnLanded;

        movePrepareGO.SetActive(false);
        lightningStrike.SetActive(false);

        mTargetStrikePulse = targetStrikeGO.GetComponent<SpriteColorPulse>();
        targetStrikeGO.SetActive(false);

        superStrikeChargeGO.SetActive(false);

        mSuperStrikeSpinner = superStrikeHolderGO.GetComponent<TransAnimSpinner>();
        superStrikeHolderGO.SetActive(false);

        superStrikeTargetGO.SetActive(false);
        superStrikeGO.SetActive(false);
    }

    protected override void Start() {
        base.Start();

        mPlayer = Player.instance;
    }

    void FixedUpdate() {
        if(mCurPhase == Phase.None)
            return;

        //Vector3 playerPos = mPlayer.collider.bounds.center;

        switch(mCurPhase) {
            case Phase.TargetStrike:
                if(targetStrikeGO.activeSelf) {
                    Vector3 forkPos = fork.position; forkPos.z = targetStrikeGO.transform.position.z;
                    targetStrikeGO.transform.position = forkPos;
                }

                if(mTargetStrikeIsSeek) {
                    //face player
                    Vector3 dpos = mPlayer.transform.position - transform.position;
                    bodySpriteCtrl.isLeft = dpos.x < 0.0f;

                    Vector3 seekDir = mPlayer.collider.bounds.center - fork.position; seekDir.z = 0.0f;
                    seekDir.Normalize();

                    targetStrikeGO.transform.right = seekDir;
                }
                break;

            case Phase.Dead:
                if(bodyCtrl.isGrounded && bodySpriteCtrl.overrideClip == null)
                    bodySpriteCtrl.PlayOverrideClip(defeatClip);
                break;
        }
    }

    void MoveToPlayer() {
        if(!lightningStrike.activeSelf) {
            Vector3 dpos = mPlayer.transform.position - transform.position;
            float s = Mathf.Sign(dpos.x);
            if(bodyCtrl.moveSide != s) {
                bodyCtrl.moveSide = s;
            }
        }
    }

    void OnLanded(PlatformerController ctrl) {
        switch(mCurPhase) {
            case Phase.Move:
                if(!IsInvoking(moveToPlayerFunc))
                    InvokeRepeating(moveToPlayerFunc, 0.0f, moveFacePlayerDelay);
                break;
        }
    }

    void OnMoveSensorUpdate(EntitySensor sensor) {
        switch(mCurPhase) {
            case Phase.Move:
                if(sensor.isHit) {
                    bodyCtrl.moveSide *= -1;
                    bodySpriteCtrl.isLeft = !bodySpriteCtrl.isLeft;
                }
                break;
        }
    }

    IEnumerator DoSuperStrike() {
        WaitForFixedUpdate waitUpdate = new WaitForFixedUpdate();

        Vector3 dest = superStrikeDest.position;

        //move towards target
        while(true) {
            yield return waitUpdate;

            float delta = dest.x - transform.position.x;
            if(Mathf.Abs(delta) > 0.1f) {
                bodyCtrl.moveSide = Mathf.Sign(delta);
            }
            else
                break;
        }

        bodyCtrl.moveSide = 0.0f;
        bodyCtrl.rigidbody.velocity = Vector3.zero;

        //jump
        Jump(bodyCtrl.jumpDelay);
        while(Mathf.Abs(dest.y - collider.bounds.center.y) > 0.1f) {
            yield return waitUpdate;
        }
        Jump(0);

        bodyCtrl.enabled = false;
        gravityCtrl.enabled = false;
        bodyCtrl.rigidbody.isKinematic = true;

        bodySpriteCtrl.PlayOverrideClip("chargeUp");

        superStrikeChargeGO.SetActive(true);

        superStrikeHolderGO.SetActive(true);
        Vector3 pos = collider.bounds.center; pos.z = 0;
        superStrikeHolderGO.transform.position = pos;
        superStrikeHolderGO.transform.eulerAngles = new Vector3(0,0,45);

        //prep
        superStrikeTargetGO.SetActive(true);
        yield return new WaitForSeconds(superStrikeReadyDelay);
        superStrikeTargetGO.SetActive(false);

        //attack
        superStrikeGO.SetActive(true);
        yield return new WaitForSeconds(superStrikeActiveDelay);

        mSuperStrikeSpinner.rotatePerSecond *= -1;

        if(mCurPhase == Phase.MidAirCharge) {
            mNextPhase = Phase.TargetStrike;
            ToPhase(Phase.Move);
        }
    }

    IEnumerator DoTargetStrike() {
        WaitForSeconds startWait = new WaitForSeconds(targetStrikeStartDelay);
        WaitForSeconds repeatWait = new WaitForSeconds(targetStrikeRepeatDelay);
        WaitForSeconds prepWait = new WaitForSeconds(targetStrikeReadyDelay);
        WaitForSeconds lightningWait = new WaitForSeconds(targetStrikeActiveDelay);

        int count = 0;

        bodySpriteCtrl.PlayOverrideClip(chargeClip);

        movePrepareGO.SetActive(true);

        bodyCtrl.rigidbody.velocity = Vector3.zero;
        bodyCtrl.moveSide = 0.0f;

        do {
            //acquire
            mTargetStrikeIsSeek = true;
            mTargetStrikePulse.pulsePerSecond = targetStrikePulse;
            targetStrikeGO.SetActive(true);
            yield return count == 0 ? startWait : repeatWait;

            //lock
            mTargetStrikeIsSeek = false;
            mTargetStrikePulse.pulsePerSecond = targetStrikeReadyPulse;
            yield return prepWait;

            //fire
            targetStrikeGO.SetActive(false);

            Vector3 lightningPos = fork.position; lightningPos.z = 0.0f;
            lightningStrike.transform.position = lightningPos;
            lightningStrike.transform.up = targetStrikeGO.transform.right;

            lightningStrike.SetActive(true);
            yield return lightningWait;
            lightningStrike.SetActive(false);

            count++;
        } while(mCurPhase == Phase.TargetStrike && count < strikeCount);

        if(mCurPhase == Phase.TargetStrike) {
            mNextPhase = Phase.MidAirCharge;
            ToPhase(Phase.Move);
        }
    }

    IEnumerator DoMoveLightningStrike() {
        WaitForSeconds startWait = new WaitForSeconds(moveStrikeStartDelay);
        WaitForSeconds prepWait = new WaitForSeconds(moveStrikePrepDelay);
        WaitForSeconds lightningWait = new WaitForSeconds(moveLightningActiveDelay);

        int count = 0;

        do {
            if(mInitialMoveStrike) {
                mInitialMoveStrike = false;

                movePrepareGO.SetActive(true);
                yield return new WaitForSeconds(0.5f);
            }
            else {
                movePrepareGO.SetActive(false);
                yield return startWait;

                if(count == strikeCount)
                    break;

                //sparky
                movePrepareGO.SetActive(true);
                yield return prepWait;
            }

            //strike
            //note: assume anchor is bottom
            Vector3 lightningPos = fork.position; lightningPos.z = 0.0f;
            lightningStrike.transform.position = lightningPos;
            lightningStrike.transform.rotation = Quaternion.identity;

            bodyCtrl.rigidbody.velocity = Vector3.zero;
            bodyCtrl.moveSide = 0.0f;
            bodySpriteCtrl.PlayOverrideClip(chargeClip);

            lightningStrike.SetActive(true);
            yield return lightningWait;
            lightningStrike.SetActive(false);

            bodySpriteCtrl.StopOverrideClip();

            //balls
            Vector3 ballProjPos = fork.position; ballProjPos.z = 0;

            Projectile proj = Projectile.Create(lightningBallGroup, lightningBallType, ballProjPos, Vector3.left, null);
            proj.bounceRotateAngle = -90;

            proj = Projectile.Create(lightningBallGroup, lightningBallType, ballProjPos, Vector3.right, null);
            proj.bounceRotateAngle = 90;

            count++;
        } while(mCurPhase == Phase.Move);

        if(mCurPhase == Phase.Move) {
            //switch between target strike and air strike
            ToPhase(mNextPhase);
        }
    }
}
