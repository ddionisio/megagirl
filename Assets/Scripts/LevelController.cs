using UnityEngine;
using System.Collections;

public class LevelController : MonoBehaviour {
    public const string levelBitState = "levelBits";
    public const string levelPickupBitState = "levelPickupBits"; //for certain items that can't be picked up until complete restart

    private const string levelFinalCurState = "levelFinalCur";

    public const string levelTimeAccumKey = "lvlatime";
    public const string levelTimePostfix = "_t";

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

    public static bool isLevelComplete(string level) {
        return SceneState.instance.GetGlobalValue(level) == 1;
    }

    /// <summary>
    /// Determine if life up has been dropped, uses bit 30 in levelPickupBitState
    /// </summary>
    public static bool isLifeUpDropped {
        get { return SceneState.instance.CheckGlobalFlag(levelPickupBitState, 30); }
        set { SceneState.instance.SetGlobalFlag(levelPickupBitState, 30, value, false); }
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

    public static void Complete() {
        SceneState.instance.SetGlobalValue(mLevelLoaded, 1, true);
    }

    public static float LevelTime(string level) {
        return SceneState.instance.GetGlobalValueFloat(level+levelTimePostfix, 0.0f);
    }

    public static string LevelTimeFormat(float time) {
        int centi = Mathf.RoundToInt(time * 100.0f);
        int seconds = Mathf.RoundToInt(time);
        int minutes = seconds / 60;

        return string.Format("{0:D2}:{1:D2}.{2:D2}", minutes % 60, seconds % 60, centi % 100);
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
            SceneState.instance.SetGlobalValueFloat(mLevelLoaded+levelTimePostfix, t, true);

            mTimeStarted = false;

            Debug.Log("Level Clear Time: "+LevelTimeFormat(t));
        }
    }

    void OnDestroy() {
        if(mInstance == this) {
            mInstance = null;
        }
    }

    void Awake() {
        if(mInstance == null) {
            mLevelLoaded = Application.loadedLevelName;

            mInstance = this;
        }
        else
            DestroyImmediate(gameObject);
    }
}
