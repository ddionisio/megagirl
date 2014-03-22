using UnityEngine;
using System.Collections;

public class SlotInfo {
    public const string defaultName = "gg";
    public const string dataKey = "dat";
    public const string timeKey = "t";

    public const int hpModMaxCount = 8;

    public const int stateSubTankEnergy1 = 1;
    public const int stateSubTankEnergy2 = 2;
    public const int stateSubTankWeapon1 = 4;
    public const int stateSubTankWeapon2 = 8;
    public const int stateArmor = 16;

    public enum GameMode {
        Normal,
        Hardcore,
        Easy
    }

    private static int mData=0;
    private static bool mCurrentSlotLoaded=false;

    public static GameMode gameMode {
        get { return GetGameMode(UserSlotData.currentSlot); }
    }

    public static GameMode GetGameMode(int slot) {
        int d = GrabData(slot);

        return (GameMode)((d>>11) & 3);
    }

    //call this once the last level's time has been computed (during Player's Final state)
    public static void ComputeClearTime() {
        float t = 0;

        string[] levelTimeKeys = UserData.instance.GetKeys(itm => itm.Key.LastIndexOf(LevelController.levelTimePostfix) != -1);

        for(int i = 0; i < levelTimeKeys.Length; i++) {
            t += UserData.instance.GetFloat(levelTimeKeys[i]);
        }

        float oldT = UserSlotData.GetSlotValueFloat(UserSlotData.currentSlot, timeKey, float.MaxValue);
        if(t < oldT)
            UserSlotData.SetSlotValueFloat(UserSlotData.currentSlot, timeKey, t);
    }

    public static bool HasClearTime(int slot) {
        return UserSlotData.HasSlotValue(slot, timeKey);
    }

    public static float GetClearTime(int slot) {
        return UserSlotData.GetSlotValueFloat(slot, timeKey);
    }

    public static string GetClearTimeString(int slot) {
        if(HasClearTime(slot)) {
            return LevelController.LevelTimeFormat(GetClearTime(slot));
        }
        else {
            return "---:--.--";
        }
    }

    public static bool IsDead(int slot) {
        int d = GrabData(slot);
        return (d & 8192) != 0;
    }

    public static void SetDead(bool dead) {
        if(!mCurrentSlotLoaded) LoadCurrentSlotData();

        if(dead) {
            mData |= 8192;
        }
        else {
            mData &= ~8192;
        }
    }

    public static void WeaponUnlock(int index) {
        if(index > 0) {
            if(!mCurrentSlotLoaded) LoadCurrentSlotData();
            mData |= 1<<(index - 1);

            SaveCurrentSlotData();
            PlayerPrefs.Save();
        }
    }

    public static bool WeaponIsUnlock(int index) {
        return WeaponIsUnlock(UserSlotData.currentSlot, index);
    }

    public static bool WeaponIsUnlock(int slot, int index) {
        if(index == 0 || index == 7)
            return true;
        else {
            int d = GrabData(slot);
            return (d & (1<<(index-1))) != 0;
        }
    }

    public static void SetHeartFlags(int flags) {
        if(!mCurrentSlotLoaded) LoadCurrentSlotData();
        mData = (mData & 16383) | (flags<<14);
    }

    public static int GetHeartFlags(int slot) {
        int d = GrabData(slot);

        return (d >> 14) & 255;
    }

    public static void SetItemsFlags(int flags) {
        if(!mCurrentSlotLoaded) LoadCurrentSlotData();
        mData = mData | ((flags&31)<<6);
    }

    public static int GetItemsFlags() {
        return GetItemsFlags(UserSlotData.currentSlot);
    }

    public static int GetItemsFlags(int slot) {
        int d = GrabData(slot);
        return (d >> 6) & 31;
    }

    public static int heartCount {
        get {
            return GetHeartCount(UserSlotData.currentSlot);
        }
    }

    public static bool isArmorAcquired {
        get {
            return IsArmorAcquired(UserSlotData.currentSlot);
        }
    }
    
    public static bool isSubTankEnergy1Acquired {
        get {
            return IsSubTankEnergy1Acquired(UserSlotData.currentSlot);
        }
    }
    
    public static bool isSubTankEnergy2Acquired {
        get {
            return IsSubTankEnergy2Acquired(UserSlotData.currentSlot);
        }
    }
    
    public static bool isSubTankWeapon1Acquired {
        get {
            return IsSubTankWeapon1Acquired(UserSlotData.currentSlot);
        }
    }
    
    public static bool isSubTankWeapon2Acquired {
        get {
            return IsSubTankWeapon2Acquired(UserSlotData.currentSlot);
        }
    }
    
    
    public static void AddHPMod(int bit) {
        int hd = GetHeartFlags(UserSlotData.currentSlot);
        hd |= 1<<bit;
        SetHeartFlags(hd);
    }
    
    public static bool IsHPModAcquired(int bit) {
        return (GetHeartFlags(UserSlotData.currentSlot) & (1<<bit)) != 0;
    }

    public static int GetHeartCount(int slot) {
        int flags = GetHeartFlags(slot);
        int numMod = 0;
        for(int i = 0; i < hpModMaxCount; i++) {
            if((flags & (1<<i)) != 0)
                numMod++;
        }
        
        return numMod;
    }

    public static bool IsArmorAcquired(int slot) {
        return (GetItemsFlags(slot) & stateArmor) != 0;
    }
    
    public static bool IsSubTankEnergy1Acquired(int slot) {
        return (GetItemsFlags(slot) & stateSubTankEnergy1) != 0;
    }
    
    public static bool IsSubTankEnergy2Acquired(int slot) {
        return (GetItemsFlags(slot) & stateSubTankEnergy2) != 0;
    }
    
    public static bool IsSubTankWeapon1Acquired(int slot) {
        return (GetItemsFlags(slot) & stateSubTankWeapon1) != 0;
    }
    
    public static bool IsSubTankWeapon2Acquired(int slot) {
        return (GetItemsFlags(slot) & stateSubTankWeapon2) != 0;
    }

    public static void CreateSlot(int slot, GameMode mode) {
        UserSlotData.CreateSlot(slot, defaultName);
        mData = ((int)mode)<<11;
        UserSlotData.SetSlotValueInt(slot, dataKey, mData);

        mCurrentSlotLoaded = true;
    }

    //call this before deleting the slot
    public static void DeleteData(int slot) {
        UserSlotData.DeleteValue(slot, dataKey);
        UserSlotData.DeleteValue(slot, timeKey);
        ClearCurrentSlotLoaded();
    }

    public static void LoadCurrentSlotData() {
        mData = UserSlotData.GetSlotValueInt(UserSlotData.currentSlot, dataKey, 0);
        mCurrentSlotLoaded = true;
    }

    public static void SaveCurrentSlotData() {
        UserSlotData.SetSlotValueInt(UserSlotData.currentSlot, dataKey, mData);
    }

    public static void LoadTimeTrialData() {
        UserSlotData.LoadSlot(99, true);
        mData = 4177983;
        mCurrentSlotLoaded = true;
    }

    public static void ClearCurrentSlotLoaded() {
        mData = 0;
        mCurrentSlotLoaded = false;
    }

    private static int GrabData(int slot) {
        if(mCurrentSlotLoaded && slot == UserSlotData.currentSlot)
            return mData;
        else {
            int d = UserSlotData.GetSlotValueInt(slot, dataKey, 0);
            return d;
        }
    }
}
