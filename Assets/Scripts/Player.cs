using UnityEngine;
using System.Collections;

public class Player : EntityBase {
    public const string clipHurt = "hurt";
    public float hurtForce = 15.0f;
    public float hurtInvulDelay = 0.5f;
    public float deathFinishDelay = 2.0f;
    public float slideForce;
    public float slideSpeedMax;
    public float slideDelay;
    public float slideHeight = 0.79f;
    public ParticleSystem slideParticle;
    public GameObject deathGOActivate;
    public LayerMask solidMask; //use for standing up, etc.

    public Weapon[] weapons;

    public GameObject[] armorDisplayGOs;

    public bool saveLevelComplete = true;
    public bool levelCompletePersist = true;
    public bool preserveEnergySpent = false; //when moving to next level

    public SoundPlayer sfxWallJump;
    public SoundPlayer sfxLanded;
    public SoundPlayer sfxSlide;
    public SoundPlayer sfxHurt;

    private static Player mInstance;
    private PlayerStats mStats;
    private PlatformerController mCtrl;
    private PlatformerSpriteController mCtrlSpr;
    private SpriteColorBlink[] mBlinks;
    private float mDefaultCtrlMoveForce;
    private float mDefaultCtrlMoveMaxSpeed;
    private Vector3 mDefaultColliderCenter;
    private float mDefaultColliderHeight;
    private CapsuleCollider mCapsuleColl;
    private bool mInputEnabled;
    private bool mSliding;
    private float mSlidingLastTime;
    private bool mHurtActive;
    private int mCurWeaponInd = -1;
    private int mPauseCounter;
    private bool mAllowPauseTime = true;

    private PlayMakerFSM mFireFSM;

    public static Player instance { get { return mInstance; } }

    public int currentWeaponIndex {
        get { return mCurWeaponInd; }
        set {
            if(mCurWeaponInd != value && (value == -1 || (SlotInfo.WeaponIsUnlock(value) && weapons[value] != null))) {
                int prevWeaponInd = mCurWeaponInd;
                mCurWeaponInd = value;

                //disable previous
                if(prevWeaponInd >= 0 && prevWeaponInd < weapons.Length && weapons[prevWeaponInd]) {
                    weapons[prevWeaponInd].FireCancel();
                    weapons[prevWeaponInd].gameObject.SetActive(false);
                }

                //enable new one
                if(mCurWeaponInd >= 0) {
                    Weapon weapon = weapons[mCurWeaponInd];

                    //show energy thing
                    HUD hud = HUD.instance;

                    if(weapon.energyType != Weapon.EnergyType.Unlimited) {
                        hud.barEnergy.gameObject.SetActive(true);

                        hud.barEnergy.SetBarColor(weapon.color);
                        hud.barEnergy.SetIconSprite(weapon.iconSpriteRef);
                        hud.barEnergy.max = Mathf.CeilToInt(Weapon.weaponEnergyDefaultMax);
                        hud.barEnergy.current = Mathf.CeilToInt(weapon.currentEnergy);
                    } else {
                        hud.barEnergy.gameObject.SetActive(false);
                    }

                    mCtrlSpr.animLibIndex = weapon.playerAnimIndex;

                    weapon.gameObject.SetActive(true);
                } else {
                    mCtrlSpr.animLibIndex = -1;
                    HUD.instance.barEnergy.gameObject.SetActive(false);
                }
            }
        }
    }

    public Weapon currentWeapon {
        get {
            if(mCurWeaponInd >= 0)
                return weapons[mCurWeaponInd];
            return null;
        }
    }

    /// <summary>
    /// Returns the weapon with the current lowest energy
    /// </summary>
    public Weapon lowestEnergyWeapon {
        get {
            Weapon lowestWpn = null;
            for(int i = 0, max = weapons.Length; i < max; i++) {
                Weapon wpn = weapons[i];
                if(wpn && wpn.energyType != Weapon.EnergyType.Unlimited && !wpn.isMaxEnergy) {
                    if(lowestWpn) {
                        if(wpn.currentEnergy < lowestWpn.currentEnergy)
                            lowestWpn = wpn;
                    } else
                        lowestWpn = wpn;
                }
            }
            return lowestWpn;
        }
    }

