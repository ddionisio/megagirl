using UnityEngine;
using System.Collections;

public class TimeTrial : MonoBehaviour {
    public const string trialKeyPostfix = "_trial";

    [System.Serializable]
    public class Data {
        public string name;
        public string iconRef;
        public string level;
        public int achieveId; //for unlocking
        public bool requireUnlock;
    }

    public Data[] data;

    public static void Save(string level, float time) {
        PlayerPrefs.SetFloat(level+trialKeyPostfix, time);
    }

    public static bool Exists(string level) {
        return PlayerPrefs.HasKey(level+trialKeyPostfix);
    }

    /// <summary>
    /// Returns 0 if no record found.
    /// </summary>
    public static float Load(string level) {
        return PlayerPrefs.GetFloat(level+trialKeyPostfix, 0.0f);
    }

    public static void Post(string level, float time) {
        //post to leaderboard
        int ind = -1;
        for(int i = 0; i < mInstance.data.Length; i++) {
            if(mInstance.data[i].level == level) {
                ind = i;
                break;
            }
        }
        
        if(ind != -1)
            Leaderboard.instance.PostScore("Trial - " + mInstance.data[ind].name, Mathf.RoundToInt(time*1000.0f));
    }

    private static TimeTrial mInstance;

    public static TimeTrial instance { get { return mInstance; } }

    void OnDestroy() {
        if(mInstance == this) {
            mInstance = null;
        }
    }

    void Awake() {
        if(mInstance == null) {
            mInstance = this;
        }
    }
}
