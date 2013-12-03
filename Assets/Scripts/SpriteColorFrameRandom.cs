using UnityEngine;
using System.Collections;

public class SpriteColorFrameRandom : MonoBehaviour {
    public tk2dSpriteAnimator anim;
    public bool animPlayRepeat = true;

    public tk2dBaseSprite spr;

    public float startDelay;
    public float delay;
    public float pauseDelay;

    public float startDelayRandOfs;
    public float delayRandOfs;
    public float pauseDelayRandOfs;

    public Color startColor;
    public Color[] endColors = { Color.white };

    public bool squared;

    private WaitForFixedUpdate mDoUpdate;
    private bool mStarted = false;

    void OnEnable() {
        if(mStarted) {
            StartCoroutine(DoPulseUpdate());
        }
    }

    void OnDisable() {
        if(mStarted) {
            StopAllCoroutines();

            if(anim != null)
                anim.Stop();

            if(spr != null)
                spr.color = startColor;
        }
    }

    void Awake() {
        if(anim == null)
            anim = GetComponent<tk2dSpriteAnimator>();

        if(spr == null) {
            spr = GetComponent<tk2dBaseSprite>();
        }

        mDoUpdate = new WaitForFixedUpdate();
    }

    // Use this for initialization
    void Start() {
        mStarted = true;

        StartCoroutine(DoPulseUpdate());
    }

    IEnumerator DoPulseUpdate() {
        spr.color = startColor;

        float sDelay = startDelay + Random.value * startDelayRandOfs;
        if(sDelay > 0.0f)
            yield return new WaitForSeconds(sDelay);
        else
            yield return mDoUpdate;

        float t = 0.0f;
        float curDelay = delay + Random.value * delayRandOfs;
        Color curEndColor = endColors[Random.Range(0, endColors.Length)];

        if(anim != null)
            anim.Play();

        while(true) {
            t += Time.fixedDeltaTime;

            if(t >= curDelay) {
                if(anim != null && animPlayRepeat) {
                    anim.Stop();
                    anim.Play();
                }

                spr.color = startColor;

                t = 0.0f;
                curDelay = delay + Random.value * delayRandOfs;

                curEndColor = endColors[Random.Range(0, endColors.Length)];

                float pdelay = pauseDelay + Random.value * pauseDelayRandOfs;
                if(pdelay > 0.0f)
                    yield return new WaitForSeconds(pdelay);
            }
            else {
                float s = Mathf.Sin(Mathf.PI * (t / delay));
                spr.color = Color.Lerp(startColor, curEndColor, squared ? s * s : s);
            }

            yield return mDoUpdate;
        }
    }
}
