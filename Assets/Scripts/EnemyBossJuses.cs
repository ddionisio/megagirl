using UnityEngine;
using System.Collections;

public class EnemyBossJuses : Enemy {
    public enum Phase {
        None,
        Normal,
        Dead
    }

    [System.Serializable]
    public class TurretDat {
        public Enemy entity;
        public bool followPlayer; //assumes only one nozzle and projPt
        public float followAngleCap;
        public Transform followDirHolder;

        public float eyeOpenStartDelay;
        public float eyeCloseDelay;

        public Transform[] nozzles; //each will be used as dir and will spawn for each.
        public Transform[] projPts;
        public string projType;
        public int projCount = 1; //number of times to strike
        public float projStartDelay;
        public float projRepeatDelay;
        public float projAngleSpread;
        public int projAngleSpreadRes = 8;
        public bool projSeekPlayer;

        public AnimatorData projFireAnimDat;
        public bool projFireTakeOpen;
        public bool projFireTakeFire = true;
        public bool projFireTakeClose;

        public tk2dSpriteAnimator eyeAnim;
        public tk2dSpriteAnimator turretAnim;

        public GameObject activeGO;
        public GameObject deathDeactiveGO; //deactivate upon death

        private bool mFiring;

        public bool isDead {
            get { return entity.state == (int)EntityState.Dead; }
        }

        public bool isFiring {
            get { return mFiring; }
            set { mFiring = value; }
        }

        public void Init() {
            eyeAnim.Play("close");
            entity.stats.damageReduction = 100.0f;

            activeGO.SetActive(false);
        }

        public void Dead() {
            activeGO.SetActive(false);
            deathDeactiveGO.SetActive(false);
        }

        public void TurretUpdate() {
            if(!isDead && !mFiring) {
                if(followPlayer) {
                    if(nozzles[0].gameObject.activeSelf) {
                        //assumes one nozzle
                        Vector3 pos = nozzles[0].position;
                        Vector3 playerPos = Player.instance.transform.position;
                        Vector3 newDir = playerPos - pos; newDir.z = 0; newDir.Normalize();

                        nozzles[0].up = M8.MathUtil.DirCap(followDirHolder.up, newDir, followAngleCap);
                    }
                }
            }
        }
    }

    public const string normalRoutine = "DoNormal";

    public const string takeDefeat = "defeat";

    public TurretDat[] turrets;

    public float followDelay = 1.0f;
    public float followSpeedMax = 8.0f;
    public Bounds followRange;

    public Transform rotateTarget;
    public float rotateStartDelay = 0.5f;
    public float rotateDelay = 1.5f;

    public float turretActiveDelay = 0.5f;

    private Phase mCurPhase = Phase.None;
    
