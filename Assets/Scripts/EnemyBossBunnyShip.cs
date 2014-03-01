using UnityEngine;
using System.Collections;

public class EnemyBossBunnyShip : Enemy {
    public enum Phase {
        None,
        Fire,
        Lazors,
        Move,
        Bomb,
        Dead
    }

    public const string takeNormal = "normal";
    public const string takeDefeat = "defeat";

    public const string takeFireOpen = "fireOpen";
    public const string takeFireClose = "fireClose";

    public const string takeMoveClosedLeft = "chargeClosedReadyLeft";
    public const string takeMoveClosedRight = "chargeClosedReadyRight";
    public const string takeMoveOpenLeft = "chargeOpenReadyLeft";
    public const string takeMoveOpenRight = "chargeOpenReadyRight";

    public const string takeBombOpen = "bombOpen";
    public const string takeBombClose = "bombClose";

    public const string fireRoutine = "DoFire";
    public const string lazorsRoutine = "DoLazors";
    public const string moveRoutine = "DoMove";
    public const string bombRoutine = "DoBomb";

    public Enemy weakpoint;

    public SpritePropertyMulti bodySprProp;

    public float laserPointStart = 0.3f;
    public GameObject laserActiveGO;
    public Transform laserPoint;
    public GameObject laserPointActive;
    public GameObject laserPointScan;
    public float laserPointDeactiveDelay;
    public LayerMask laserPointColMask;
    public float laserPointDuration;
    public Transform laserPointDest;
    public float laserPointDestZOfs = -1.5f;

    public string fireProjType;
    public Transform firePt;
    public float fireMoveSpeed;
    public float fireStartDelay = 0.3f;
    public float fireRepeatDelay = 0.5f;

    public GameObject[] moveActiveGOs;

    public Transform moveYUpper;
    public Transform moveYDowner;
    public Transform moveYDefault;

    public Transform moveXLeftDefault;
    public Transform moveXLeftCharge;
    public Transform moveXRightDefault;
    public Transform moveXRightCharge;

    public float moveStartDelay = 0.3f;
    public float moveDelay = 2.0f;
    public float moveBackStartDelay = 1.0f;
    public float moveBackDelay = 1.0f;

    public Transform eye;
    public float eyeFollowDelay;

    public Transform bombPt;
    public string bombProjType;
    public float bombMoveSpeed;
    public int bombCount;
    public float bombEndDelay = 0.5f;
    public float bombLaunchAngle = 45.0f;

    public SoundPlayer fireSfx;
    public SoundPlayer laserSfx;
    public SoundPlayer cannonSfx;

    public Phase[] pattern;

    private AnimatorData mAnimDat;
    private Player mPlayer;
    private Phase mCurPhase = Phase.None;
    private Vector3 mEyeCurVel;
    private bool mEyeFollowLocked;

    private Damage mLaserPointDmg;
    private tk2dBaseSprite[]mLaserPointSprites;
    private bool mLaserActive;
    private float mLaserActiveTime;

    private int mMoveTypeCounter;

    private int mBombCurCount;

    private int mCurPattern;

    public void LaserActive(bool a) {
        if(!mLaserActive && a) {
            laserSfx.Play();
            laserPointDest.gameObject.SetActive(true);
        }

        mLaserActive = a;
        mLaserActiveTime = Time.fixedTime;
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
                DoNextPhase();
                break;
                
            case EntityState.Dead:
                mAnimDat.Play(takeDefeat);
                
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
            case Phase.Fire:
                StopCoroutine(fireRoutine);
                break;

            case Phase.Lazors:
                mEyeFollowLocked = false;
                laserActiveGO.SetActive(false);
                laserPoint.gameObject.SetActive(false);
                laserPointDest.gameObject.SetActive(false);

                StopCoroutine(lazorsRoutine);
                break;

            case Phase.Move:
                foreach(GameObject go in moveActiveGOs)
                    go.SetActive(false);

                StopCoroutine(moveRoutine);
                break;

            case Phase.Bomb:
                foreach(GameObject go in moveActiveGOs)
                    go.SetActive(false);

                StopCoroutine(bombRoutine);
                break;
                
            case Phase.Dead:
                break;
        }
        
