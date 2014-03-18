using UnityEngine;
using System.Collections;

public class LevelController : MonoBehaviour {
    public const string levelBitState = "levelBits";
    public const string levelPickupBitState = "levelPickupBits"; //for certain items that can't be picked up until complete restart

    private const string levelFinalCurState = "levelFinalCur";

    public const string levelTimeAccumKey = "lvlatime";
    public const string levelTimePostfix = "_ctime";

    private static LevelController mInstance = null;
    private static bool mCheckpointActive = false;
    private static Vector3 mCheckpoint;
    private static string mLevelLoaded;

    private float mLastTime;
    private float mAccumTime; //time accumulated
    private bool mTimeStarted;
    private bool mTimePaused;

    /// <summary>
    /// Get the level that was loaded from stage
    /// </summary>
    public static string levelLoaded {
        get {
            return mLevelLoaded;
        }
    }

    public static bool IsLevelComplete(string level) {
        return SceneState.instance.GetGlobalValue(level) == 1;
    }

    public static bool isLevelLoadedComplete {
        get {
            return IsLevelComplete(mLevelLoaded);
        }
    }

    /// <summary>
    /// Determine if life up has been dropped, uses bit 30 in levelPickupBitState
    /// </summary>
    public static bool isLifeUpDropped {
        get { return SceneState.instance.CheckFlag(levelPickupBitState, 30); }
        set { 
            SceneState.instance.SetFlag(levelPickupBitState, 30, value, SlotInfo.gameMode == SlotInfo.GameMode.Hardcore); 
        }
    }

    public static void CheckpointApplyTo(Transform target) {
        if(mCheckpointActive) {
            target.position = mCheckpoint;
        }
    }

    public static void CheckpointSet(Vector3 pos) {
        mCheckpointActive = true;
        mCheckpoint = pos;

        if(mInstance)
            mInstance.TimeProgressSave();
    }

    public static void CheckpointReset() {
        mCheckpointActive = false;
    }

    /// <summary>
    /// For specific level state and pickups, when exiting, call this
    /// </summary>
    public static void LevelStateReset() {
        SceneState.instance.SetGlobalValue(levelBitState, 0, false);

        SceneState.instance.SetGlobalValue(levelPickupBitState, 0, false);

        SceneState.instance.DeleteGlobalValue(levelTimeAccumKey, false);
    }

    public static void Complete(bool persistent) {
        SceneState.instance.SetGlobalValue(mLevelLoaded, 1, persistent);
    }

    public static float LevelTime(string level) {
        return SceneState.instance.GetGlobalValueFloat(level+levelTimePostfix, 0.0f);
    }

    public static string LevelTimeFormat(float time) {
        int centi = Mathf.RoundToInt(time * 100.0f);
        int seconds = Mathf.RoundToInt(time);
        int minutes = seconds / 60;

        return string.Format("{0:D3}:{1:D2}.{2:D2}", minutes, seconds % 60, centi % 100);
    }

    public static LevelController instance { get { return mInstance; } }

    /// <summary>
    /// Call at the beginning of the stage
    /// </summary>
    public void TimeStart() {
        mAccumTime = SceneState.instance.GetGlobalValueFloat(levelTimeAccumKey, 0.0f);
        mLastTime = Time.time;

        mTimeStarted = true;
    }

    /// <summary>
    /// When pausing the game, or cutscene happens, etc.
    /// </summary>
    public void TimePause() {
        if(mTimeStarted && !mTimePaused) {
            mAccumTime += Time.time - mLastTime;
            mTimePaused = true;
        }
    }

    public void TimeResume() {
        if(mTimeStarted && mTimePaused) {
            mLastTime = Time.time;
            mTimePaused = false;
        }
    }

    /// <summary>
    /// Save the current time progress, used on checkpoints
    /// </summary>
    public void TimeProgressSave() {
        if(mTimeStarted) {
            if(!mTimePaused)
                mAccumTime += Time.time - mLastTime;

            SceneState.instance.SetGlobalValueFloat(levelTimeAccumKey, mAccumTime, false);

            if(!mTimePaused)
                mLastTime = Time.time;
        }
    }

    /// <summary>
    /// Save actual time, call as soon as boss is defeated
    /// </summary>
    public void TimeSave() {
        if(mTimeStarted) {
            TimeResume();

            float t = mAccumTime + (Time.time - mLastTime);

            string key = mLevelLoaded + levelTimePostfix;

            float oldT = SceneState.instance.GetGlobalValueFloat(key, float.MaxValue);
            if(oldT < t)
                t = oldT;

            SceneState.instance.SetGlobalValueFloat(key, t, true);

            mTimeStarted = false;

            Debug.Log("Level Clear Time: "+LevelTimeFormat(t));
        }
    }

    void OnDestroy() {
        if(mInstance == this) {
            if(UserData.instance) {
                UserData.instance.actCallback -= OnUserDataAct;
            }

            mInstance = null;
        }
    }

    void Awake() {
        if(mInstance == null) {
            mLevelLoaded = Application.loadedLevelName;

            if(SceneState.instance.HasValue(levelPickupBitState))
                SceneState.instance.SetGlobalValue(levelPickupBitState, SceneState.instance.GetValue(levelPickupBitState, 0), false);

            UserData.instance.actCallback += OnUserDataAct;

            mInstance = this;
        }
        else
            DestroyImmediate(gameObject);
    }

    void OnUserDataAct(UserData ud, UserData.Action act) {
        if(act == UserData.Action.Save) {
            SlotInfo.SaveCurrentSlotData();
            //Debug.Log("save slot data");
        }
    }
}
