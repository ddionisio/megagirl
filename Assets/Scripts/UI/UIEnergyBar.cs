using UnityEngine;
using System.Collections;

/// <summary>
/// Assumes vertical, just rotate the base for horizontal.
/// </summary>
public class UIEnergyBar : MonoBehaviour {
    public delegate void GenericCallback(UIEnergyBar bar);

    public UILabel label;
    public UISprite icon;

    public UISprite bar; //make sure anchor is bottom and it is in tile mode
    public int barHeight = 3; //height of each bar
	public float barHeightOfs = 0.0f; //lame

    public UISprite panelTop; //make sure anchor is bottom
    public float panelTopYOfs;
    public UISprite panelBase; //make sure anchor is bottom

    public float smoothSpeed = 10.0f; //for when smoothing

    public Color flashColor = Color.red;
    public float flashRepeatDelay = 0.15f;
    public float flashDelay = 1.0f;
    public UIWidget[] flashWidgets;

    public SoundPlayer fillSfx; //during smoothing, the repeating sound to play when increasing

    public event GenericCallback animateEndCallback;

    private int mCurMaxBar = 1;
    private int mCurNumBar;

    private bool mIsAnimate;
    private float mCurT;
    private float mEndT;
    private float mDirT;
    private float mLastAnimTime;
    private bool mFlashed;

    private const string flashRoutine = "DoFlash";

    public int max {
        get { return mCurMaxBar; }
        set {
            if(mCurMaxBar != value) {
                mCurMaxBar = value;
                RefreshHeight();
            }
        }
    }

    public int current {
        get { return mCurNumBar; }
        set {
            if(mCurNumBar != value) {
                mCurT = (float)value;
                mCurNumBar = value;

                if(mIsAnimate && animateEndCallback != null) {
                    animateEndCallback(this);
                }

                mIsAnimate = false;

                if(fillSfx)
                    fillSfx.Stop();

                RefreshBars();
            }
        }
    }

    /// <summary>
    /// Use this for smoothing the transition
    /// </summary>
    public int currentSmooth {
        get { return mCurNumBar; }
        set {
            if(mCurNumBar != value) {
                mCurNumBar = value;
                mEndT = (float)value;
                mDirT = Mathf.Sign(mEndT - mCurT);

                if(!mIsAnimate) {
                    mIsAnimate = true;
                    mLastAnimTime = Time.realtimeSinceStartup;

                    if(mDirT > 0.0f && fillSfx)
                        fillSfx.Play();
                }
            }
            else {
                if(animateEndCallback != null) {
                    animateEndCallback(this);
                }
            }
        }
    }

    public bool isAnimating { get { return mIsAnimate; } }

    public void SetIconSprite(string atlasRef) {
        if(icon) {
            icon.spriteName = atlasRef;
            icon.MakePixelPerfect();

            Flash(false);
        }
    }

    public void SetBarSprite(string atlasRef) {
        bar.spriteName = atlasRef;
    }

    public void SetBarColor(Color clr) {
        bar.color = clr;
    }

    public void RefreshBars() {
        if(mCurNumBar <= 0)
            bar.gameObject.SetActive(false);
        else {
            bar.gameObject.SetActive(true);
			float h = mCurNumBar * barHeight;
			h += h*barHeightOfs;
            bar.height = Mathf.RoundToInt(h);
        }
    }

    public void Flash(bool on) {
        StopCoroutine(flashRoutine);

        if(on)
            StartCoroutine(flashRoutine);
        else {
            if(mFlashed) {
                for(int i = 0; i < flashWidgets.Length; i++)
                    flashWidgets[i].color = Color.white;
                mFlashed = false;
            }
        }
    }

    void OnDisable() {
        mCurT = (float)mCurNumBar;
        mIsAnimate = false;

        if(fillSfx)
            fillSfx.Stop();

        Flash(false);
    }

    void OnDestroy() {
        animateEndCallback = null;
    }

    void RefreshHeight() {
        int h = barHeight * mCurMaxBar;

        if(panelBase) {
            panelBase.height = h;
        }

        if(panelTop) {
            Vector3 topPos = new Vector3(0, h + panelTopYOfs, 0);
            panelTop.transform.localPosition = topPos;
        }
    }

    void Update() {
        if(mIsAnimate) {
            float dt = Time.realtimeSinceStartup - mLastAnimTime;
            mCurT += mDirT * smoothSpeed * dt;
            mLastAnimTime = Time.realtimeSinceStartup;

            if((mDirT < 0.0f && mCurT <= mEndT) || (mDirT > 0.0f && mCurT >= mEndT)) {
                mCurT = (float)mCurNumBar;

                RefreshBars();

                mIsAnimate = false;

                if(fillSfx)
                    fillSfx.Stop();

                if(animateEndCallback != null)
                    animateEndCallback(this);
            }
            else {
                int b = Mathf.RoundToInt(mCurT);
                if(b == 0)
                    bar.gameObject.SetActive(false);
                else {
                    bar.gameObject.SetActive(true);
					float h = b * barHeight;
					h += h*barHeightOfs;
					bar.height = Mathf.RoundToInt(h);
                }
            }
        }
    }

    IEnumerator DoFlash() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        float lastTime = Time.fixedTime;
        float lastRepeatTime = 0;

        while(Time.fixedTime - lastTime <= flashDelay) {
            if(Time.fixedTime - lastRepeatTime > flashRepeatDelay) {
                if(mFlashed) {
                    for(int i = 0; i < flashWidgets.Length; i++)
                        flashWidgets[i].color = Color.white;
                }
                else {
                    for(int i = 0; i < flashWidgets.Length; i++)
                        flashWidgets[i].color = flashColor;
                }

                mFlashed = !mFlashed;
                lastRepeatTime = Time.fixedTime;
            }

            yield return wait;
        }

        if(mFlashed) {
            for(int i = 0; i < flashWidgets.Length; i++)
                flashWidgets[i].color = Color.white;
            mFlashed = false;
        }
    }
}