    public float controllerDefaultMaxSpeed {
        get { return mDefaultCtrlMoveMaxSpeed; }
    }

    public float controllerDefaultForce {
        get { return mDefaultCtrlMoveForce; }
    }

    public bool inputEnabled {
        get { return mInputEnabled; }
        set {
            if(mInputEnabled != value) {
                mInputEnabled = value;

                InputManager input = Main.instance != null ? Main.instance.input : null;

                if(input) {
                    if(mInputEnabled) {
                        input.AddButtonCall(0, InputAction.Fire, OnInputFire);
                        input.AddButtonCall(0, InputAction.PowerNext, OnInputPowerNext);
                        input.AddButtonCall(0, InputAction.PowerPrev, OnInputPowerPrev);
                        input.AddButtonCall(0, InputAction.Jump, OnInputJump);
                        input.AddButtonCall(0, InputAction.Slide, OnInputSlide);
                    } else {
                        input.RemoveButtonCall(0, InputAction.Fire, OnInputFire);
                        input.RemoveButtonCall(0, InputAction.PowerNext, OnInputPowerNext);
                        input.RemoveButtonCall(0, InputAction.PowerPrev, OnInputPowerPrev);
                        input.RemoveButtonCall(0, InputAction.Jump, OnInputJump);
                        input.RemoveButtonCall(0, InputAction.Slide, OnInputSlide);
                    }
                }

                mCtrl.inputEnabled = mInputEnabled;
            }
        }
    }

    public bool allowPauseTime { 
        get { return mAllowPauseTime; } 
        set {
            if(mAllowPauseTime != value) {
                mAllowPauseTime = value; 

                if(!mAllowPauseTime && mPauseCounter > 0)
                    Main.instance.sceneManager.Resume();
            }
        } 
    }

    public PlatformerController controller { get { return mCtrl; } }

    public PlatformerSpriteController controllerSprite { get { return mCtrlSpr; } }

    public PlayerStats stats { get { return mStats; } }

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Hurt:
                mHurtActive = false;
                break;

            case EntityState.Lock:
                inputEnabled = true;

                InputManager input = Main.instance != null ? Main.instance.input : null;
                if(input) {
                    input.AddButtonCall(0, InputAction.MenuEscape, OnInputPause);
                }

                mStats.isInvul = false;

                mCtrl.moveSideLock = false;

