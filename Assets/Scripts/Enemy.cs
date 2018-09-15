using UnityEngine;
using System.Collections;

public class Enemy : EntityBase {
    public const string projGroup = "projEnemy";
    public const string projCommonType = "enemyBullet";

    public const float stunDelay = 2.0f;
    public const float stunImmuneDelay = 3.0f;

    public bool respawnOnSleep = true; //for regular enemies, this will cause a restart on deactivate
    public bool releaseOnSleep = false;

    public bool toRespawnAuto = true; //if true, wait for a delay during death to wait for respawn
    public float toRespawnDelay = 0.0f;

    public bool disablePhysicsOnDeath = true;
    public bool releaseOnDeath = false;

    public AnimatorData animator; //if this enemy is controlled by an animator (usu. as parent)
    public GameObject visibleGO; //the game object to deactivate while dead/respawning
    public GameObject stunGO;

    public GameObject deathActivateGO;
    public string deathSpawnGroup; //upon death, spawn this
    public string deathSpawnType;
    public Transform deathSpawnAttach;
    public Vector3 deathSpawnOfs;

    public int weaponIndexUnlock = -1; //for bosses, acquire this weapon upon defeat

    public const string soundHurt = "enemyHit";
    public const string soundStun = "stun";

    private Stats mStats;
    private bool mRespawnReady;

    protected Vector3 mSpawnPos;
    protected Quaternion mSpawnRot;
    private bool mSpawnRigidBodyKinematic;

    private GravityController mGravCtrl;
    private PlatformerController mBodyCtrl;
    private PlatformerSpriteController mBodySpriteCtrl;
    private float mDefaultDeactiveDelay;

    private Damage[] mDamageTriggers;

    private bool mUseBossHP;
    private float mLastStunnedTime;
    private bool mIsSuddenDeath;

    public Stats stats { get { return mStats; } }
    public PlatformerController bodyCtrl { get { return mBodyCtrl; } }
    public GravityController gravityCtrl { get { return mGravCtrl; } }
    public PlatformerSpriteController bodySpriteCtrl { get { return mBodySpriteCtrl; } }

    public override void Activate() {
        base.Activate();
    }

    //for use with animator
    public void FireAt(string proj, string childGO, int fdir, bool forceDecay, float decay) {
        GameObject go = M8.Util.FindGameObjectByNameRecursive(gameObject, childGO);
        Transform pt = go.transform;
        Vector3 p = pt.position; p.z = 0;
        Vector3 dir;
        switch(fdir) {
            case 0:
                dir = pt.up;
                break;
            case 1:
                dir = -pt.up;
                break;
            case 2:
                dir = -pt.right;
                break;
            default:
                dir = pt.right;
                break;
        }
        Projectile _proj = Projectile.Create(projGroup, proj, p, dir, null);
        if(forceDecay) {
            _proj.decayEnabled = forceDecay;
            _proj.decayDelay = decay;
        }
    }

    public void Jump(float delay) {
        if(mBodyCtrl) {
            CancelInvoke(JumpFinishKey);

            if(delay > 0) {
                mBodyCtrl.Jump(true);
                Invoke(JumpFinishKey, delay);
            }
            else
                mBodyCtrl.Jump(false);
        }
    }

    public void BossHPEnable() {
        //only call this once
        if(!mUseBossHP) {
            mUseBossHP = true;
            HUD.instance.barBoss.current = 0; // make sure to call fill hp for that dramatic boss start moment
            HUD.instance.barBoss.max = Mathf.CeilToInt(mStats.maxHP);
            HUD.instance.barBoss.gameObject.SetActive(true);
            HUD.instance.barBoss.animateEndCallback += OnHPBarFilled;
        }
    }

    public void BossHPFill() {
        Player.instance.Pause(true);
        HUD.instance.barBoss.currentSmooth = Mathf.CeilToInt(mStats.maxHP);
    }

    /// <summary>
    /// Call this for manual respawn wait during death sequence
    /// </summary>
    public void ToRespawnWait() {
        state = (int)EntityState.RespawnWait;
        //check if we are inactive, then we are ready to respawn
        if(activator && !activator.isActive) {
            Restart();
            mRespawnReady = true;
        }
        else
            SetPhysicsActive(false, false);
    }

    protected override void OnDespawned() {
        //reset stuff here
        state = (int)EntityState.Invalid;

        Restart();

        mRespawnReady = false;

        base.OnDespawned();
    }

    protected override void OnDestroy() {
        //dealloc here

        base.OnDestroy();
    }

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                Jump(0.0f);
                break;

            case EntityState.Stun:
                if(stunGO)
                    stunGO.SetActive(false);

                SetPhysicsActive(true, true);

                CancelInvoke("DoStun");
                break;

            case EntityState.RespawnWait:
                if(activator)
                    activator.deactivateDelay = mDefaultDeactiveDelay;