        switch(phase) {
            case Phase.Fire:
                StartCoroutine(fireRoutine);
                break;

            case Phase.Lazors:
                StartCoroutine(lazorsRoutine);
                break;

            case Phase.Move:
                StartCoroutine(moveRoutine);
                break;

            case Phase.Bomb:
                StartCoroutine(bombRoutine);
                break;

            case Phase.Dead:
                eye.up = Vector3.up;
                break;
        }
        
        mCurPhase = phase;
    }
    
    public override void SpawnFinish() {
        base.SpawnFinish();

        weakpoint.stats.changeHPCallback += OnWeakpointHPChange;
    }
    
    protected override void Awake() {
        base.Awake();

        mPlayer = Player.instance;
        mAnimDat = GetComponent<AnimatorData>();

        mLaserPointDmg = laserPoint.GetComponent<Damage>();
        mLaserPointSprites = laserPoint.GetComponentsInChildren<tk2dBaseSprite>(true);

        laserPoint.gameObject.SetActive(false);
        laserPointDest.gameObject.SetActive(false);
    }

    void FixedUpdate() {
        if(mCurPhase == Phase.None)
            return;

        switch(mCurPhase) {
            case Phase.Dead:
                break;

            default:
                if(!mEyeFollowLocked) {
                    Vector3 playerPos = mPlayer.collider.bounds.center;
                    Vector3 pos = eye.position;
                    Vector3 dir = playerPos - pos; dir.z = 0; dir.Normalize();
                    eye.up = Vector3.SmoothDamp(eye.up, dir, ref mEyeCurVel, eyeFollowDelay, Mathf.Infinity, Time.deltaTime);
                }
                break;
        }
    }

    void OnWeakpointHPChange(Stats stat, float delta) {
        stats.curHP = stat.curHP;
    }

    void OnBombRelease(EntityBase ent) {
        ent.releaseCallback -= OnBombRelease;

        mBombCurCount--;
        if(mBombCurCount < 0)
            mBombCurCount = 0;
    }

    void DoNextPhase() {
        while(pattern[mCurPattern] == Phase.Bomb && mBombCurCount > 0) {
            mCurPattern++;
            if(mCurPattern >= pattern.Length) {
                mCurPattern = 0;
                break;
            }
        }

        ToPhase(pattern[mCurPattern]);
        mCurPattern++;
        if(mCurPattern >= pattern.Length)
            mCurPattern = 0;
    }
    
    IEnumerator DoBomb() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        //open
        mAnimDat.Play(takeBombOpen);
        while(mAnimDat.isPlaying)
            yield return wait;

        Vector3 pos = transform.position;

        foreach(GameObject go in moveActiveGOs)
            go.SetActive(true);

        //figure out move delay
        float xStart = pos.x;
        float xEnd = bodySprProp.flipX ? moveXRightDefault.position.x : moveXLeftDefault.position.x;
        float xDelta = xEnd - xStart;
        float delay = Mathf.Abs(xDelta)/bombMoveSpeed;
        float bombDelay = delay/((float)bombCount);

        float curTime = 0.0f;
        float lastBombTime = 0.0f;

        Vector3 bombDir = Quaternion.Euler(0, 0, bodySprProp.flipX ? -bombLaunchAngle : bombLaunchAngle)*Vector3.down;

        while(true) {
            pos.x = Holoville.HOTween.Core.Easing.Sine.EaseOut(curTime, xStart, xDelta, delay, 0, 0);
            transform.position = pos;

            if(mBombCurCount < bombCount && Time.fixedTime - lastBombTime >= bombDelay) {
                Vector3 projPt = bombPt.position; projPt.z = 0.0f;

                Projectile proj = Projectile.Create(projGroup, bombProjType, projPt, bombDir, null);
                proj.releaseCallback += OnBombRelease;
                mBombCurCount++;

                lastBombTime = Time.fixedTime;

                cannonSfx.Play();
            }

            if(curTime == delay)
                break;
            else {
                curTime = Mathf.Clamp(curTime + Time.fixedDeltaTime, 0, delay);
                yield return wait;
            }
        }

        bodySprProp.flipX = !bodySprProp.flipX;

        yield return new WaitForSeconds(bombEndDelay);

        //close
        mAnimDat.Play(takeBombClose);
        while(mAnimDat.isPlaying)
            yield return wait;

        DoNextPhase();
    }

    IEnumerator DoLazors() {
        laserActiveGO.SetActive(true);

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        laserPoint.gameObject.SetActive(true);
        laserPointScan.SetActive(false);
        laserPointActive.SetActive(false);

        yield return new WaitForSeconds(laserPointStart);

        laserPointScan.SetActive(true);

        mLaserActive = false;

        //acquire target
        float startTime = Time.fixedTime;
        while(Time.fixedTime - startTime < laserPointDuration) {
            float dist = 1000.0f;
            RaycastHit hit;
            bool playerHit = false;

            if(Physics.Raycast(laserPoint.position, laserPoint.up, out hit, dist, laserPointColMask)) {
                dist = hit.distance;
                laserPointDest.position = new Vector3(hit.point.x, hit.point.y, laserPointDestZOfs);
                
                //do damage
                if(hit.collider.CompareTag("Player")) {
                    if(mLaserActive) {
                        mLaserPointDmg.CallDamageTo(hit.collider.gameObject, hit.point, hit.normal);
                    }
                    else {
                        laserPointScan.SetActive(false);
                        laserPointActive.SetActive(true);
                    }

                    playerHit = true;
                    mEyeFollowLocked = true;
                }
            }
            else {
                laserPointDest.position = new Vector3(laserPoint.position.x, laserPoint.position.y, laserPointDestZOfs);
            }

            if(mLaserActive && Time.fixedTime - mLaserActiveTime > laserPointDeactiveDelay) {
                laserPointScan.SetActive(true);
                laserPointActive.SetActive(false);
                laserPointDest.gameObject.SetActive(false);
                mLaserActive = false;
                mEyeFollowLocked = false;
            }
            
            const float scaleConv = 4.0f/12.0f;
            for(int i = 0, max = mLaserPointSprites.Length; i < max; i++) {
                Vector3 s = mLaserPointSprites[i].scale;
                s.y = dist/scaleConv;
                mLaserPointSprites[i].scale = s;
            }

            yield return wait;
        }

        //next phase
        DoNextPhase();
    }

    bool MoveYNFire(float dirY, float y, Vector3 fireDir, ref Vector3 pos, ref float lastFireTime) {
        if((dirY < 0.0f && pos.y > y) || (dirY > 0.0f && pos.y < y)) {
            pos.y += dirY*fireMoveSpeed*Time.fixedDeltaTime;
            transform.position = pos;

            if(Time.fixedTime - lastFireTime > fireRepeatDelay) {
                Vector3 firePos = firePt.position; firePos.z = 0.0f;
                Projectile.Create(projGroup, fireProjType, firePos, fireDir, null);
                lastFireTime = Time.fixedTime;

                fireSfx.Play();
            }

            return true;
        }

        pos.y = y; transform.position = pos;

        return false;
    }

    IEnumerator DoFire() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        //choose point order
        Vector3 pt1, pt2;

        int rnd = Random.Range(0, 2);
        if(rnd == 0) {
            pt1 = moveYUpper.position;
            pt2 = moveYDowner.position;
        }
        else {
            pt1 = moveYDowner.position;
            pt2 = moveYUpper.position;
        }

        Vector3 fireDir = bodySprProp.flipX ? Vector3.right : Vector3.left;

        //animate open //bodySprProp
        mAnimDat.Play(takeFireOpen);
        while(mAnimDat.isPlaying)
            yield return wait;

        //wait a bit
        yield return new WaitForSeconds(fireStartDelay);

        Vector3 pos = transform.position;
        float lastFireTime = 0.0f;

        //move to first pt
        float dirY = Mathf.Sign(pt1.y - pos.y);
        while(MoveYNFire(dirY, pt1.y, fireDir, ref pos, ref lastFireTime))
            yield return wait;

        //move to next pt
        dirY = Mathf.Sign(pt2.y - pos.y);
        while(MoveYNFire(dirY, pt2.y, fireDir, ref pos, ref lastFireTime))
            yield return wait;

        //move to first pt
        dirY = Mathf.Sign(pt1.y - pos.y);
        while(MoveYNFire(dirY, pt1.y, fireDir, ref pos, ref lastFireTime))
            yield return wait;

        //move to default pt
        Vector3 ptD = moveYDefault.position;

        dirY = Mathf.Sign(ptD.y - pos.y);
        while(MoveYNFire(dirY, ptD.y, fireDir, ref pos, ref lastFireTime))
            yield return wait;

        //close
        mAnimDat.Play(takeFireClose);
        while(mAnimDat.isPlaying)
            yield return wait;

        //next phase
        DoNextPhase();
    }

    IEnumerator DoMove() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        //determine which take to begin
        if(bodySprProp.flipX) {
            if(mMoveTypeCounter <= 1)
                mAnimDat.Play(takeMoveClosedRight);
            else
                mAnimDat.Play(takeMoveOpenRight);
        }
        else {
            if(mMoveTypeCounter <= 1)
                mAnimDat.Play(takeMoveClosedLeft);
            else
                mAnimDat.Play(takeMoveOpenLeft);
        }

        //wait for anim
        while(mAnimDat.isPlaying)
            yield return wait;

        //wait a bit
        yield return new WaitForSeconds(moveStartDelay);

        foreach(GameObject go in moveActiveGOs)
            go.SetActive(true);

        //move
        Vector3 pos = transform.position;
        float xStart = pos.x;
        float xDelta = (bodySprProp.flipX ? moveXRightCharge.position.x : moveXLeftCharge.position.x) - xStart;

        float curTime = 0.0f;
        while(true) {
            pos.x = Holoville.HOTween.Core.Easing.Sine.EaseIn(curTime, xStart, xDelta, moveDelay, 0, 0);
            transform.position = pos;

            if(curTime == moveDelay)
                break;
            else {
                curTime = Mathf.Clamp(curTime + Time.fixedDeltaTime, 0.0f, moveDelay);
                yield return wait;
            }
        }

        mAnimDat.Play(takeNormal);

        //wait a bit
        yield return new WaitForSeconds(moveBackStartDelay);

        //move to screen
        xStart = pos.x;
        xDelta = (bodySprProp.flipX ? moveXRightDefault.position.x : moveXLeftDefault.position.x) - xStart;

        //flip
        bodySprProp.flipX = !bodySprProp.flipX;
        
        curTime = 0.0f;
        while(true) {
            pos.x = Holoville.HOTween.Core.Easing.Sine.EaseIn(curTime, xStart, xDelta, moveBackDelay, 0, 0);
            transform.position = pos;
            
            if(curTime == moveBackDelay)
                break;
            else {
                curTime = Mathf.Clamp(curTime + Time.fixedDeltaTime, 0.0f, moveBackDelay);
                yield return wait;
            }
        }

        mMoveTypeCounter++;
        if(mMoveTypeCounter == 4)
            mMoveTypeCounter = 0;

        DoNextPhase();
    }
}
