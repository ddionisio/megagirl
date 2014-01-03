using UnityEngine;
using System.Collections;

public class EnemyBossEffin : Enemy {
    public enum Phase {
        None,
        MoveToWP,
        FollowPlayer,
        FollowPath,
        StuffContract,
        StuffExpand,
        StuffExpandStart,
        Dead
    }

    public enum StuffState {
        Start,
        Expanded,
        Contracted
    }

    public tk2dSpriteAnimator anim;

    public AnimatorData wpAnimDat;
    public Transform mover;
    public Transform[] wps;

    public float followPlayerMoveDelay = 0.5f;
    public float followPlayerDuration = 4.0f;
    public float followPlayerMaxSpeed = 10.0f;
    public Transform followPlayerFixedPos;

    public float moveToWPSpeed = 10.0f;
    public float moveToWPEndDelay = 1.0f;

    public float followPathEndDelay = 1.5f;

    public GameObject stuffActiveGO;
    public float stuffDelay = 1.0f;
    public float stuffSpeed = 5.0f;
    public float stuffCooldown = 8.0f;

    public string attackProjType = "enemyBulletBig";
    public float attackProjDelay = 1.0f;
    public float attackProjCooldown = 1.0f;

    public const string clipNormal = "normal";
    public const string clipAttack = "attack";
    public const string clipDefeat = "defeat";

    public const string takeDefeat = "defeat";

    public const string attackRoutine = "DoAttack";

    public const string followPlayerRoutine = "DoFollowPlayer";
    public const string moveToWPRoutine = "DoMoveToWP";
    public const string followPathRoutine = "DoFollowPath";

    public const string stuffRoutine = "DoStuff";

    private Phase mCurPhase = Phase.None;
    private StuffState mCurStuffState = StuffState.Start;

    private AnimatorData mAnimDat;
    private Player mPlayer;
    private EnemyBossEffinStuff mEffinStuff;
    private float mLastEffinStuffTime;

    private int[][] mWPDests = new int[][]{
        new int[]{0,1},
        new int[]{1,0},
        new int[]{0,2},
        new int[]{2,0},
    };

    private int mCurWPInd;

    //0_1, 1_2, etc.
    string GetWPTake(int fromInd, int toInd) {
        return fromInd.ToString()+"_"+toInd.ToString();
    }

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                mEffinStuff.holder.gameObject.SetActive(false);

                StopCoroutine(attackRoutine);
                
