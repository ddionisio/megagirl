using UnityEngine;
using System.Collections;

/// <summary>
/// Player specific stats
/// </summary>
public class PlayerStats : Stats {
    public const string gameExistKey = "s";
    public const float hpMod = 2;

    public const int defaultNumLives = 5;

    public const float armorRating = 0.3f;

    public const string subTankEnergyFillKey = "etank";
    public const string subTankWeaponFillKey = "wtank";
    public const string hpKey = "chp";
    public const string deathCountKey = "ded";

    public const float subTankMaxValue = 32.0f; //max value of each tank

    public const string lifeCountKey = "playerLife";

    public static bool savePersist = false;

    public GameObject energyShield;
    public GameObject[] energyShieldPts;
    public float energyShieldMaxHP = 9.0f;

    public bool hpPersist;

    public GameObject invulGO;

    public SoundPlayer energyShieldHitSfx;

    public event ChangeCallback changeMaxHPCallback;

    private float mDefaultMaxHP;

    private float mSubTankEnergyCur;
    private float mSubTankEnergyMax;

    private float mSubTankWeaponCur;
    private float mSubTankWeaponMax;

    private float mEnergyShieldCurHP;

    private bool mNoPain;

    public static int curLife {
        get {
            return SceneState.instance.GetGlobalValue(lifeCountKey, defaultNumLives);
        }

        set {
            int l = SceneState.instance.GetGlobalValue(lifeCountKey, defaultNumLives);
            if(value < l) {
                SceneState.instance.SetGlobalValue(deathCountKey, deathCount+1, true);
            }

            SceneState.instance.SetGlobalValue(lifeCountKey, Mathf.Clamp(value, 0, 99), savePersist);
        }
    }

    public static int deathCount {
        get { return SceneState.instance.GetGlobalValue(deathCountKey, 0); }
    }

    public bool noPain { get { return mNoPain; } set { mNoPain = value; } }

    public float subTankEnergyCurrent {
        get { return mSubTankEnergyCur; }
        set {
            float val = Mathf.Clamp(value, 0.0f, mSubTankEnergyMax);
            if(mSubTankEnergyCur != val)
                mSubTankEnergyCur = val;
        }
    }

    public float subTankWeaponCurrent {
        get { return mSubTankWeaponCur; }
        set {
            float val = Mathf.Clamp(value, 0.0f, mSubTankWeaponMax);
            if(mSubTankWeaponCur != val)
                mSubTankWeaponCur = val;
        }
    }

    public bool energyShieldIsActive {
        get { return energyShield.gameObject.activeSelf; }
    }

    public int energyShieldActivePointCount {
        get {
            float c = (float)energyShieldPts.Length;
            return Mathf.CeilToInt(c*(mEnergyShieldCurHP/energyShieldMaxHP));
        }
    }

    public override bool isInvul {
        get {
            return base.isInvul || invulGO.activeSelf;
        }
        set {
            base.isInvul = value;
        }
    }

    public void AcquireSubTankEnergy1() {
        if(!SlotInfo.isSubTankEnergy1Acquired) {
            mSubTankEnergyMax += subTankMaxValue;

            int d = SlotInfo.GetItemsFlags();
            d |= SlotInfo.stateSubTankEnergy1;
            SlotInfo.SetItemsFlags(d);
        }
    }

    public void AcquireSubTankEnergy2() {
        if(!SlotInfo.isSubTankEnergy2Acquired) {
            mSubTankEnergyMax += subTankMaxValue;

            int d = SlotInfo.GetItemsFlags();
            d |= SlotInfo.stateSubTankEnergy2;
            SlotInfo.SetItemsFlags(d);
        }
    }

    public void AcquireSubTankWeapon1() {
        if(!SlotInfo.isSubTankWeapon1Acquired) {
            mSubTankWeaponMax += subTankMaxValue;

            int d = SlotInfo.GetItemsFlags();
            d |= SlotInfo.stateSubTankWeapon1;
            SlotInfo.SetItemsFlags(d);
        }
    }

    public void AcquireSubTankWeapon2() {
        if(!SlotInfo.isSubTankWeapon2Acquired) {
            mSubTankWeaponMax += subTankMaxValue;

            int d = SlotInfo.GetItemsFlags();
            d |= SlotInfo.stateSubTankWeapon2;
            SlotInfo.SetItemsFlags(d);
        }
    }

    public void AcquireArmor() {
        damageReduction = armorRating;

        int d = SlotInfo.GetItemsFlags();
        d |= SlotInfo.stateArmor;
        SlotInfo.SetItemsFlags(d);
    }

