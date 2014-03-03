using UnityEngine;
using System.Collections;

public class SlotInfo {
    public const string defaultName = "gg";
    public const string dataKey = "dat";
    public const string timeKey = "t";

    public const int hpModMaxCount = 8;

    public const int stateSubTankEnergy1 = 0;
    public const int stateSubTankEnergy2 = 1;
    public const int stateSubTankWeapon1 = 2;
    public const int stateSubTankWeapon2 = 3;
    public const int stateArmor = 4;

    public enum GameMode {
        Normal,
        Hardcore,
        Easy
    }

    public static GameMode gameMode {
        get { return GetGameMode(UserSlotData.currentSlot); }
    }

    public static GameMode GetGameMode(int slot) {
        int d = GetData(slot);

        return (GameMode)((d>>11) & 3);
    }

    //call this once the last level's time has been computed (during Player's Final state)
    public static void ComputeClearTime() {
        float t = 0;

        string[] levelTimeKeys = SceneState.instance.GetGlobalKeys(itm => itm.Key.LastIndexOf(LevelController.levelTimePostfix) != -1);

        for(int i = 0; i < levelTimeKeys.Length; i++) {
            t += SceneState.instance.GetGlobalValueFloat(levelTimeKeys[i]);
        }

        UserSlotData.SetSlotValueFloat(UserSlotData.currentSlot, timeKey, t);
    }

    public static string GetClearTimeString(int slot) {
        if(UserSlotData.HasSlotValue(slot, timeKey)) {
            return LevelController.LevelTimeFormat(UserSlotData.GetSlotValueFloat(slot, timeKey));
        }
        else {
            return "---:--.--";
        }
    }

    public static void WeaponUnlock(int index) {
        if(index > 0) {
            int d = GetData();

            d |= 1<<(index - 1);

            SaveData(d);
        }
    }

    public static bool WeaponIsUnlock(int index) {
        return WeaponIsUnlock(UserSlotData.currentSlot, index);
    }

    public static bool WeaponIsUnlock(int slot, int index) {
        if(index == 0)
            return true;
        else {
            return (GetData(slot) & (1<<(index-1))) != 0;
        }
    }

    public static void SetHeartFlags(int flags) {
        int d = GetData();

        d = (d & 8191) | (flags<<13);

        SaveData(d);
    }

    public static int GetHeartFlags(int slot) {
        int d = GetData(slot);

        return (d >> 13) & 255;
    }

    public static void SetItemsFlags(int flags) {
        int d = GetData();

        d = (d & (~1984)) | (flags<<6);

        SaveData(d);
    }

    public static int GetItemsFlags() {
        return GetItemsFlags(UserSlotData.currentSlot);
    }

    public static int GetItemsFlags(int slot) {
        int d = GetData(slot);
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
        int d = (int)mode;
        SaveData(d<<11);
    }

    //call this before deleting the slot
    public static void DeleteData(int slot) {
        UserSlotData.DeleteValue(slot, dataKey);
        UserSlotData.DeleteValue(slot, timeKey);
    }

    private static int GetData() {
        return GetData(UserSlotData.currentSlot);
    }

    private static int GetData(int slot) {
        return UserSlotData.GetSlotValueInt(slot, dataKey, 0);
    }

    private static void SaveData(int dat) {
        UserSlotData.SetSlotValueInt(UserSlotData.currentSlot, dataKey, dat);
    }
}