                mStats.isInvul = false;
                break;
        }

        switch((EntityState)state) {
            case EntityState.Dead:
                if(mUseBossHP) {
                    mUseBossHP = false;
                    HUD.instance.barBoss.animateEndCallback -= OnHPBarFilled;
                }

                if(weaponIndexUnlock != -1) {
                    //save weapon info for victory screen
                    Player player = Player.instance;
                    ModalVictory.sWeaponIconRef = player.weapons[weaponIndexUnlock].iconSpriteRef;
                    ModalVictory.sWeaponTitleRef = player.weapons[weaponIndexUnlock].labelTextRef;

                    if(!LevelController.isTimeTrial)
                        SlotInfo.WeaponUnlock(weaponIndexUnlock);
                }

                if(disablePhysicsOnDeath)
                    SetPhysicsActive(false, false);

                Blink(0.0f);
                mStats.isInvul = true;

                if(visibleGO)
                    visibleGO.SetActive(false);

                if(mIsSuddenDeath) { //don't spawn death stuff if we fall in death
                    mIsSuddenDeath = false;
                }
                else {
                    if(!string.IsNullOrEmpty(deathSpawnGroup) && !string.IsNullOrEmpty(deathSpawnType)) {
                        Vector3 pt = (deathSpawnAttach ? deathSpawnAttach : transform).localToWorldMatrix.MultiplyPoint(deathSpawnOfs); pt.z = 0;
                        PoolController.Spawn(deathSpawnGroup, deathSpawnType, deathSpawnType, null, pt, Quaternion.identity);
                    }

                    if(deathActivateGO) {
                        deathActivateGO.SetActive(true);
                    }
                }

                if(toRespawnAuto) {
                    if(gameObject.activeSelf)
                        StartCoroutine(DoRespawnWaitDelayKey);
                    else
                        ToRespawnWait();
                }
                else if(releaseOnDeath)
                    Release();
                break;

            case EntityState.Stun:
                if(stunGO)
                    stunGO.SetActive(true);

                SetPhysicsActive(false, true);

                Invoke("DoStun", stunDelay);

                SoundPlayerGlobal.instance.Play(soundStun);
                break;

            case EntityState.RespawnWait:
                //Debug.Log("respawn wait");
                RevertTransform();

                if(activator) {
                    activator.deactivateDelay = 0.0f;
                    activator.ForceActivate();
                }

                if(mBodyCtrl)
                    mBodyCtrl.moveSide = 0.0f;

                if(mBodySpriteCtrl)
                    mBodySpriteCtrl.ResetAnimation();

                if(GetComponent<Rigidbody>() && !GetComponent<Rigidbody>().isKinematic)
                    GetComponent<Rigidbody>().velocity = Vector3.zero;
                break;
        }
    }

    protected override void ActivatorWakeUp() {
        base.ActivatorWakeUp();

        if(mRespawnReady) {
            SetPhysicsActive(true, true);

            //Debug.Log("respawned");
            mRespawnReady = false;
            state = (int)EntityState.Normal;
        }
        else {
            if(!respawnOnSleep && animator && animator.onDisableAction == AnimatorData.DisableAction.Pause) {
                animator.Resume();
            }
        }
    }

    protected override void ActivatorSleep() {
        base.ActivatorSleep();

        switch((EntityState)state) {
            case EntityState.RespawnWait:
                Restart();
                mRespawnReady = true;
                break;

            case EntityState.Normal:
            case EntityState.Hurt:
            case EntityState.Stun:
                Jump(0.0f);

                if(respawnOnSleep) {
                    SetPhysicsActive(false, false);

                    if(visibleGO)
                        visibleGO.SetActive(false);

                    ToRespawnWait();
					//mRespawnReady = true;
                }
                else if(releaseOnSleep) {
                    activator.ForceActivate();
                    Release();
                }
                break;

            case EntityState.Dead:
                if(toRespawnAuto) {
                    StopCoroutine(DoRespawnWaitDelayKey);
                    ToRespawnWait();
                }
                break;

            case EntityState.Invalid:
                break;
        }
    }

    public override void SpawnFinish() {
        //start ai, player control, etc
        mStats.isInvul = false;

        state = (int)EntityState.Normal;
    }

    protected override void SpawnStart() {
        //initialize some things
        mSpawnPos = transform.position;
        mSpawnRot = transform.rotation;

        if(GetComponent<Rigidbody>())
            mSpawnRigidBodyKinematic = GetComponent<Rigidbody>().isKinematic;
    }

    protected override void Awake() {
        base.Awake();

        mStats = GetComponent<Stats>();
        mStats.changeHPCallback += OnStatsHPChange;
        mStats.isInvul = true;

        mBodyCtrl = GetComponent<PlatformerController>();
        if(mBodyCtrl)
            mBodyCtrl.moveSideLock = true;

        mGravCtrl = GetComponent<GravityController>();

        mBodySpriteCtrl = GetComponent<PlatformerSpriteController>();

        if(!FSM)
            autoSpawnFinish = true;

        if(stunGO)
            stunGO.SetActive(false);

        if(activator)
            mDefaultDeactiveDelay = activator.deactivateDelay;

        if(deathActivateGO)
            deathActivateGO.SetActive(false);

        //initialize variables
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();

        //initialize variables from other sources (for communicating with managers, etc.)
    }

    protected void SetDamageTriggerActive(bool aActive) {
        if(mDamageTriggers == null || mDamageTriggers.Length == 0)
            mDamageTriggers = GetComponentsInChildren<Damage>(true);
        
        for(int i = 0, max = mDamageTriggers.Length; i < max; i++)
            mDamageTriggers[i].gameObject.SetActive(aActive);
    }

    protected void SetPhysicsActive(bool aActive, bool excludeCollision) {
        if(GetComponent<Rigidbody>()) {
            if(!mSpawnRigidBodyKinematic) {
                GetComponent<Rigidbody>().isKinematic = !aActive;
            }

            if(aActive || !excludeCollision)
                GetComponent<Rigidbody>().detectCollisions = aActive;
        }

        if(GetComponent<Collider>() && (aActive || !excludeCollision)) {
            GetComponent<Collider>().enabled = aActive;
        }

        if(mGravCtrl) {
            mGravCtrl.enabled = aActive;
        }

        if(animator) {
            if(aActive) {
                if(animator.isPaused)
                    animator.Resume();
            }
            else {
                if(animator.isPlaying)
                    animator.Pause();
            }
        }

        if(mBodyCtrl)
            mBodyCtrl.enabled = aActive;

        SetDamageTriggerActive(aActive);
    }

    /// <summary>
    /// This is called after death or when set to sleep,
    /// Use this to reset states.  The base will reset stats and telemetry
    /// </summary>
    protected virtual void Restart() {
        //reset physics
        SetPhysicsActive(true, false);

        if(animator)
            animator.Stop();

        if(visibleGO)
            visibleGO.SetActive(true);

        //reset blink
        Blink(0.0f);

        mStats.Reset();
        mStats.isInvul = true;

        if(FSM)
            FSM.Reset();

        if(mBodyCtrl)
            mBodyCtrl.enabled = true;

        Jump(0.0f);

        if(stunGO)
            stunGO.SetActive(false);
                
        StopCoroutine(DoRespawnWaitDelayKey);

        mLastStunnedTime = 0.0f;
    }

    protected void RevertTransform() {
        if(animator)
            animator.Stop();

        transform.position = mSpawnPos;
        transform.rotation = mSpawnRot;
    }

    protected virtual void OnStatsHPChange(Stats stat, float delta) {
        if(mUseBossHP)
            HUD.instance.barBoss.current = Mathf.CeilToInt(stat.curHP);

        if(stat.curHP <= 0.0f) {
            state = (int)EntityState.Dead;
        }
        else if(delta < 0.0f) {
            if(stat.lastDamageSource != null && stat.lastDamageSource.stun && !stat.stunImmune && Time.time - mLastStunnedTime > stunImmuneDelay)
                state = (int)EntityState.Stun;
            else
                SoundPlayerGlobal.instance.Play(soundHurt);
        }
    }

    protected virtual void OnSuddenDeath() {
        mIsSuddenDeath = true;
        stats.curHP = 0;
    }

    private const string DoRespawnWaitDelayKey = "DoRespawnWaitDelay";
    IEnumerator DoRespawnWaitDelay() {
        if(toRespawnDelay > 0.0f) {
            yield return new WaitForSeconds(toRespawnDelay);
        }
        else {
            yield return new WaitForFixedUpdate();
        }

        ToRespawnWait();
    }

    private const string JumpFinishKey = "JumpFinish";
    void JumpFinish() {
        mBodyCtrl.Jump(false);
    }

    void DoStun() {
        if(state == (int)EntityState.Stun) {
            mLastStunnedTime = Time.time;
            state = (int)EntityState.Normal;
        }
    }

    void OnHPBarFilled(UIEnergyBar bar) {
        Player.instance.Pause(false);

        HUD.instance.barBoss.animateEndCallback -= OnHPBarFilled;
    }

    protected virtual void OnDrawGizmosSelected() {
        if(!string.IsNullOrEmpty(deathSpawnGroup) && !string.IsNullOrEmpty(deathSpawnType)) {
            Color clr = Color.cyan;
            clr.a = 0.3f;
            Gizmos.color = clr;
            Gizmos.DrawSphere((deathSpawnAttach ? deathSpawnAttach : transform).localToWorldMatrix.MultiplyPoint(deathSpawnOfs), 0.25f);
        }
    }
}
