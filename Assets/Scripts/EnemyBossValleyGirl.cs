using UnityEngine;
using System.Collections;

public class EnemyBossValleyGirl : Enemy {
    public enum Phase {
        None,
        Idle,
        ShootLasers,
        ChargeToPlayer,
        Tornado,
        Dead
    }

    public const string lazorsClip = "laser";
    public const string defeatClip = "defeated";

    public float idleWait = 1.0f;

    public AnimatorData tornadoAnimDat;
    public float tornadoDuration = 4.0f;

    public GameObject damageGO;

    public GameObject chargeGO;
    public float chargeTurnDelay;
    public float chargeDuration;
    public float chargeMoveScale = 1.0f;
    public float chargeJumpDelay = 0.2f;
    public float chargeJumpStopDelay = 0.1f;

    public string laserProjType;
    public Transform[] laserPts;
    public float laserStartDelay = 1.0f;
    public float laserRepeatDelay = 1.0f;
    public int laserCount = 3;
    public float laserAngleRand = 15.0f;

    public Transform[] moveRandomWPs; //use for randomly moving in X

    public SoundPlayer twirlSfx;
    public SoundPlayer tornadoSfx;
    public SoundPlayer laserSfx;

    private const string tornadoRoutine = "DoTornado";
    private const string chargeRoutine = "DoCharge";
    private const string shootLasersRoutine = "DoLazors";
    private const string nextPhaseFunc = "DoNextPhase";

    private Phase mCurPhase = Phase.None;
    private Phase mNextPhase;
    private int mPhaseCounter = 0;
    
    private Player mPlayer;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                bodyCtrl.moveEnabled = false;
                