    public void SaveStates() {
        SceneState.instance.SetGlobalValueFloat(subTankEnergyFillKey, mSubTankEnergyCur, true);
        SceneState.instance.SetGlobalValueFloat(subTankWeaponFillKey, mSubTankWeaponCur, true);

        if(hpPersist) {
            if(mCurHP > 0)
                SceneState.instance.SetGlobalValueFloat(hpKey, mCurHP, savePersist);
            else
                SceneState.instance.DeleteGlobalValue(hpKey, savePersist);
        }
    }

    public void EnergyShieldSetActive(bool aActive) {
        energyShield.SetActive(aActive);

        if(aActive) {
            for(int i = 0; i < energyShieldPts.Length; i++)
                energyShieldPts[i].SetActive(true);

            mEnergyShieldCurHP = energyShieldMaxHP;
        }
        else {
            mEnergyShieldCurHP = 0.0f;
        }
    }

    public void RefreshHPMod() {
        int numMod = SlotInfo.heartCount;

        float newMaxHP = mDefaultMaxHP + numMod * hpMod;
        
        if(maxHP != newMaxHP) {
            float prevMaxHP = maxHP;
            
            maxHP = newMaxHP;
            
            float delta = maxHP - prevMaxHP;
            
            if(changeMaxHPCallback != null) {
                changeMaxHPCallback(this, delta);
            }
            
            if(delta > 0) {
                curHP += delta;
            }
        }
    }

    public void LoadHP() {
        mCurHP = SceneState.instance.GetGlobalValueFloat(hpKey, maxHP);
    }

    public override bool ApplyDamage(Damage damage, Vector3 hitPos, Vector3 hitNorm) {

        mLastDamage = damage;
        mLastDamagePos = hitPos;
        mLastDamageNorm = hitNorm;
        
        if(!isInvul && curHP > 0.0f) {
            float amt = CalculateDamageAmount(damage);

            //determine if energy
            if(energyShieldIsActive) {
                float shieldDmgAmt = 0;

                switch(damage.type) {
                    case Damage.Type.Energy:
                        shieldDmgAmt = amt;
                        amt = 0.0f;
                        break;

                    case Damage.Type.Explosion:
                        amt *= 0.5f;
                        shieldDmgAmt = amt;
                        break;
                }

                if(shieldDmgAmt > 0.0f) {
                    mEnergyShieldCurHP = Mathf.Clamp(mEnergyShieldCurHP - shieldDmgAmt, 0.0f, energyShieldMaxHP);

                    if(mEnergyShieldCurHP <= 0.0f) {
                        energyShield.SetActive(false);
                    }
                    else {
                        int numActive = energyShieldActivePointCount;
                        for(int i = 0; i < numActive; i++) {
                            energyShieldPts[i].SetActive(true);
                        }
                        for(int i = numActive; i < energyShieldPts.Length; i++) {
                            energyShieldPts[i].SetActive(false);
                        }
                    }

                    energyShieldHitSfx.Play();
                }
            }

            if(amt > 0.0f) {
                curHP -= amt;

                ApplyDamageEvent(damage);

                return true;
            }
        }
        
        return false;
    }

    protected override void OnDestroy() {
        changeMaxHPCallback = null;

        base.OnDestroy();
    }

    protected override void Awake() {
        mDefaultMaxHP = maxHP;

        RefreshHPMod();

        mSubTankEnergyCur = SceneState.instance.GetGlobalValueFloat(subTankEnergyFillKey);
        mSubTankWeaponCur = SceneState.instance.GetGlobalValueFloat(subTankWeaponFillKey);

        mSubTankEnergyMax = 0.0f;
        if(SlotInfo.isSubTankEnergy1Acquired)
            mSubTankEnergyMax += subTankMaxValue;
        if(SlotInfo.isSubTankEnergy2Acquired)
            mSubTankEnergyMax += subTankMaxValue;

        mSubTankWeaponMax = 0.0f;
        if(SlotInfo.isSubTankWeapon1Acquired)
            mSubTankWeaponMax += subTankMaxValue;
        if(SlotInfo.isSubTankWeapon2Acquired)
            mSubTankWeaponMax += subTankMaxValue;

        if(SlotInfo.isArmorAcquired)
            damageReduction = armorRating;

        energyShield.SetActive(false);

        base.Awake();

        if(hpPersist) {
            LoadHP();
        }

        invulGO.SetActive(false);
    }
}
