using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour {
    public enum EnergyType {
        Unlimited = -1,

        LightningFork,
        ConflictResolver,
        TimeWarp,
        Whip,
        HoolaHoop,
        Clone,
        Happy,
        NumTypes
    }

    public enum AnimState {
        normal,
        attack,
        charge
    }

    [System.Serializable]
    public class ChargeInfo {
        public float energyCost;

        public GameObject go;
        public float delay;
        public string projType;
        public ParticleSystem particles;

        public SoundPlayer sfx;

        public bool chargeSfxOn;
        public float chargeSfxPitch;

        public void Enable(bool enable) {
            if(particles) {
                if(enable) {
                    particles.loop = true;
                    particles.Play();
                }
                else {
                    particles.loop = false;
                }
            }

            if(go)
                go.SetActive(enable);
        }
    }

    public delegate void ChangeValueCallback(Weapon weapon, float delta);

    public const string weaponEnergyPrefix = "wpnE";
    public const float weaponEnergyDefaultMax = 32.0f;

    [SerializeField]
    string _iconSpriteRef;

    [SerializeField]
    string _gitGirlSpriteRef;

    [SerializeField]
    string _labelTextRef;

    public int playerAnimIndex = -1; //-1 is default

    public EnergyType energyType = EnergyType.Unlimited;

    public Color color = Color.white;

    public tk2dSpriteAnimator anim;
    public GameObject activeGO;

    public string projGroup = "projPlayer";
    public int projMax = 4;

    public bool stopOnHurt = false;
    public bool allowSlide = true; //allow slide while we are active?

    //first charge is the regular fire
    public ChargeInfo[] charges;

    public SoundPlayer chargeSfx;

    public float lockAfterChargeDelay = 0.1f;

    public event ChangeValueCallback energyChangeCallback;

    [SerializeField]
    Transform _spawnPoint;

    protected tk2dSpriteAnimationClip[] mClips;

    protected bool mFireActive = false;
    private int mCurChargeLevel = 0;
    protected int mCurProjCount = 0;
    protected bool mStarted = false;
    protected bool mFireCancel = false;

    private float mCurTime;

    private float mCurEnergy;
    private float mLastChargeShotTime;

    public static string GetWeaponEnergyKey(EnergyType type) {
        if(type == EnergyType.Unlimited || type == EnergyType.NumTypes)
            return null;

        return weaponEnergyPrefix + ((int)type);
    }

    public static void ResetWeaponEnergies() {
        SceneState.instance.DeleteValuesByNameContain(weaponEnergyPrefix);
    }

    public string iconSpriteRef { get { return _iconSpriteRef; } }
    public string gitGirlSpriteRef { get { return _gitGirlSpriteRef; } }
    public string labelTextRef { get { return _labelTextRef; } }
    public string labelText { get { return GameLocalize.GetText(_labelTextRef); } }

    public string energyTypeKey {
        get { return GetWeaponEnergyKey(energyType); }
    }

    public float currentEnergy {
        get { return mCurEnergy; }
        set {
            if(energyType != EnergyType.Unlimited) {
                float newVal = Mathf.Clamp(value, 0.0f, weaponEnergyDefaultMax);
                if(mCurEnergy != newVal) {
                    float prevVal = mCurEnergy;
                    mCurEnergy = newVal;

                    if(energyChangeCallback != null)
                        energyChangeCallback(this, mCurEnergy - prevVal);
                }
            }
        }
    }

    public int currentChargeLevel {
        get { return mCurChargeLevel; }
    }

    public bool isMaxEnergy {
        get { return energyType == EnergyType.Unlimited || mCurEnergy >= weaponEnergyDefaultMax; }
    }

    public virtual bool hasEnergy {
        get { return energyType == EnergyType.Unlimited || (mCurEnergy > 0.0f && (charges.Length == 0 || mCurEnergy >= charges[mCurChargeLevel].energyCost)); }
    }

    public virtual bool canFire {
        get { return (projMax == 0 || mCurProjCount < projMax) && hasEnergy; }
    }

    public bool isFireActive {
        get { return mFireActive; }
    }

    public virtual Vector3 spawnPoint {
        get {
            Vector3 pt = _spawnPoint ? _spawnPoint.position : transform.position;
            pt.z = 0.0f;
            return pt;
        }
    }

    public virtual Vector3 dir {
        get {
            if(_spawnPoint) {
                Vector3 r = _spawnPoint.right;
                if(Player.instance.controllerSprite.isLeft)
                    r.x *= -1.0f;
                return r;
            }

            return new Vector3(Mathf.Sign(transform.lossyScale.x), 0.0f, 0.0f);
        }
    }

    /// <summary>
    /// Call this to preserve energy when going to a new scene, usu. when you die
    /// </summary>
    public void SaveEnergySpent(bool preserve) {
        string key = energyTypeKey;
        if(!string.IsNullOrEmpty(key)) {
            SceneState.instance.SetGlobalValueFloat(key, mCurEnergy, preserve);
        }
    }

    public void ResetEnergySpent(bool preserve) {
        string key = energyTypeKey;
        if(!string.IsNullOrEmpty(key)) {
            SceneState.instance.SetGlobalValueFloat(key, weaponEnergyDefaultMax, preserve);
        }

        mCurEnergy = weaponEnergyDefaultMax;
    }

    public virtual void FireStart() {
        if(canFire) {
            StopAllCoroutines();
            StartCoroutine(DoFire());
        }
    }

    public virtual void FireStop() {
        mFireActive = false;

        if(chargeSfx && chargeSfx.isPlaying)
            chargeSfx.Stop();
    }

    public virtual void FireCancel() {
        if(mFireActive) {
            mFireActive = false;
            mFireCancel = true;
        }

        if(chargeSfx && chargeSfx.isPlaying)
            chargeSfx.Stop();
    }

    /// <summary>
    /// Called when player press jump, use for certain weapons. Return true if jump overridden
    /// </summary>
    public virtual bool Jump(Player player) {
        return false;
    }

    public void ResetCharge() {
        if(mCurChargeLevel > 0 && charges.Length > 0)
            charges[mCurChargeLevel].Enable(false);

        mCurChargeLevel = 0;
        mCurTime = 0;

        if(chargeSfx && chargeSfx.isPlaying)
            chargeSfx.Stop();
    }

    protected void PlaySfx(int chargeInd) {
        SoundPlayer s = charges[chargeInd].sfx;
        if(s)// && !s.isPlaying)
            s.Play();
    }

    protected virtual Projectile CreateProjectile(int chargeInd, Transform seek) {
        Projectile ret = null;

        string type = charges.Length > 0 ? charges[chargeInd].projType : null;
        if(!string.IsNullOrEmpty(type)) {
            ret = Projectile.Create(projGroup, type, spawnPoint, dir, seek);
            if(ret) {
                if(SceneState.instance.GetGlobalValue("cheat") > 0) {
                    Damage dmg = ret.GetComponent<Damage>();
                    if(dmg)
                        dmg.amount = 100.0f;
                }

                mCurProjCount++;
                ret.releaseCallback += OnProjRelease;

                //spend energy
                currentEnergy -= charges[chargeInd].energyCost;

                PlaySfx(chargeInd);
            }
        }

        return ret;
    }

    protected virtual void OnEnable() {
        if(mStarted) {
            if(anim) {
                anim.gameObject.SetActive(true);

                anim.Play(mClips[(int)AnimState.normal]);
            }

            if(activeGO)
                activeGO.SetActive(true);

            if(charges.Length > 0)
                charges[mCurChargeLevel].Enable(true);

            mLastChargeShotTime = 0.0f;
        }
    }

    protected virtual void OnDisable() {
        if(anim)
            anim.gameObject.SetActive(false);

        if(activeGO)
            activeGO.SetActive(false);

        if(charges.Length > 0) {
            charges[mCurChargeLevel].Enable(false);
        }

        mFireActive = false;
        mCurChargeLevel = 0;
        mFireCancel = false;
    }

    protected virtual void OnDestroy() {
        if(anim)
            anim.AnimationCompleted -= OnAnimationClipEnd;

        energyChangeCallback = null;
    }

    protected virtual void Awake() {
        if(anim) {
            anim.AnimationCompleted += OnAnimationClipEnd;

            mClips = M8.tk2dUtil.GetSpriteClips(anim, typeof(AnimState));

            anim.gameObject.SetActive(false);
        }

        if(activeGO)
            activeGO.SetActive(false);

        foreach(ChargeInfo inf in charges) {
            inf.Enable(false);
        }

        //get saved energy spent
        string key = energyTypeKey;
        if(!string.IsNullOrEmpty(key))
            mCurEnergy = SceneState.instance.GetGlobalValueFloat(key, weaponEnergyDefaultMax);
    }

    // Use this for initialization
    void Start() {
        mStarted = true;
        OnEnable();
    }

    IEnumerator DoFire() {
        if(anim) {
            anim.Stop();

            if(mClips[(int)AnimState.attack] != null)
                anim.Play(mClips[(int)AnimState.attack]);
            else
                anim.Play(mClips[(int)AnimState.normal]);
        }

        mCurChargeLevel = 0;
        mFireCancel = false;

        //fire projectile
        if(Time.time - mLastChargeShotTime > lockAfterChargeDelay) {
            mLastChargeShotTime = 0.0f;
            CreateProjectile(mCurChargeLevel, null);
        }

        //do charging

        if(charges.Length > 1) {
            mFireActive = true;

            mCurTime = 0.0f;
            WaitForFixedUpdate wait = new WaitForFixedUpdate();

            while(mFireActive) {
                //check if ready for next charge level
                int nextLevel = mCurChargeLevel + 1;
                if(nextLevel < charges.Length) {
                    //check if we can fire this charge
                    if(energyType == EnergyType.Unlimited || currentEnergy >= charges[nextLevel].energyCost) {
                        mCurTime += Time.fixedDeltaTime;
                        if(mCurTime >= charges[nextLevel].delay) {
                            //hide previous charge gameobject and activate/set new one
                            charges[mCurChargeLevel].Enable(false);
                                

                            charges[nextLevel].Enable(true);

                            mCurChargeLevel = nextLevel;

                            //charge sound
                            if(chargeSfx && charges[mCurChargeLevel].chargeSfxOn) {
                                chargeSfx.audio.pitch = charges[mCurChargeLevel].chargeSfxPitch;

                                if(!chargeSfx.isPlaying)
                                    chargeSfx.Play();
                            }
                        }
                    }
                    //else {
                        //if we are only in level 0, then just stop
                        //if(mCurChargeLevel == 0)
                            //break;
                    //}
                }

                if(!Main.instance.input.IsDown(0, InputAction.Fire))
                    break; //release

                yield return wait;
            }
        }

        //release charge?
        if(mCurChargeLevel > 0) {
            if(mFireCancel) {
                mFireCancel = false;
            }
            else {
                if(anim) {
                    if(mClips[(int)AnimState.attack] != null)
                        anim.Play(mClips[(int)AnimState.attack]);
                    else
                        anim.Play(mClips[(int)AnimState.normal]);
                }

                //spawn charged projectile
                CreateProjectile(mCurChargeLevel, null);

                mLastChargeShotTime = Time.time;
            }

            //reset charge
            if(charges.Length > 0) {
                charges[mCurChargeLevel].Enable(false);

                mCurChargeLevel = 0;

                charges[mCurChargeLevel].Enable(true);
            }
        }
    }

    //> AnimationCompleted
    protected virtual void OnAnimationClipEnd(tk2dSpriteAnimator aAnim, tk2dSpriteAnimationClip aClip) {
        if(aAnim == anim && aClip == mClips[(int)AnimState.attack]) {
            //beginning first charge
            if(mFireActive && mClips[(int)AnimState.charge] != null) {
                //anim.Play(mClips[(int)AnimState.charge]);
            }
            else {
                anim.Play(mClips[(int)AnimState.normal]);
            }
        }
    }

    protected virtual void OnProjRelease(EntityBase ent) {
        mCurProjCount = Mathf.Clamp(mCurProjCount - 1, 0, projMax);
        ent.releaseCallback -= OnProjRelease;
    }
}