                ToPhase(Phase.None);
                break;
        }

        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                bodyCtrl.moveEnabled = true;
                mPhaseCounter = 0;
                mNextPhase = Phase.ChargeToPlayer;
                ToPhase(Phase.Idle);
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
        
        //prev
        switch(mCurPhase) {
            case Phase.Idle:
                CancelInvoke(nextPhaseFunc);
                break;

            case Phase.ChargeToPlayer:
                StopCoroutine(chargeRoutine);
                chargeGO.SetActive(false);
                bodyCtrl.lockDragOverrideCount = 0;
                bodyCtrl.moveScale = 1.0f;

                damageGO.SetActive(true);
                break;

            case Phase.ShootLasers:
                StopCoroutine(shootLasersRoutine);

                foreach(Transform t in laserPts)
                    t.gameObject.SetActive(false);
                break;

            case Phase.Tornado:
                stats.damageReduction = 0.0f;
                StopCoroutine(tornadoRoutine);
                tornadoAnimDat.Stop();
                bodyCtrl.lockDragOverrideCount = 0;
                break;
        }

        //new
        switch(phase) {
            case Phase.Idle:
                bodyCtrl.moveSide = 0.0f;
                //wait if we are in mid air
                if(bodyCtrl.isGrounded) {
                    Invoke(nextPhaseFunc, idleWait);
                }
                break;

            case Phase.ChargeToPlayer:
                bodyCtrl.moveScale = chargeMoveScale;
                damageGO.SetActive(false);

                StartCoroutine(chargeRoutine);
                break;

            case Phase.ShootLasers:
                StartCoroutine(shootLasersRoutine);
                break;

            case Phase.Tornado:
                StartCoroutine(tornadoRoutine);
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
        
        bodyCtrl.landCallback += OnLanded;
    }

    protected override void Start() {
        base.Start();
        
        mPlayer = Player.instance;

        foreach(Transform t in laserPts)
            t.gameObject.SetActive(false);

        chargeGO.SetActive(false);
    }
    
    void FixedUpdate() {
        if(mCurPhase == Phase.None)
            return;

        switch(mCurPhase) {
            case Phase.Dead:
                if(bodyCtrl.isGrounded && bodySpriteCtrl.overrideClip == null)
                    bodySpriteCtrl.PlayOverrideClip(defeatClip);
                break;
        }
    }

    float RandomX() {
        return Random.Range(moveRandomWPs[0].position.x, moveRandomWPs[1].position.x);
    }

    //true if moving
    bool MoveTowardsX(float toX) {
        float x = GetComponent<Collider>().bounds.center.x;
        float dx = toX - x;
        if(Mathf.Abs(dx) > 0.15f) {
            bodyCtrl.moveSide = Mathf.Sign(dx);
            return true;
        }
        else {
            bodyCtrl.moveSide = 0.0f;
            return false;
        }
    }

    void FacePlayer() {
        float x = transform.position.x;
        float playerX = mPlayer.transform.position.x;
        bodySpriteCtrl.isLeft = Mathf.Sign(playerX - x) < 0.0f;
    }

    IEnumerator DoTornado() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        //move to a random location
        float destX = RandomX();
        
        while(MoveTowardsX(destX))
            yield return wait;

        stats.damageReduction = 1.0f;

        tornadoAnimDat.Play("active");

        bodyCtrl.moveSide = 1.0f;

        float curTime = 0.0f;

        bodyCtrl.lockDragOverrideCount = 1;
        bodyCtrl.GetComponent<Rigidbody>().drag = 0;

        tornadoSfx.Play();

        while(curTime < tornadoDuration) {
            Vector3 tornadoPos = tornadoAnimDat.transform.position;
            Vector3 pos = transform.position;
            tornadoPos.x = pos.x;
            tornadoAnimDat.transform.position = tornadoPos;

            bodyCtrl.moveSide = Mathf.Sign(destX - pos.x);

            curTime += Time.fixedDeltaTime;
            yield return wait;
        }

        ToPhase(Phase.Idle);
    }

    IEnumerator DoCharge() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        WaitForSeconds waitJump = new WaitForSeconds(chargeJumpDelay);

        chargeGO.SetActive(true);

        float lastJumpTime = 0.0f;
        float lastTurnTime = 0.0f;
        float curTime = 0.0f;

        bodyCtrl.lockDragOverrideCount = 1;
        bodyCtrl.GetComponent<Rigidbody>().drag = 0.0f;

        twirlSfx.Play();

        while(curTime < chargeDuration) {
            if(Time.fixedTime - lastTurnTime > chargeTurnDelay) {
                FacePlayer();
                bodyCtrl.moveSide = bodySpriteCtrl.isLeft ? -1.0f : 1.0f;
                lastTurnTime = Time.fixedTime;
            }

            Vector3 playerPos = mPlayer.transform.position;
            Vector3 pos = transform.position;
            if(bodyCtrl.isGrounded) {
                if(playerPos.y > pos.y) {
                    yield return waitJump;

                    lastJumpTime = Time.fixedTime;
                    Jump(2.0f);
                }
            }
            else {
                if(playerPos.y < pos.y && Time.fixedTime - lastJumpTime > chargeJumpStopDelay)
                    Jump(0);
            }

            curTime += Time.fixedDeltaTime;

            yield return wait;
        }

        ToPhase(Phase.Idle);
    }

    IEnumerator DoLazors() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        WaitForSeconds waitLaser = new WaitForSeconds(laserRepeatDelay);

        //move to a random location
        float destX = RandomX();

        while(MoveTowardsX(destX))
            yield return wait;

        FacePlayer();

        bodySpriteCtrl.PlayOverrideClip(lazorsClip);

        foreach(Transform t in laserPts)
            t.gameObject.SetActive(true);

        yield return new WaitForSeconds(laserStartDelay);


        for(int i = 0; i < laserCount; i++) {
            Vector3 playerPos = mPlayer.GetComponent<Collider>().bounds.center; playerPos.z = 0;

            bodySpriteCtrl.isLeft = Mathf.Sign(playerPos.x - transform.position.x) < 0.0f;

            for(int j = 0; j < laserPts.Length; j++) {
                Vector3 pos = laserPts[j].position; pos.z = 0;
                Vector3 dir = playerPos - pos; dir.Normalize();
                dir = Quaternion.AngleAxis(Random.Range(-laserAngleRand, laserAngleRand), Vector3.forward)*dir;
                Projectile.Create(projGroup, laserProjType, pos, dir, null);
            }

            laserSfx.Play();

            yield return waitLaser;
        }

        ToPhase(Phase.Idle);
    }

    void DoNextPhase() {
        ToPhase(mNextPhase);

        mPhaseCounter++;
        switch(mPhaseCounter) {
            case 1:
                mNextPhase = Phase.ChargeToPlayer;
                break;
            case 2:
                mNextPhase = Phase.ShootLasers;
                break;
            case 3:
                //rand
                mNextPhase = Random.Range(0, 2) == 0 ? Phase.ChargeToPlayer : Phase.ShootLasers;
                break;
            case 4:
                //special
                mPhaseCounter = 0;
                mNextPhase = Phase.Tornado;
                break;
        }
    }

    void OnLanded(PlatformerController ctrl) {
        switch(mCurPhase) {
            case Phase.Idle:
                if(!IsInvoking(nextPhaseFunc))
                    Invoke(nextPhaseFunc, idleWait);
                break;
        }
    }
}
