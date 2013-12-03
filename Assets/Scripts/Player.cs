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
    public GameObject deathGOActivate;
    public LayerMask solidMask; //use for standing up, etc.

    public Weapon[] weapons;
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

    public static Player instance { get { return mInstance; } }

    public int currentWeaponIndex {
        get { return mCurWeaponInd; }
        set {
            if(mCurWeaponInd != value && (value == -1 || (Weapon.IsAvailable(value) && weapons[value] != null))) {
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
                    } else {
                        input.RemoveButtonCall(0, InputAction.Fire, OnInputFire);
                        input.RemoveButtonCall(0, InputAction.PowerNext, OnInputPowerNext);
                        input.RemoveButtonCall(0, InputAction.PowerPrev, OnInputPowerPrev);
                        input.RemoveButtonCall(0, InputAction.Jump, OnInputJump);
                    }
                }

                mCtrl.inputEnabled = mInputEnabled;
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
                break;

            case EntityState.Dead:
                {
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

                    StartCoroutine(DoDeathFinishDelay());
                }
                break;

            case EntityState.Lock:
                LockControls();
                break;

            case EntityState.Victory:
                currentWeaponIndex = -1;
                LockControls();
                mCtrlSpr.PlayOverrideClip("victory");

                LevelController.Complete();
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
        mCtrl.ResetCollision();
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
        //start ai, player control, etc
        currentWeaponIndex = 0;

        state = (int)EntityState.Normal;
    }

    protected override void SpawnStart() {
        //initialize some things
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

    void Update() {
        if(mSliding) {
            InputManager input = Main.instance.input;

            float inpX = input.GetAxis(0, InputAction.MoveX);
            if(inpX < -0.1f)
                mCtrl.moveSide = -1.0f;
            else if(inpX > 0.1f)
                mCtrl.moveSide = 1.0f;

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
            if(currentWeapon) {
                if(currentWeapon.allowSlide || !mSliding)
                    currentWeapon.FireStart();
            }
        } else if(dat.state == InputManager.State.Released) {
            if(currentWeapon) {
                currentWeapon.FireStop();
            }
        }
    }

    void OnInputPowerNext(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            for(int i = 0, max = weapons.Length, toWeaponInd = currentWeaponIndex + 1; i < max; i++, toWeaponInd++) {
                if(toWeaponInd >= weapons.Length)
                    toWeaponInd = 0;

                if(weapons[toWeaponInd] && Weapon.IsAvailable(toWeaponInd)) {
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

                if(weapons[toWeaponInd] && Weapon.IsAvailable(toWeaponInd)) {
                    currentWeaponIndex = toWeaponInd;
                    break;
                }
            }
        }
    }

    void OnInputJump(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            if(!mSliding) {
                InputManager input = Main.instance.input;

                if(input.GetAxis(0, InputAction.MoveY) < -0.1f && mCtrl.isGrounded) {
                    Weapon curWpn = weapons[mCurWeaponInd];
                    if(!curWpn.isFireActive || curWpn.allowSlide)
                        SetSlide(true);

                } else {
                    mCtrl.Jump(true);
                }
            } else {
                //if we can stop sliding, then jump
                SetSlide(false, false);
                if(!mSliding) {
                    mCtrl.Jump(true);
                }
            }
        } else if(dat.state == InputManager.State.Released) {
            mCtrl.Jump(false);
        }
    }

    void OnInputPause(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            if(!UIModalManager.instance.ModalIsInStack("pause")) {
                UIModalManager.instance.ModalOpen("pause");
            }
        }
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
                Main.instance.sceneManager.Pause();
                inputEnabled = false;

                Main.instance.input.RemoveButtonCall(0, InputAction.MenuEscape, OnInputPause);
            }
        } else {
            mPauseCounter--;
            if(mPauseCounter == 0) {
                Main.instance.sceneManager.Resume();
                inputEnabled = true;

                Main.instance.input.AddButtonCall(0, InputAction.MenuEscape, OnInputPause);
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
                        rigidbody.velocity = Vector3.zero;
                    } else {
                        //limit x velocity
                        Vector3 v = rigidbody.velocity;
                        if(Mathf.Abs(v.x) > 12.0f) {
                            v.x = Mathf.Sign(v.x) * 12.0f;
                            rigidbody.velocity = v;
                        }
                    }

                    mCtrlSpr.state = PlatformerSpriteController.State.None;

                    Vector3 pos = transform.position;
                    pos.y += (mDefaultColliderHeight - slideHeight) * 0.5f - 0.1f;
                    transform.position = pos;
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

        mStats.SaveStates();

        if(nextScene == Application.loadedLevelName) {
            //restarting level
            foreach(Weapon weapon in weapons) {
                if(weapon)
                    weapon.SaveEnergySpent();
            }
        } else {
            LevelController.CheckpointReset();

            foreach(Weapon weapon in weapons) {
                if(weapon)
                    weapon.ResetEnergySpent();
            }

            if(nextScene == Scenes.gameover) {
                PlayerStats.curLife = PlayerStats.defaultNumLives;
            }
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
}