                LevelController.instance.TimeResume();
                break;
        }

        switch((EntityState)state) {
            case EntityState.Normal:
                inputEnabled = true;
                break;

            case EntityState.Hurt:
                if(mCurWeaponInd >= 0) {
                    Weapon curWpn = weapons[mCurWeaponInd];
                    if(curWpn.stopOnHurt)
                        curWpn.FireStop();
                }
                
                Blink(hurtInvulDelay);

                //check to see if we can stop sliding, then do hurt
                SetSlide(false);
                if(!mSliding) {
                    inputEnabled = false;

                    mCtrlSpr.PlayOverrideClip(clipHurt);

                    StartCoroutine(DoHurtForce(mStats.lastDamageNormal));
                }

                sfxHurt.Play();
                break;

            case EntityState.Dead:
                {
                    UIModalManager.instance.ModalCloseAll();

                    stats.EnergyShieldSetActive(false);

                    if(mCurWeaponInd >= 0)
                        weapons[mCurWeaponInd].FireStop();

                    SetSlide(false);
                    
                    mCtrl.enabled = false;
                    rigidbody.isKinematic = true;
                    rigidbody.detectCollisions = false;
                    collider.enabled = false;

                    //disable all input
                    inputEnabled = false;

                    InputManager input = Main.instance != null ? Main.instance.input : null;
                    if(input) {
                        input.RemoveButtonCall(0, InputAction.MenuEscape, OnInputPause);
                    }
                    //

                    mCtrlSpr.anim.gameObject.SetActive(false);

                    if(deathGOActivate)
                        deathGOActivate.SetActive(true);

                    PlayerStats.curLife--;

                    bool isHardcore = SlotInfo.gameMode == SlotInfo.GameMode.Hardcore;
                    if(isHardcore && PlayerStats.curLife == 0)
                        SlotInfo.SetDead(true);

                    //save when we die
                    if(!UserData.instance.autoSave) {
                        UserData.instance.autoSave = true;
                        UserData.instance.Save();
                        SlotInfo.SaveCurrentSlotData();
                        PlayerPrefs.Save();
                    }

                    StartCoroutine(DoDeathFinishDelay());
                }
                break;

            case EntityState.Lock:
                UIModalManager.instance.ModalCloseAll();
                if(currentWeapon)
                    currentWeapon.FireStop();

                LockControls();
                break;

            case EntityState.Victory:
                UIModalManager.instance.ModalCloseAll();
                if(currentWeapon)
                    currentWeapon.FireStop();

                stats.EnergyShieldSetActive(false);

                currentWeaponIndex = -1;
                LockControls();
                mCtrlSpr.PlayOverrideClip("victory");

                if(saveLevelComplete)
                    LevelController.Complete(levelCompletePersist);

                //ok to save now
                UserData.instance.autoSave = true;
                SlotInfo.SaveCurrentSlotData();
                PlayerPrefs.Save();
                break;

            case EntityState.Final:
                UIModalManager.instance.ModalCloseAll();
                if(currentWeapon)
                    currentWeapon.FireStop();

                stats.EnergyShieldSetActive(false);
                
                currentWeaponIndex = -1;
                LockControls();

                if(saveLevelComplete)
                    LevelController.Complete(levelCompletePersist);

                SlotInfo.ComputeClearTime();
                SlotInfo.SaveCurrentSlotData();
                PlayerPrefs.Save();
                break;

            case EntityState.Exit:
                UIModalManager.instance.ModalCloseAll();
                if(currentWeapon)
                    currentWeapon.FireStop();

                stats.EnergyShieldSetActive(false);

                currentWeaponIndex = -1;
                LockControls();
                break;

            case EntityState.Invalid:
                inputEnabled = false;
                break;
        }
    }

    void LockControls() {
        SetSlide(false);

        //disable all input
        inputEnabled = false;

        InputManager input = Main.instance != null ? Main.instance.input : null;
        if(input) {
            input.RemoveButtonCall(0, InputAction.MenuEscape, OnInputPause);
        }
        //

        mStats.isInvul = true;

        mCtrl.moveSideLock = true;
        mCtrl.moveSide = 0.0f;
        //mCtrl.ResetCollision();

        LevelController.instance.TimePause();
    }

    protected override void SetBlink(bool blink) {
        foreach(SpriteColorBlink blinker in mBlinks) {
            blinker.enabled = blink;
        }

        mStats.isInvul = blink;
    }

    protected override void OnDespawned() {
        //reset stuff here

        base.OnDespawned();
    }

    protected override void OnDestroy() {
        mInstance = null;

        //dealloc here
        inputEnabled = false;

        InputManager input = Main.instance != null ? Main.instance.input : null;
        if(input) {
            input.RemoveButtonCall(0, InputAction.MenuEscape, OnInputPause);
        }

        base.OnDestroy();
    }

    public override void SpawnFinish() {
        state = (int)EntityState.Normal;

        LevelController.instance.TimeStart();

        if(SceneState.instance.GetGlobalValue("cheat") > 0) {
            stats.damageReduction = 1.0f;
        }
    }

    public void RefreshArmor() {
        if(armorDisplayGOs != null) {
            for(int i = 0; i < armorDisplayGOs.Length; i++)
                armorDisplayGOs[i].SetActive(SlotInfo.isArmorAcquired);
        }
    }

    protected override void SpawnStart() {
        //initialize some things

        //start ai, player control, etc
        currentWeaponIndex = 0;
        
        RefreshArmor();
    }

    protected override void Awake() {
        mInstance = this;

        base.Awake();

        //CameraController camCtrl = CameraController.instance;
        //camCtrl.transform.position = collider.bounds.center;

        //initialize variables
        Main.instance.input.AddButtonCall(0, InputAction.MenuEscape, OnInputPause);

        mCtrl = GetComponent<PlatformerController>();
        mCtrl.moveInputX = InputAction.MoveX;
        mCtrl.moveInputY = InputAction.MoveY;
        mCtrl.collisionEnterCallback += OnRigidbodyCollisionEnter;
        mCtrl.landCallback += OnLand;

        mDefaultCtrlMoveMaxSpeed = mCtrl.moveMaxSpeed;
        mDefaultCtrlMoveForce = mCtrl.moveForce;

        mCtrlSpr = GetComponent<PlatformerSpriteController>();

        mCtrlSpr.clipFinishCallback += OnSpriteCtrlOneTimeClipEnd;

        mCapsuleColl = collider as CapsuleCollider;
        mDefaultColliderCenter = mCapsuleColl.center;
        mDefaultColliderHeight = mCapsuleColl.height;

        mStats = GetComponent<PlayerStats>();

        mBlinks = GetComponentsInChildren<SpriteColorBlink>(true);
        foreach(SpriteColorBlink blinker in mBlinks) {
            blinker.enabled = false;
        }

        if(deathGOActivate)
            deathGOActivate.SetActive(false);

        //disable autosave if hardcore
        if(SlotInfo.gameMode == SlotInfo.GameMode.Hardcore) {
            //preserveEnergySpent = true;
            mStats.hpPersist = true;
            PlayerStats.savePersist = true;
            UserData.instance.autoSave = LevelController.isLevelLoadedComplete;
        }
        else
            PlayerStats.savePersist = false;
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();

        foreach(Weapon weapon in weapons) {
            if(weapon) {
                weapon.energyChangeCallback += OnWeaponEnergyCallback;
                weapon.gameObject.SetActive(false);
            }
        }

        //initialize variables from other sources (for communicating with managers, etc.)
        LevelController.CheckpointApplyTo(transform);
        LevelController.CheckpointApplyTo(CameraController.instance.transform);

        //initialize hp stuff
        HUD.instance.barHP.max = Mathf.CeilToInt(mStats.maxHP);
        HUD.instance.barHP.current = Mathf.CeilToInt(mStats.curHP);

        mStats.changeHPCallback += OnStatsHPChange;
        mStats.changeMaxHPCallback += OnStatsHPMaxChange;

        HUD.instance.barHP.animateEndCallback += OnEnergyAnimStop;
        HUD.instance.barEnergy.animateEndCallback += OnEnergyAnimStop;

        HUD.instance.RefreshLifeCount();
    }

    void OnTriggerEnter(Collider col) {
        if(col.CompareTag("FireTrigger")) {
            PlayMakerFSM fsm = col.GetComponent<PlayMakerFSM>();
            if(fsm) {
                mFireFSM = fsm;
            }
        }
    }

    void OnTriggerExit(Collider col) {
        if(mFireFSM && col == mFireFSM.collider) {
            mFireFSM = null;
        }
    }

    void Update() {
        if(mSliding) {
            InputManager input = Main.instance.input;

            float inpX = input.GetAxis(0, InputAction.MoveX);
            if(inpX < -0.1f)
                mCtrl.moveSide = -1.0f;
            else if(inpX > 0.1f)
                mCtrl.moveSide = 1.0f;

            if(!slideParticle.isPlaying)
                slideParticle.Play();

            if(Time.time - mSlidingLastTime >= slideDelay) {
                SetSlide(false);
            }
        }
    }

    //stats/weapons

    void OnStatsHPChange(Stats stat, float delta) {
        if(delta < 0.0f) {
            if(stat.curHP <= 0.0f) {
                state = (int)EntityState.Dead;
            } else {
                state = (int)EntityState.Hurt;
            }

            HUD.instance.barHP.current = Mathf.CeilToInt(stat.curHP);
        } else {
            //healed
            if(!HUD.instance.barHP.isAnimating)
                Pause(true);

            HUD.instance.barHP.currentSmooth = Mathf.CeilToInt(stat.curHP);
        }
    }

    void OnStatsHPMaxChange(Stats stat, float delta) {
        HUD.instance.barHP.max = Mathf.CeilToInt(stat.maxHP);
    }

    void OnWeaponEnergyCallback(Weapon weapon, float delta) {
        if(weapon == weapons[mCurWeaponInd]) {
            if(delta <= 0.0f) {
                HUD.instance.barEnergy.current = Mathf.CeilToInt(weapon.currentEnergy);
            } else {
                if(!HUD.instance.barEnergy.isAnimating)
                    Pause(true);

                HUD.instance.barEnergy.currentSmooth = Mathf.CeilToInt(weapon.currentEnergy);
            }
        }
    }

    IEnumerator DoHurtForce(Vector3 normal) {
        mHurtActive = true;

        mCtrl.enabled = false;
        rigidbody.velocity = Vector3.zero;
        rigidbody.drag = 0.0f;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        normal.x = Mathf.Sign(normal.x);
        normal.y = 0.0f;
        normal.z = 0.0f;

        while(mHurtActive) {
            yield return wait;

            rigidbody.AddForce(normal * hurtForce);
        }

        mCtrl.enabled = true;
        mCtrl.ResetCollision();

        mHurtActive = false;
    }

    //anim

    void OnSpriteCtrlOneTimeClipEnd(PlatformerSpriteController ctrl, tk2dSpriteAnimationClip clip) {
        if(clip.name == clipHurt) {
            if(state == (int)EntityState.Hurt)
                state = (int)EntityState.Normal;
        }
    }

    void OnEnergyAnimStop(UIEnergyBar bar) {
        Pause(false);
    }

    //input

    void OnInputFire(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            if(mFireFSM) {
                if(currentWeapon)
                    currentWeapon.FireStop();

                mFireFSM.SendEvent(EntityEvent.Interact);
            }
            else if(currentWeapon) {
                if(currentWeapon.allowSlide || !mSliding) {
                    if(currentWeapon.hasEnergy) {
                        currentWeapon.FireStart();
                    }
                    else {
                        HUD.instance.barEnergy.Flash(true);
                    }
                }
            }
        } else if(dat.state == InputManager.State.Released) {
            if(!mFireFSM && currentWeapon) {
                currentWeapon.FireStop();
            }
        }
    }

    void OnInputPowerNext(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            for(int i = 0, max = weapons.Length, toWeaponInd = currentWeaponIndex + 1; i < max; i++, toWeaponInd++) {
                if(toWeaponInd >= weapons.Length)
                    toWeaponInd = 0;

                if(weapons[toWeaponInd] && SlotInfo.WeaponIsUnlock(toWeaponInd)) {
                    currentWeaponIndex = toWeaponInd;
                    break;
                }
            }
        }
    }

    void OnInputPowerPrev(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            for(int i = 0, max = weapons.Length, toWeaponInd = currentWeaponIndex - 1; i < max; i++, toWeaponInd--) {
                if(toWeaponInd < 0)
                    toWeaponInd = weapons.Length - 1;

                if(weapons[toWeaponInd] && SlotInfo.WeaponIsUnlock(toWeaponInd)) {
                    currentWeaponIndex = toWeaponInd;
                    break;
                }
            }
        }
    }

    void OnInputJump(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            InputManager input = Main.instance.input;

            if(!mSliding) {
                if(input.GetAxis(0, InputAction.MoveY) < -0.1f && mCtrl.isGrounded) {
                    Weapon curWpn = weapons[mCurWeaponInd];
                    if(!curWpn.isFireActive || curWpn.allowSlide)
                        SetSlide(true);

                } else {
                    mCtrl.Jump(true);
                    if(mCtrl.isJumpWall) {
                        Vector2 p = mCtrlSpr.wallStickParticle.transform.position;
                        PoolController.Spawn("fxp", "wallSpark", "wallSpark", null, p);
                        sfxWallJump.Play();
                    }
                }
            } else {
                if(input.GetAxis(0, InputAction.MoveY) >= 0.0f) {
                    //if we can stop sliding, then jump
                    SetSlide(false, false);
                    if(!mSliding) {
                        mCtrl.Jump(true);
                    }
                }
            }
        } else if(dat.state == InputManager.State.Released) {
            mCtrl.Jump(false);
        }
    }

    void OnInputSlide(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            if(!mSliding) {
                Weapon curWpn = weapons[mCurWeaponInd];
                if((!curWpn.isFireActive || curWpn.allowSlide) && mCtrl.isGrounded)
                    SetSlide(true);
            }
        }
    }

    void OnInputPause(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            if(state != (int)EntityState.Dead && state != (int)EntityState.Victory && state != (int)EntityState.Final) {
                if(UIModalManager.instance.activeCount == 0 && !UIModalManager.instance.ModalIsInStack("pause")) {
                    UIModalManager.instance.ModalOpen("pause");
                }
            }
        }
    }

    void OnSuddenDeath() {
        stats.curHP = 0;
    }

    //misc

    void OnUIModalActive() {
        Pause(true);
    }

    void OnUIModalInactive() {
        Pause(false);
    }

    public void Pause(bool pause) {
        if(pause) {
            mPauseCounter++;
            if(mPauseCounter == 1) {
                if(mAllowPauseTime)
                    Main.instance.sceneManager.Pause();

                inputEnabled = false;

                Main.instance.input.RemoveButtonCall(0, InputAction.MenuEscape, OnInputPause);

                LevelController.instance.TimePause();
            }
        } else {
            mPauseCounter--;
            if(mPauseCounter == 0) {
                if(mAllowPauseTime)
                    Main.instance.sceneManager.Resume();

                if(state != (int)EntityState.Lock && state != (int)EntityState.Invalid) {
                    inputEnabled = true;

                    Main.instance.input.AddButtonCall(0, InputAction.MenuEscape, OnInputPause);

                    LevelController.instance.TimeResume();
                }
            }
        }
    }

    void SetSlide(bool slide, bool clearVelocity = true) {
        if(mSliding != slide) {
            mSliding = slide;

            if(mSliding) {
                mSlidingLastTime = Time.time;

                mCapsuleColl.height = slideHeight;
                mCapsuleColl.center = new Vector3(mDefaultColliderCenter.x, mDefaultColliderCenter.y - (mDefaultColliderHeight - slideHeight) * 0.5f, mDefaultColliderCenter.z);

                mCtrl.moveMaxSpeed = slideSpeedMax;
                mCtrl.moveForce = slideForce;
                mCtrl.moveSideLock = true;
                mCtrl.moveSide = mCtrlSpr.isLeft ? -1.0f : 1.0f;

                mCtrlSpr.state = PlatformerSpriteController.State.Slide;

                sfxSlide.Play();
            } else {
                //cannot set to false if we can't stand
                if(CanStand()) {
                    //revert
                    mCapsuleColl.height = mDefaultColliderHeight;
                    mCapsuleColl.center = mDefaultColliderCenter;

                    mCtrl.moveMaxSpeed = mDefaultCtrlMoveMaxSpeed;
                    mCtrl.moveSideLock = false;
                    mCtrl.moveForce = mDefaultCtrlMoveForce;

                    if(clearVelocity) {
                        mCtrl.moveSide = 0.0f;

                        if(!rigidbody.isKinematic) {
                            Vector3 v = rigidbody.velocity; v.x = 0.0f; v.z = 0.0f;
                            rigidbody.velocity = v;
                        }
                    } else {
                        //limit x velocity
                        Vector3 v = rigidbody.velocity;
                        if(Mathf.Abs(v.x) > 12.0f) {
                            v.x = Mathf.Sign(v.x) * 12.0f;
                            rigidbody.velocity = v;
                        }
                    }

                    mCtrlSpr.state = PlatformerSpriteController.State.None;

                    //Vector3 pos = transform.position;
                    //pos.y += (mDefaultColliderHeight - slideHeight) * 0.5f - 0.1f;
                    //transform.position = pos;

                    slideParticle.Stop();
                    slideParticle.Clear();
                } else {
                    mSliding = true;
                }
            }
        }
    }

    bool CanStand() {
        const float ofs = 0.2f;

        float r = mCapsuleColl.radius - 0.05f;

        Vector3 c = transform.position + mDefaultColliderCenter;
        Vector3 u = new Vector3(c.x, c.y + (mDefaultColliderHeight * 0.5f - mCapsuleColl.radius) + ofs, c.z);
        Vector3 d = new Vector3(c.x, (c.y - (mDefaultColliderHeight * 0.5f - mCapsuleColl.radius)) + ofs, c.z);

        return !Physics.CheckCapsule(u, d, r, solidMask);
    }

    void SceneChange(string nextScene) {
        //Debug.Log("scene from: " + Application.loadedLevelName + " to: " + nextScene);

        bool isHardcore = SlotInfo.gameMode == SlotInfo.GameMode.Hardcore;

        mStats.SaveStates();

        if(nextScene == Application.loadedLevelName) {
            //restarting level
            foreach(Weapon weapon in weapons) {
                if(weapon)
                    weapon.SaveEnergySpent(isHardcore);
            }
        } else {
            LevelController.CheckpointReset();
            LevelController.LevelStateReset();

            if(!preserveEnergySpent || nextScene == Scenes.gameover) {
                foreach(Weapon weapon in weapons) {
                    if(weapon)
                        weapon.ResetEnergySpent(isHardcore);
                }
            }
            else {
                foreach(Weapon weapon in weapons) {
                    if(weapon)
                        weapon.SaveEnergySpent(isHardcore);
                }
            }

            if(!isHardcore) {
                if(PlayerStats.curLife < PlayerStats.defaultNumLives) {
                    PlayerStats.curLife = PlayerStats.defaultNumLives;
                }

                SceneState.instance.DeleteGlobalValue(PlayerStats.hpKey, false);
            }
        }

        if(UserData.instance.autoSave) {
            SlotInfo.SaveCurrentSlotData();
            UserData.instance.Save();
            PlayerPrefs.Save();
        }
    }

    IEnumerator DoDeathFinishDelay() {
        yield return new WaitForSeconds(deathFinishDelay);

        if(PlayerStats.curLife > 0) {
            Main.instance.sceneManager.Reload();
        } else {
            Main.instance.sceneManager.LoadScene(Scenes.gameover);
        }
    }

    void OnRigidbodyCollisionEnter(RigidBodyController controller, Collision col) {
        if(col.collider.CompareTag("DropDamage")) {
            RigidBodyController.CollideInfo inf = controller.GetCollideInfo(col.collider);
            //Debug.Log("infflag: "+inf.flag);
            if(inf != null && (inf.flag & CollisionFlags.Above) != CollisionFlags.None && col.relativeVelocity.sqrMagnitude >= 100.0f) {
                Damage dmg = col.gameObject.GetComponent<Damage>();
                if(dmg) {
                    dmg.CallDamageTo(gameObject, col.contacts[0].point, col.contacts[0].normal);
                }
            }
        }
    }

    void OnLand(PlatformerController ctrl) {
        if(state != (int)EntityState.Invalid) {
            Vector2 p = transform.position;
            PoolController.Spawn("fxp", "landdust", "landdust", null, p);
            sfxLanded.Play();
        }
    }
}