                ToPhase(Phase.None);
                break;
        }
        
        base.StateChanged();
        
        switch((EntityState)state) {
            case EntityState.Normal:
                wpAnimDat.Play("default");

                mEffinStuff.holder.gameObject.SetActive(true);

                anim.Play(clipNormal);

                mCurStuffState = StuffState.Start;
                ToPhase(Phase.StuffContract);

                StartCoroutine(attackRoutine);
                break;
                
            case EntityState.Dead:
                anim.Play(clipDefeat);
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
            case Phase.FollowPlayer:
                StopCoroutine(followPlayerRoutine);
                break;

            case Phase.MoveToWP:
                StopCoroutine(moveToWPRoutine);
                break;

            case Phase.FollowPath:
                StopCoroutine(followPathRoutine);
                break;

            case Phase.StuffContract:
            case Phase.StuffExpand:
            case Phase.StuffExpandStart:
                stuffActiveGO.SetActive(false);
                StopCoroutine(stuffRoutine);
                break;
        }
        
        //new
        switch(phase) {
            case Phase.FollowPlayer:
                StartCoroutine(followPlayerRoutine);
                break;
                
            case Phase.MoveToWP:
                StartCoroutine(moveToWPRoutine);
                break;
                
            case Phase.FollowPath:
                StartCoroutine(followPathRoutine);
                break;

            case Phase.StuffContract:
            case Phase.StuffExpand:
            case Phase.StuffExpandStart:
                stuffActiveGO.SetActive(true);
                StartCoroutine(stuffRoutine);
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

        mAnimDat = GetComponent<AnimatorData>();
        mEffinStuff = GetComponent<EnemyBossEffinStuff>();

        mCurWPInd = 0;
        M8.ArrayUtil.Shuffle(mWPDests);

        mEffinStuff.holder.gameObject.SetActive(false);

        //initialize stuff data
    }
    
    protected override void Start() {
        base.Start();

        mPlayer = Player.instance;
    }

    void FixedUpdate() {
        if(mCurPhase == Phase.None)
            return;

        switch(mCurPhase) {
            case Phase.FollowPath:
            case Phase.FollowPlayer:
            case Phase.MoveToWP:
                rigidbody.MovePosition(mover.position);
                break;
        }
    }

    IEnumerator DoAttack() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        WaitForSeconds waitDelay = new WaitForSeconds(attackProjDelay);
        WaitForSeconds waitCooldown = new WaitForSeconds(attackProjCooldown);
        
        while(true) {
            yield return waitCooldown;

            while(mCurPhase == Phase.StuffContract || mCurPhase == Phase.StuffExpand || mCurPhase == Phase.StuffExpandStart)
                yield return wait;

            anim.Play(clipAttack);
            yield return waitDelay;

            Vector3 pos = collider.bounds.center; pos.z = 0;

            Vector3 dir = mPlayer.collider.bounds.center - pos; 
            dir.z = 0;
            dir.Normalize();

            Projectile.Create(projGroup, attackProjType, pos, dir, null);

            anim.Play(clipNormal);
        }
    }

    IEnumerator DoFollowPlayer() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        float lastTime = Time.fixedTime;
        Vector3 fixedPos = followPlayerFixedPos.position;
        Vector3 curVel = Vector3.zero;
        
        while(Time.fixedTime - lastTime < followPlayerDuration) {
            Vector3 followPos = mover.position;
            Vector3 playerPos = mPlayer.transform.position;
            Vector3 destPos = new Vector3(playerPos.x, fixedPos.y, 0.0f);

            mover.position = Vector3.SmoothDamp(followPos, destPos, ref curVel, followPlayerMoveDelay, followPlayerMaxSpeed, Time.fixedDeltaTime);

            yield return wait;
        }

        ToPhase(Phase.MoveToWP);
    }

    IEnumerator DoMoveToWP() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        Vector3 dest = wps[mWPDests[mCurWPInd][0]].position;
        Vector3 pos = mover.position;

        Vector3 dpos = dest - pos;
        float dist = dpos.magnitude;
        if(dist > 0) {
            float delay = dist/moveToWPSpeed;

            float curT = 0.0f;
            while(curT < delay) {
                float t = Holoville.HOTween.Core.Easing.Sine.EaseInOut(curT, 0.0f, 1.0f, delay, 0.0f, 0.0f);

                mover.position = Vector3.Lerp(pos, dest, t);

                curT += Time.fixedDeltaTime;

                yield return wait;
            }

            yield return new WaitForSeconds(moveToWPEndDelay);
        }
        else
            yield return wait;

        if(Time.fixedTime - mLastEffinStuffTime > stuffCooldown) {
            switch(mCurStuffState) {
                case StuffState.Contracted:
                    ToPhase(Phase.StuffExpand);
                    break;
                case StuffState.Expanded:
                    ToPhase(Phase.StuffExpandStart);
                    break;
                case StuffState.Start:
                    ToPhase(Phase.StuffContract);
                    break;
            }
        }
        else {
            ToPhase(Phase.FollowPath);
        }
    }

    IEnumerator DoFollowPath() {
        string take = GetWPTake(mWPDests[mCurWPInd][0], mWPDests[mCurWPInd][1]);

        wpAnimDat.Play(take);

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        while(wpAnimDat.isPlaying)
            yield return wait;

        yield return new WaitForSeconds(followPathEndDelay);

        int lastWPInd = mCurWPInd;

        mCurWPInd++;
        if(mCurWPInd == mWPDests.Length) {
            mCurWPInd = 0;
            M8.ArrayUtil.Shuffle(mWPDests);
        }

        ToPhase(Phase.MoveToWP);
    }

    bool _DoStuffMoveOfsY(RigidBodyMoveToTarget mover, float start, float end, float curTime) {
        float delay = Mathf.Abs(end - start)/stuffSpeed;
        if(curTime < delay) {
            float t = Holoville.HOTween.Core.Easing.Sine.EaseInOut(curTime, 0.0f, 1.0f, delay, 0.0f, 0.0f);
            mover.offset.y = Mathf.Lerp(start, end, t);
            return false;
        }
        else {
            mover.offset.y = end;
            return true;
        }
    }

    IEnumerator DoStuff() {
        yield return new WaitForSeconds(stuffDelay);

        //mEffinStuff.Shuffle();
        
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
                        
        for(int i = 0, max = mEffinStuff.data.Length; i < max; i+=2) {
            float lastT = Time.fixedTime;
            EnemyBossEffinStuff.Data dat = mEffinStuff.data[i];
            EnemyBossEffinStuff.Data dat2 = mEffinStuff.data[i+1];

            float ofsY = dat.mover.offset.y, ofsY2 = dat2.mover.offset.y;

            float endY = 0, endY2 = 0;
            switch(mCurPhase) {
                case Phase.StuffContract:
                    endY = dat.originOfsY; endY2 = dat2.originOfsY;
                    break;
                case Phase.StuffExpand:
                    endY = dat.expandOfsY; endY2 = dat2.expandOfsY;
                    break;
                case Phase.StuffExpandStart:
                    endY = dat.startOfsY; endY2 = dat2.startOfsY;
                    break;
            }
            
            bool done=false,done2=false;
            
            while(!(done || done2)) {
                if(!done) {
                    done = _DoStuffMoveOfsY(dat.mover, ofsY, endY, Time.fixedTime - lastT);
                }
                
                if(!done2) {
                    done2 = _DoStuffMoveOfsY(dat2.mover, ofsY2, endY2, Time.fixedTime - lastT);
                }

                yield return wait;
            }
        }

        switch(mCurPhase) {
            case Phase.StuffContract:
                mCurStuffState = StuffState.Contracted;
                break;
            case Phase.StuffExpand:
                mCurStuffState = StuffState.Expanded;
                break;
            case Phase.StuffExpandStart:
                mCurStuffState = StuffState.Start;
                break;
        }

        mLastEffinStuffTime = Time.fixedTime;
        
        ToPhase(Phase.MoveToWP);
    }
}