    private AnimatorData mAnimDat;
    private Player mPlayer;
    private Vector3 mFollowVel = Vector3.zero;
    private int[] mTurretInds;
    private int mTurretAliveCount;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                ToPhase(Phase.None);
                break;
        }
        
        base.StateChanged();
        
        switch((EntityState)state) {
            case EntityState.Normal:
                ToPhase(Phase.Normal);
                break;

            case EntityState.Dead:
                mAnimDat.Play(takeDefeat);
                
                ToPhase(Phase.Dead);
                break;
        }
    }

    void ToPhase(Phase phase) {
        if(mCurPhase == phase)
            return;
        
        //Debug.Log("phase: " + phase);
        
        //prev
        switch(mCurPhase) {
            case Phase.Normal:
                StopCoroutine(normalRoutine);
                break;

            case Phase.Dead:
                break;
        }

        switch(phase) {
            case Phase.Normal:
                StartCoroutine(normalRoutine);
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

        mTurretInds = new int[turrets.Length];
        for(int i = 0; i < mTurretInds.Length; i++)
            mTurretInds[i] = i;

        mTurretAliveCount = turrets.Length;
    }

    protected override void Start() {
        base.Start();

        foreach(TurretDat turret in turrets) {
            turret.Init();
        }
    }

    void FixedUpdate() {
        if(mCurPhase == Phase.None)
            return;

        switch(mCurPhase) {
            case Phase.Normal:
                for(int i = 0, max = turrets.Length; i < max; i++) {
                    turrets[i].TurretUpdate();
                }

                Vector3 playerPos = Player.instance.transform.position;
                Vector3 followDestPos = new Vector3(
                    Mathf.Clamp(playerPos.x, followRange.min.x, followRange.max.x),
                    Mathf.Clamp(playerPos.y, followRange.min.y, followRange.max.y));

                rigidbody.MovePosition(Vector3.SmoothDamp(rigidbody.position, followDestPos, ref mFollowVel, followDelay, followSpeedMax, Time.deltaTime));
                break;
        }
    }

    IEnumerator DoNormal() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        WaitForSeconds waitRotStart = new WaitForSeconds(rotateStartDelay);
        WaitForSeconds waitTurretStart = new WaitForSeconds(turretActiveDelay);

        M8.ArrayUtil.Shuffle(mTurretInds);

        int curTurretRefInd = 0;

        while(mTurretAliveCount > 0) {
            //turret strike
            yield return waitTurretStart;

            TurretDat turret = null;
            for(int i = curTurretRefInd; i < mTurretInds.Length; i++) {
                if(!turrets[mTurretInds[i]].isDead) {
                    turret = turrets[mTurretInds[i]];
                    curTurretRefInd = i;
                    break;
                }
            }

            /////////////////////////////
            while(true) {
                turret.activeGO.SetActive(true);
                
                turret.eyeAnim.Play("open");
                turret.entity.stats.damageReduction = 0.0f;
                
                if(turret.projFireTakeOpen) {
                    turret.projFireAnimDat.Play("open");
                }
                
                yield return new WaitForSeconds(turret.eyeOpenStartDelay);
                if(turret.isDead) break;
                
                //get ready
                turret.turretAnim.Play("ready");
                
                yield return new WaitForSeconds(turret.projStartDelay);
                if(turret.isDead) break;
                
                turret.turretAnim.Play("normal");
                
                //start firing stuff
                WaitForSeconds fireWait = new WaitForSeconds(turret.projRepeatDelay);
                
                Transform seek = turret.projSeekPlayer ? Player.instance.transform : null;
                
                for(int i = 0; i < turret.projCount; i++) {
                    if(!string.IsNullOrEmpty(turret.projType)) {
                        for(int j = 0; j < turret.nozzles.Length; j++) {
                            Vector3 pos = turret.projPts[j].position; pos.z = 0;
                            Vector3 dir = turret.nozzles[j].up;
                            
                            if(turret.projAngleSpread > 0) {
                                float angle = 0;
                                if(turret.projAngleSpreadRes > 0) {
                                    float div = (float)turret.projAngleSpreadRes;
                                    angle = Mathf.Lerp(-turret.projAngleSpread*0.5f, turret.projAngleSpread*0.5f, ((float)Random.Range(0, turret.projAngleSpreadRes))/div);
                                }
                                else {
                                    angle = Random.Range(-turret.projAngleSpread*0.5f, turret.projAngleSpread*0.5f);
                                }
                                
                                dir = Quaternion.AngleAxis(angle, Vector3.forward)*dir;
                            }
                            
                            Projectile.Create(projGroup, turret.projType, pos, dir, seek);
                        }
                    }
                    
                    turret.isFiring = true;
                    
                    if(turret.projFireTakeFire) {
                        turret.projFireAnimDat.Play("fire");
                        
                        while(turret.projFireAnimDat.isPlaying)
                            yield return wait;
                    }
                    
                    turret.isFiring = false;

                    if(turret.isDead) break;
                    else yield return fireWait;
                }
                
                //done
                if(turret.isDead) break;
                yield return new WaitForSeconds(turret.eyeCloseDelay);
                if(turret.isDead) break;
                
                turret.eyeAnim.Play("close");
                turret.entity.stats.damageReduction = 100.0f;
                
                if(turret.projFireTakeClose) {
                    turret.projFireAnimDat.Play("close");
                }
                
                turret.activeGO.SetActive(false);
                break;
            }

            if(turret.isDead) {
                turret.Dead();
                mTurretAliveCount--;
            }
            ///////////////

            //rotate
            yield return waitRotStart;

            Quaternion rot = rotateTarget.rotation;
            float curTime = 0;
            while(curTime < rotateDelay) {
                curTime = Mathf.Clamp(curTime + Time.fixedDeltaTime, 0.0f, rotateDelay);
                float t = Holoville.HOTween.Core.Easing.Sine.EaseInOut(curTime, 0.0f, 1.0f, rotateDelay, 0, 0);

                rotateTarget.rotation = rot*Quaternion.Euler(0, 0, 90*t);

                yield return wait;
            }

            //advance to next turret
            curTurretRefInd++;
            if(curTurretRefInd == mTurretInds.Length) {
                curTurretRefInd = 0;
                M8.ArrayUtil.Shuffle(mTurretInds);
            }
        }

        //panic

        /*public Transform rotateTarget;
    public float rotateStartDelay = 0.5f;
    public float rotateDelay = 1.5f;

    public float turretActiveDelay = 0.5f;*/

        yield return wait;
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.cyan*0.5f;
        Gizmos.DrawWireCube(followRange.center, followRange.size);
    }
}
