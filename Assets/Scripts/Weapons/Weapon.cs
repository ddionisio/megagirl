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

    public const string weaponFlagsKey = "playerWeapons";

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

    public static bool IsAvailable(int index) {
        //TODO: temp! remove later!
        if(index == 3 || index == 4)
            return true;
        return index == 0 ? true : SceneState.instance.CheckGlobalFlag(weaponFlagsKey, index);
        //return true;
    }

    public static void UnlockWeapon(int index) {
        SceneState.instance.SetGlobalFlag(weaponFlagsKey, index, true, true);
    }

    public static string GetWeaponEnergyKey(EnergyType type) {
        if(type == EnergyType.Unlimited || type == EnergyType.NumTypes)
            return null;

        return weaponEnergyPrefix + ((int)type);
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

    public virtual bool canFire {
        get { return (projMax == 0 || mCurProjCount < projMax) && (energyType == EnergyType.Unlimited || (mCurEnergy > 0.0f && (charges.Length == 0 || mCurEnergy >= charges[mCurChargeLevel].energyCost))); }
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
    public void SaveEnergySpent() {
        string key = energyTypeKey;
        if(!string.IsNullOrEmpty(key)) {
            SceneState.instance.SetGlobalValueFloat(key, mCurEnergy, false);
        }
    }

    public void ResetEnergySpent() {
        string key = energyTypeKey;
        if(!string.IsNullOrEmpty(key)) {
            SceneState.instance.SetGlobalValueFloat(key, weaponEnergyDefaultMax, false);
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
    }

    public virtual void FireCancel() {
        if(mFireActive) {
            mFireActive = false;
            mFireCancel = true;
        }
    }

    public void ResetCharge() {
        if(mCurChargeLevel > 0 && charges.Length > 0)
            charges[mCurChargeLevel].Enable(false);

        mCurChargeLevel = 0;
        mCurTime = 0;
    }

    protected virtual Projectile CreateProjectile(int chargeInd, Transform seek) {
        Projectile ret = null;

        string type = charges.Length > 0 ? charges[chargeInd].projType : null;
        if(!string.IsNullOrEmpty(type)) {
            ret = Projectile.Create(projGroup, type, spawnPoint, dir, seek);
            if(ret) {
                mCurProjCount++;
                ret.releaseCallback += OnProjRelease;

                //spend energy
                currentEnergy -= charges[chargeInd].energyCost;
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
        CreateProjectile(mCurChargeLevel, null);

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

                            //beginning first charge
                            if(mCurChargeLevel == 1) {
                                if(anim && mClips[(int)AnimState.charge] != null) {
                                    anim.Play(mClips[(int)AnimState.charge]);
                                }
                            }
                        }
                    }
                    //else {
                        //if we are only in level 0, then just stop
                        //if(mCurChargeLevel == 0)
                            //break;
                    //}
                }

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
            anim.Play(mClips[(int)AnimState.normal]);
        }
    }

    protected virtual void OnProjRelease(EntityBase ent) {
        mCurProjCount = Mathf.Clamp(mCurProjCount - 1, 0, projMax);
        ent.releaseCallback -= OnProjRelease;
    }
}
