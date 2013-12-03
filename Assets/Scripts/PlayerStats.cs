using UnityEngine;
using System.Collections;

/// <summary>
/// Player specific stats
/// </summary>
public class PlayerStats : Stats {
    public const string gameExistKey = "s";
    public const string hpModFlagsKey = "playerHPMod";
    public const int hpModCount = 8;
    public const float hpMod = 2;

    public const int defaultNumLives = 3;

    public const string subTankEnergyFillKey = "etank";
    public const string subTankWeaponFillKey = "wtank";

    public const float subTankMaxValue = 32.0f; //max value of each tank

    public const int stateSubTankEnergy1 = 1;
    public const int stateSubTankEnergy2 = 2;
    public const int stateSubTankWeapon1 = 3;
    public const int stateSubTankWeapon2 = 4;
    public const int stateArmor = 5;

    public const string lifeCountKey = "playerLife";

    public const string itemFlagsKey = "playerItems"; //for sub tanks, armor, etc.

    public event ChangeCallback changeMaxHPCallback;

    private float mDefaultMaxHP;

    private float mSubTankEnergyCur;
    private float mSubTankEnergyMax;

    private float mSubTankWeaponCur;
    private float mSubTankWeaponMax;

    public static bool isGameExists {
        get {
            return SceneState.instance.GetGlobalValue(gameExistKey) == 1;
        }

        set {
            SceneState.instance.SetGlobalValue(gameExistKey, value ? 1 : 0, true);
        }
    }

    public static int curLife {
        get {
            return SceneState.instance.GetGlobalValue(lifeCountKey, defaultNumLives);
        }

        set {
            SceneState.instance.SetGlobalValue(lifeCountKey, Mathf.Clamp(value, 0, 99), false);
        }
    }

    public static bool isArmorAcquired {
        get {
            return SceneState.instance.CheckGlobalFlag(itemFlagsKey, stateArmor);
        }
    }

    public static bool isSubTankEnergy1Acquired {
        get {
            return SceneState.instance.CheckGlobalFlag(itemFlagsKey, stateSubTankEnergy1);
        }
    }

    public static bool isSubTankEnergy2Acquired {
        get {
            return SceneState.instance.CheckGlobalFlag(itemFlagsKey, stateSubTankEnergy2);
        }
    }

    public static bool isSubTankWeapon1Acquired {
        get {
            return SceneState.instance.CheckGlobalFlag(itemFlagsKey, stateSubTankWeapon1);
        }
    }

    public static bool isSubTankWeapon2Acquired {
        get {
            return SceneState.instance.CheckGlobalFlag(itemFlagsKey, stateSubTankWeapon2);
        }
    }


    public static void AddHPMod(int bit) {
        SceneState.instance.SetGlobalFlag(hpModFlagsKey, bit, true, true);
    }

    public static bool IsHPModAcquired(int bit) {
        return SceneState.instance.CheckGlobalFlag(hpModFlagsKey, bit);
    }

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

    public void AcquireSubTankEnergy1() {
        if(!isSubTankEnergy1Acquired) {
            SceneState.instance.SetGlobalFlag(itemFlagsKey, stateSubTankEnergy1, true, true);
            mSubTankEnergyMax += subTankMaxValue;
        }
    }

    public void AcquireSubTankEnergy2() {
        if(!isSubTankEnergy2Acquired) {
            SceneState.instance.SetGlobalFlag(itemFlagsKey, stateSubTankEnergy2, true, true);
            mSubTankEnergyMax += subTankMaxValue;
        }
    }

    public void AcquireSubTankWeapon1() {
        if(!isSubTankWeapon1Acquired) {
            SceneState.instance.SetGlobalFlag(itemFlagsKey, stateSubTankWeapon1, true, true);
            mSubTankWeaponMax += subTankMaxValue;
        }
    }

    public void AcquireSubTankWeapon2() {
        if(!isSubTankWeapon2Acquired) {
            SceneState.instance.SetGlobalFlag(itemFlagsKey, stateSubTankWeapon2, true, true);
            mSubTankWeaponMax += subTankMaxValue;
        }
    }

    public void AcquireArmor() {
        SceneState.instance.SetGlobalFlag(itemFlagsKey, stateArmor, true, true);
        damageReduction = 0.5f;
    }

    public void SaveStates() {
        SceneState.instance.SetGlobalValueFloat(subTankEnergyFillKey, mSubTankEnergyCur, true);
        SceneState.instance.SetGlobalValueFloat(subTankWeaponFillKey, mSubTankWeaponCur, true);
    }

    protected override void OnDestroy() {
        if(SceneState.instance) {
            SceneState.instance.onValueChange -= OnSceneStateValue;
        }

        changeMaxHPCallback = null;

        base.OnDestroy();
    }

    protected override void Awake() {
        mDefaultMaxHP = maxHP;

        SceneState.instance.onValueChange += OnSceneStateValue;

        ApplyHPMod();

        mSubTankEnergyCur = SceneState.instance.GetGlobalValueFloat(subTankEnergyFillKey);
        mSubTankWeaponCur = SceneState.instance.GetGlobalValueFloat(subTankWeaponFillKey);

        mSubTankEnergyMax = 0.0f;
        if(isSubTankEnergy1Acquired)
            mSubTankEnergyMax += subTankMaxValue;
        if(isSubTankEnergy2Acquired)
            mSubTankEnergyMax += subTankMaxValue;

        mSubTankWeaponMax = 0.0f;
        if(isSubTankWeapon1Acquired)
            mSubTankWeaponMax += subTankMaxValue;
        if(isSubTankWeapon2Acquired)
            mSubTankWeaponMax += subTankMaxValue;

        if(isArmorAcquired)
            damageReduction = 0.5f;

        base.Awake();
    }

    void ApplyHPMod() {
        //change max hp for any upgrade
        int numMod = 0;

        //get hp mod flags
        int hpModFlags = SceneState.instance.GetGlobalValue(hpModFlagsKey);
        for(int i = 0, check = 1; i < hpModCount; i++, check <<= 1) {
            if((hpModFlags & check) != 0)
                numMod++;
        }

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

    void OnSceneStateValue(bool isGlobal, string name, SceneState.StateValue val) {
        if(isGlobal && name == hpModFlagsKey) {
            ApplyHPMod();
        }
    }
}
