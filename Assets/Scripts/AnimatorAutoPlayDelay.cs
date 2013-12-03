using UnityEngine;
using System.Collections;

public class AnimatorAutoPlayDelay : MonoBehaviour {
    public AnimatorData animDat;
    public string take = "default";
    public float delay = 1.0f;
    public bool repeat = false;
    public float repeatDelay = 1.0f;

    private bool mStarted;
    private bool mStopped;

    public void Stop() {
        mStopped = true;
        animDat.Stop();
        CancelInvoke("DoPlay");
    }

    void OnEnable() {
        if(mStarted && !IsInvoking("DoPlay")) {
            mStopped = false;
            Invoke("DoPlay", delay);
        }
    }

    void OnDisable() {
        Stop();
    }

    void Awake() {
        if(animDat == null)
            animDat = GetComponent<AnimatorData>();

        animDat.takeCompleteCallback += AnimCompleted;
    }

    void Start() {
        mStarted = true;
        mStopped = false;
        Invoke("DoPlay", delay);
    }

    void DoPlay() {
        animDat.Play(take);
    }

    void AnimCompleted(AnimatorData dat, AMTake _take) {
        if(repeat && !mStopped)
            Invoke("DoPlay", repeatDelay);
    }
}
