using UnityEngine;
using System.Collections;

public class SpriteBlinkDelay : MonoBehaviour {
    public float delay;

    private SpriteColorBlink[] mBlinks;
    private bool mStarted;
    private bool mActive;

    void OnEnable() {
        if(mStarted && !mActive) {
            Invoke("DoBlink", delay);
        }
    }

    void OnDisable() {
        CancelInvoke();

        mActive = false;

        foreach(SpriteColorBlink blinker in mBlinks) {
            blinker.enabled = false;
        }
    }

    void Awake() {
        mBlinks = GetComponentsInChildren<SpriteColorBlink>(true);
        foreach(SpriteColorBlink blinker in mBlinks) {
            blinker.enabled = false;
        }
    }

    // Use this for initialization
    void Start() {
        mStarted = true;
        Invoke("DoBlink", delay);
    }

    void DoBlink() {
        mActive = true;
        foreach(SpriteColorBlink blinker in mBlinks) {
            blinker.enabled = true;
        }
    }
}
