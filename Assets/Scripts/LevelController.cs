using UnityEngine;
using System.Collections;

public class LevelController : MonoBehaviour {
    private static bool mCheckpointActive = false;
    private static Vector3 mCheckpoint;
    private static string mLevelLoaded;

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

    public static void CheckpointApplyTo(Transform target) {
        if(mCheckpointActive) {
            target.position = mCheckpoint;
        }
    }

    public static void CheckpointSet(Vector3 pos) {
        mCheckpointActive = true;
        mCheckpoint = pos;
    }

    public static void CheckpointReset() {
        mCheckpointActive = false;
    }

    public static void Complete() {
        SceneState.instance.SetGlobalValue(mLevelLoaded, 1, true);
    }

    void Awake() {
        mLevelLoaded = Application.loadedLevelName;
    }
}
