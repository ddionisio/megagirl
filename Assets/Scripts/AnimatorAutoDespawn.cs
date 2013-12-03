using UnityEngine;
using System.Collections;

/// <summary>
/// Make sure to set auto play on enable
/// </summary>
public class AnimatorAutoDespawn : MonoBehaviour {
    public float delay = 0.2f;

    private AnimatorData mAnimDat;
    private bool mSpawned = false;
    private bool mActive = false;

    void OnSpawned() {
        mSpawned = true;
        mActive = true;

        if(delay > 0.0f)
            Invoke("DoActive", delay);
        else
            DoActive();
    }

    void OnDespawned() {
        mSpawned = false;
        mActive = false;
    }

    void Awake() {
        mAnimDat = GetComponent<AnimatorData>();
    }

    // Update is called once per frame
    void Update() {
        if(mSpawned && mActive) {
            if(!mAnimDat.isPlaying && !mAnimDat.isPaused)
                PoolController.ReleaseAuto(transform);
        }
    }

    void DoActive() {
        mActive = true;
    }
}
