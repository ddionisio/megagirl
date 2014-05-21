using UnityEngine;
using System.Collections;

using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class AchievementServiceOffline : MonoBehaviour, Achievement.IService {
    public const string userDataKey = "la";

    [System.Serializable]
    public class AchievedData {
        public int id;
        public bool achieved;

        public AchievedData(int aId) {
            id = aId;
            achieved = false;
        }
    }

    private AchievedData[] mData;

    public bool AchievementAllow() {
        return true;
    }

    /// <summary>
    /// Return true if we are ready to process new data.  Should return false if processing a data or is still initializing.
    /// </summary>
    public bool AchievementIsReady() {
        return true; //always ready!
    }

    /// <summary>
    /// Return the current data being processed, return null if no data to process.
    /// </summary>
    public Achievement.Data AchievementCurrentData() {
        return null; //we are always ready anyhow, so no current data
    }

    /// <summary>
    /// Get the current status of data being processed.
    /// </summary>
    public Achievement.Status AchievementCurrentStatus() {
        return Achievement.Status.Complete;
    }

    /// <summary>
    /// Check to see if given achievement has already been completed.
    /// </summary>
    public bool AchievementIsUnlocked(Achievement.Data data) {
        int ind = -1;
        for(int i = 0; i < mData.Length; i++) {
            if(mData[i].id == data.id) {
                ind = i;
                break;
            }
        }

        return ind == -1 ? false : mData[ind].achieved;
    }

    /// <summary>
    /// Called when processing new data.
    /// </summary>
    public void AchievementProcessData(Achievement.Data data, int progress, bool complete) {
        for(int i = 0; i < mData.Length; i++) {
            if(mData[i].id == data.id) {
                if(mData[i].achieved != complete) {
                    mData[i].achieved = complete;
                    SaveData(mData);
                    PlayerPrefs.Save();
                }
                break;
            }
        }
    }

    AchievedData[] LoadData() {
        AchievedData[] ret = null;

        string dat = PlayerPrefs.GetString(userDataKey, "");
        if(!string.IsNullOrEmpty(dat)) {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(System.Convert.FromBase64String(dat));
            ret = (AchievedData[])bf.Deserialize(ms);
        }

        return ret == null ? new AchievedData[0] : ret;
    }

    void SaveData(AchievedData[] dat) {
        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        bf.Serialize(ms, dat);
        PlayerPrefs.SetString(userDataKey, System.Convert.ToBase64String(ms.GetBuffer()));
    }

    void Awake() {
        Achievement.instance.RegisterService(this);

        //load data
        mData = LoadData();
        if(mData == null) { //create new and put in the ids
            Achievement.Data[] data = Achievement.instance.data;
            mData = new AchievedData[data.Length];
            for(int i = 0; i < data.Length; i++) {
                mData[i] = new AchievedData(data[i].id);
            }
        }
        //TODO: match with size when, if ever, new achievements added, or removed
    }
}
