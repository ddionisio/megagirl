using UnityEngine;
using System.Collections;

public class EnemyBossDotLifePrim : Enemy {
    public enum Phase {
        None,
        Move,
        Dead
    }

    public const string takeNormal = "normal";
    public const string takeAttack = "attack";

    public const string moveRoutine = "DoMove";
    public const string deathRoutine = "DoDeath";

    public GameObject nextBossGO; //assume it's disabled

    public Transform eye;

    public Transform eyeAttachPt;
    public float eyeAttachOrientDelay = 0.1f;
    public float eyeAttachMoveDelay = 0.1f;
    public float eyeAttachYPulse = 0.5f;
    public float eyeAttachYPulsePerSec = 1.0f;

    public Transform[] moveWps;
    public float moveStartDelay;
    public float moveSpeed;
    public string moveProjType; //fire when wp reached
    public GameObject moveAttackTrigger;
    public float moveAttackCooldown;
    public int moveCount = 3;

    public AnimatorData deathFader;
    public GameObject[] deathGOs; //game objects to manipulate upon death
    public float deathShakeDuration = 5.0f;
    public float deathShakeAmount = 0.5f;
    public float deathShakeAmp = 0.1f;
    public float deathShakePeriod = 0.12f;
    public float deathFallStartDelay = 2.0f;
    public float deathFallAccel = 0.25f;

    private AnimatorData mAnimDat;
    private Player mPlayer;
    private Phase mCurPhase = Phase.None;
    private bool mEyeLock;
    private Vector3 mEyeAttachVelPos;
    private Vector3 mEyeAttachVelDir;
    private float mEyeAttachCurPulseTime;
    private float mLastAttackTime;
    private int mCurMoveInd = 0;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                ToPhase(Phase.None);
                break;
        }
        
        base.StateChanged();
        
        switch((EntityState)state) {
            case EntityState.Normal:
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
            case Phase.Move:
                moveAttackTrigger.SetActive(false);
                StopCoroutine(moveRoutine);
                break;

            case Phase.Dead:
                StopCoroutine(deathRoutine);
                break;
        }
        
        switch(phase) {
            case Phase.Move:
                StartCoroutine(moveRoutine);
                break;

            case Phase.Dead:
                StartCoroutine(deathRoutine);
                break;
        }
        
        mCurPhase = phase;
    }
    
    public override void SpawnFinish() {
        base.SpawnFinish();
    }
    
    protected override void Awake() {
        base.Awake();
        
        mPlayer = Player.instance;
        mAnimDat = GetComponent<AnimatorData>();

        moveAttackTrigger.SetActive(false);
    }

    void OnTriggerStay(Collider col) {
        switch(mCurPhase) {
            case Phase.Move:
                if(Time.fixedTime - mLastAttackTime > moveAttackCooldown) {
                    moveAttackTrigger.SetActive(false);
                    mAnimDat.Play(takeAttack);
                }
                break;
        }
    }
    
    void Update() {
        float dt = Time.deltaTime;
        
        if(!mEyeLock) {
            //look at player
            mEyeAttachCurPulseTime += dt;

            Vector3 eyePos = eye.position;
            eyePos.y += Mathf.Sin(Mathf.PI*eyeAttachYPulsePerSec*mEyeAttachCurPulseTime)*eyeAttachYPulse;
            Vector3 dirToPlayer = mPlayer.transform.position - eyePos; dirToPlayer.z = 0.0f; dirToPlayer.Normalize();
            
            eye.up = dirToPlayer;
            
            //move and orient eye attach slowly
            eyeAttachPt.position = Vector3.SmoothDamp(eyeAttachPt.position, eyePos, ref mEyeAttachVelPos, eyeAttachMoveDelay, Mathf.Infinity, dt);
            eyeAttachPt.up = Vector3.SmoothDamp(eyeAttachPt.up, dirToPlayer, ref mEyeAttachVelDir, eyeAttachOrientDelay, Mathf.Infinity, dt);
        }
    }

    IEnumerator DoMove() {
        WaitForSeconds waitMoveStart = new WaitForSeconds(moveStartDelay);
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        moveAttackTrigger.SetActive(true);
        mLastAttackTime = 0;

        mAnimDat.Play(takeNormal);

        for(int i = 0; i < moveCount; i++) {
            yield return waitMoveStart;

            //initialize movement
            Vector3 ePos = moveWps[mCurMoveInd].position;
            mCurMoveInd++; if(mCurMoveInd == moveWps.Length) mCurMoveInd = 0;

            Vector3 sPos = transform.position;
            Vector3 delta = ePos - sPos;
            float dist = delta.magnitude;
            float delay = dist/moveSpeed;

            float curMoveTime = 0;
            while(curMoveTime < delay) {
                //we are attacking, wait a bit
                if(mAnimDat.currentPlayingTake.name == takeAttack) {
                    while(mAnimDat.isPlaying)
                        yield return wait;

                    mAnimDat.Play(takeNormal);
                    moveAttackTrigger.SetActive(true);
                    mLastAttackTime = Time.fixedTime;

                    //done, resume
                }

                //move
                curMoveTime += Time.fixedDeltaTime; 
                if(curMoveTime >= delay) {
                    rigidbody.MovePosition(ePos);
                }
                else {
                    float t = Holoville.HOTween.Core.Easing.Sine.EaseInOut(curMoveTime, 0.0f, 1.0f, delay, 0, 0);
                    rigidbody.MovePosition(Vector3.Lerp(sPos, ePos, t));
                }

                yield return wait;
            }

            //fire
            Vector3 projPos = eye.position; projPos.z = 0.0f;
            Vector3 projDir = eye.up;

            Projectile proj = Projectile.Create(projGroup, moveProjType, projPos, projDir, null);
            ProjectileSpawnOnDeath projDeath = proj.GetComponent<ProjectileSpawnOnDeath>();
            projDeath.seek = mPlayer.transform;
        }
    }

    IEnumerator DoDeath() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        mEyeLock = false;

        Holoville.HOTween.Tweener[] shakeTweens;

        //shake
        shakeTweens = new Holoville.HOTween.Tweener[deathGOs.Length];
        for(int i = 0; i < deathGOs.Length; i++) {
            shakeTweens[i] = Holoville.HOTween.HOTween.Shake(deathGOs[i].transform, deathShakeDuration, 
                                                             "localPosition", new Vector3(deathShakeAmount, 0, 0), 
                                                             true, deathShakeAmp, deathShakePeriod);
            shakeTweens[i].loops = -1;
            shakeTweens[i].Play();
        }

        //wait
        yield return new WaitForSeconds(deathFallStartDelay);

        //fade
        deathFader.gameObject.SetActive(true);
        deathFader.Play("fadeout");

        //fall down while fading
        float curFallVel = 0;
        while(deathFader.isPlaying) {
            for(int i = 0; i < deathGOs.Length; i++) {
                Vector3 pos = deathGOs[i].transform.position;
                pos.y -= curFallVel*Time.fixedDeltaTime;
            }

            curFallVel += deathFallAccel*Time.fixedDeltaTime;

            yield return wait;
        }

        for(int i = 0; i < shakeTweens.Length; i++) {
            Holoville.HOTween.HOTween.Kill(shakeTweens[i]);
        }

        //deactivate self, go to next boss
        gameObject.SetActive(false);
        nextBossGO.SetActive(true);
    }
}
